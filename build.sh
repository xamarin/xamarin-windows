#!/bin/bash

BASE_DIR=$(cd `dirname $0`; pwd)
BUILD_DIR=$BASE_DIR/build
AUTOGEN_ARGS="--host=x86_64-w64-mingw32 --disable-boehm --with-runtime_preset=winaot"
MONO_REPO=https://github.com/mono/mono.git
VSIX_REFERENCE_ASSEMBLIES_PATH=$BUILD_DIR/ReferenceAssemblies
VSIX_MSBUILD_PATH=$BUILD_DIR/MSBuild

FORCE_MONO_REBUILD=0
FORCE_MONO_EXE_REBUILD=0
FORCE_MONO_BCL_REBUILD=0

function usage {
  cat <<EOF
Usage: $SELF [options]
Options:
  --run-test                 Run tests after successful build.
  --force-mono-rebuild       Forces a complete rebuild of mono (native executables and BCL).
  --force-mono-exe-rebuild   Forces a rebuild of the mono native executable.
  --force-mono-bcl-rebuild   Forces a rebuild of the mono BCL.
  --help                     Displays this information and exits.
EOF
  exit $1
}

while [ "${1:0:2}" = '--' ]; do
  NAME=${1%%=*}
  VALUE=${1#*=}
  case $NAME in
    '--run-test') RUN_TEST=1 ;;
    '--force-mono-rebuild') FORCE_MONO_REBUILD=1 ;;
    '--force-mono-exe-rebuild') FORCE_MONO_EXE_REBUILD=1 ;;
    '--force-mono-bcl-rebuild') FORCE_MONO_BCL_REBUILD=1 ;;
    '--help')
      usage 0
      ;;
    *)
      echo "Unrecognized option or syntax error in option '$1'"
      usage 1
      ;;
  esac
  shift
done

if [ "$RUN_TEST" = '1' ]; then
  echo "Tests will be run after successful build"
fi

if ! git --version | grep -q windows; then
  echo "Git for Windows is required."
  exit 1
fi
if [ -d "/cygdrive/c/Program Files (x86)/Mono/bin" ]; then
  SYSTEM_MONO_PATH="/cygdrive/c/Program Files (x86)/Mono/bin"
elif [ -d "/cygdrive/c/Program Files/Mono/bin" ]; then
  SYSTEM_MONO_PATH="/cygdrive/c/Program Files/Mono/bin"
else
  echo "No Mono installation found in 'C:\Program Files (x86)\Mono\bin' nor 'C:\Program Files\Mono\bin'. A 64-bit or 32-bit Mono installation is required."
  exit 1
fi
echo "Using system mono: '$(cygpath -w "$SYSTEM_MONO_PATH")'"

NUGET_EXE=$BASE_DIR/buildtools/nuget.exe
test ! -f "$NUGET_EXE" && echo "$NUGET_EXE does not exist" && exit 1
echo "Using NuGet executable: '$(cygpath -w "$NUGET_EXE")'"

VSWHERE_EXE="/cygdrive/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe"
test ! -f "$VSWHERE_EXE" && echo "$VSWHERE_EXE does not exist" && exit 1
VSINSTALLDIR="$(cygpath "$("$VSWHERE_EXE" -latest -requires 'Microsoft.Component.MSBuild' -property installationPath -format value | tr -d '\r')")"
test -z "$VSINSTALLDIR" && echo "$VSWHERE_EXE did not return an MSBuild installation path" && exit 1
echo "Looking for MSBuild.exe in Visual Studio installation at '$(cygpath -w "$VSINSTALLDIR")'..."

MSBUILD_EXE="$VSINSTALLDIR/MSBuild/15.0/Bin/MSBuild.exe"
test ! -f "$MSBUILD_EXE" && echo "$MSBUILD_EXE does not exist" && exit 1
echo "Using MSBuild executable: '$(cygpath -w "$MSBUILD_EXE")'"

VC_ROOT="$VSINSTALLDIR/VC"
CLANGC2_VERSION_FILE="$VC_ROOT/Auxiliary/Build/Microsoft.ClangC2Version.default.txt"
test ! -f "$CLANGC2_VERSION_FILE" && echo "$CLANGC2_VERSION_FILE does not exist (is the Clang feature installed in Visual Studio?)" && exit 1
CLANGC2_VERSION=$(sed -n 1p "$CLANGC2_VERSION_FILE" | sed 's/\s//g')
test -z "$CLANGC2_VERSION" && echo "Failed to determine clang version from $CLANGC2_VERSION_FILE" && exit 1
echo "Found ClangC2 version $CLANGC2_VERSION"
CLANGC2_PATH="$VC_ROOT/Tools/ClangC2/$CLANGC2_VERSION/bin/HostX64"
test ! -d "$CLANGC2_PATH" && echo "$CLANGC2_PATH does not exist (is the Clang feature installed in Visual Studio?)" && exit 1

VCTOOLS_VERSION_FILE="$VC_ROOT/Auxiliary/Build/Microsoft.VCToolsVersion.default.txt"
test ! -f "$VCTOOLS_VERSION_FILE" && echo "$VCTOOLS_VERSION_FILE does not exist (is the Clang feature installed in Visual Studio?)" && exit 1
VCTOOLS_VERSION=$(sed -n 1p "$VCTOOLS_VERSION_FILE" | sed 's/\s//g')
test -z "$VCTOOLS_VERSION" && echo "Failed to determine clang version from $VCTOOLS_VERSION_FILE" && exit 1
echo "Found VCTools version $VCTOOLS_VERSION"
VCTOOLS_PATH="$VC_ROOT/Tools/MSVC/$VCTOOLS_VERSION/bin/HostX64/x64"
test ! -d "$VCTOOLS_PATH" && echo "$VCTOOLS_PATH does not exist (is the Clang feature installed in Visual Studio?)" && exit 1

pushd "$BASE_DIR" > /dev/null

echo "Updating Git submodules..."
git submodule update --init --recursive || exit $?

echo "Restoring NuGet packages..."
"$NUGET_EXE" restore . || exit $?

# Use MSBuild to get the commit for mono
MONO_COMMIT=$("$MSBUILD_EXE" Xamarin.Windows.props /v:diag /t:NoSuchTarget | grep 'MonoCommit = ' | cut -d= -f2 | sed 's/\s//g')
test -z "$MONO_COMMIT" && echo "Failed to determine MONO_COMMIT" && exit 1
# Get the version were building
XAMARIN_WINDOWS_VERSION=$("$MSBUILD_EXE" vs/Xamarin.Windows.VisualStudio.Vsix/Xamarin.Windows.VisualStudio.Vsix.csproj /t:GetXamarinWindowsProductVersions | grep 'XamarinWindowsVersion=' | cut -d= -f2)
XAMARIN_WINDOWS_FULL_VERSION=$("$MSBUILD_EXE" vs/Xamarin.Windows.VisualStudio.Vsix/Xamarin.Windows.VisualStudio.Vsix.csproj /t:GetXamarinWindowsProductVersions | grep 'XamarinWindowsFullVersion=' | cut -d= -f2)
test -z "$XAMARIN_WINDOWS_VERSION" && echo "Failed to determine XAMARIN_WINDOWS_VERSION" && exit 1
test -z "$XAMARIN_WINDOWS_FULL_VERSION" && echo "Failed to determine XAMARIN_WINDOWS_FULL_VERSION" && exit 1

echo "Building Xamarin Windows $XAMARIN_WINDOWS_FULL_VERSION based on Mono $MONO_COMMIT..."

mkdir -p "$BUILD_DIR"

if [ ! -f "$BUILD_DIR/mono_configured" ]; then
  FORCE_MONO_REBUILD=1
fi
if [ ! -f "$BUILD_DIR/mono_exe_built" ]; then
  FORCE_MONO_EXE_REBUILD=1
fi
if [ ! -f "$BUILD_DIR/mono_bcl_built" ]; then
  FORCE_MONO_BCL_REBUILD=1
fi

if [ ! -d external/mono/.git ]; then
  git clone "$MONO_REPO" external/mono || exit $?
  FORCE_MONO_REBUILD=1
else
  pushd external/mono > /dev/null
  CURRENT_MONO_COMMIT=$(git rev-parse HEAD)
  if [[ $CURRENT_MONO_COMMIT != $MONO_COMMIT ]]; then
    FORCE_MONO_REBUILD=1
  elif [ ! -f config.log ]; then
    FORCE_MONO_REBUILD=1
  elif ! grep -q "\$ \./configure .*$AUTOGEN_ARGS" config.log; then
    FORCE_MONO_REBUILD=1
  fi
  popd > /dev/null
fi

if [ $FORCE_MONO_REBUILD = 1 ]; then
  FORCE_MONO_EXE_REBUILD=1
  FORCE_MONO_BCL_REBUILD=1
  pushd external/mono > /dev/null
  git clean -d -x -f || exit $?
  git checkout -- '*' || exit $?
  git checkout master || exit $?
  git pull || exit $?
  git checkout $MONO_COMMIT || exit $?
  git submodule update --init --recursive || exit $?
  git reset --hard || exit $?
  ./autogen.sh $AUTOGEN_ARGS || exit $?
  popd > /dev/null
  touch "$BUILD_DIR/mono_configured"
fi

if [ $FORCE_MONO_EXE_REBUILD = 1 ]; then
  pushd external/mono > /dev/null
  "$MSBUILD_EXE" /p:PlatformToolset=v140 /p:Platform=x64 /p:Configuration=Release /p:MONO_TARGET_GC=sgen msvc/mono.sln || exit $?
  popd > /dev/null
  touch "$BUILD_DIR/mono_exe_built"
fi

if [ $FORCE_MONO_BCL_REBUILD = 1 ]; then
  pushd external/mono > /dev/null
  export MONO_EXECUTABLE=$(cygpath -u -a msvc/build/sgen/x64/bin/Release/mono-sgen.exe)
  OLDPATH=$PATH
  export PATH="$CLANGC2_PATH:$VCTOOLS_PATH:$SYSTEM_MONO_PATH:$PATH"
  make -C mcs/ V=1 || exit $?
  export PATH=$OLDPATH
  popd > /dev/null
  touch "$BUILD_DIR/mono_bcl_built"
fi

"$MSBUILD_EXE" msbuild/CopyReferenceAssembliesFromMonoDevRoot.proj || exit $?
"$MSBUILD_EXE" msbuild/CreateToolchain.proj || exit $?
"$MSBUILD_EXE" vs/Xamarin.Windows.VisualStudio.Vsix/Xamarin.Windows.VisualStudio.Vsix.csproj '/t:GenerateMSBuildItems;GenerateReferenceAssembliesItems' || exit $?
"$MSBUILD_EXE" vs/Xamarin.Windows.VisualStudio.Vsix/Xamarin.Windows.VisualStudio.Vsix.csproj /p:ZipPackageCompressionLevel=Normal '/t:Clean;Build' || exit $?

rm -rf "$BUILD_DIR/artifacts"
mkdir -p "$BUILD_DIR/artifacts"
echo "Copying build artifacts to '$(cygpath -w "$BUILD_DIR/artifacts")'..."
cp vs/Xamarin.Windows.VisualStudio.Vsix/bin/Debug/Xamarin.Windows.VisualStudio*.vsix "$BUILD_DIR/artifacts/Xamarin.Windows.VisualStudio.$XAMARIN_WINDOWS_FULL_VERSION.vsix"

echo "Xamarin Windows $XAMARIN_WINDOWS_FULL_VERSION built successfully!"

popd > /dev/null

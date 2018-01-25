Xamarin Windows
===============

Goals for this project:

 * Provide MSBuild and VS tooling for building, launching, debugging Windows
   x64 full AOT binaries.
 * Provide reusable MSBuild tasks and targets for other platforms which also
   compile down to full AOT binaries, e.g. Xbox One and PS4.

# Build instructions

In order to build this project you'll need:

* **Git for Windows**
* **Visual Studio 2017**
* **Clang2** 
* **Windows 8.1 SDK and all Windows 10 SDKs** 
You'll find Clang2 and the Windows SDKs in the Visual Studio 2017 setup, under the "Individual components" tab.
* **Mono for Windows** release


**NOTE**: Windows has a problem with long (> 260 characters) paths. To avoid
any issues related to this limitation when building this project you should
make sure to checkout this repository in a shallow path on your system. E.g.
to `C:\xw`:

```
cd C:\
git clone git@github.com:xamarin/xamarin-windows.git xw
cd xw
```

## Build using build.cmd

To build the project just run the `build.cmd` script.

## Manual build

When developing on this project you may prefer to run the individual steps
carried out by `build.cmd` manually.

For a manual build you will need Cygwin installed. Consult the [Compiling Mono
on Windows](http://www.mono-project.com/docs/compiling-mono/windows/)
documentation for more details on the prerequisites.

The commands below asumes Visual Studio 2017 **Enterprise** is installed. This is **not required**.
Adjust the paths to Visual Studio according to the specific flavor of your Visual Studio 2017 installation.

### Fetch submodules

```
cd C:\xw
git submodule update --init --recursive
```

### Build the mono winaot BCL profile

By default a mono checkout is expected in `mono/external`. Create a
`Xamarin.Windows.Override.props` file in the root of the `xamarin-windows`
checkout with a `MonoDevRoot` property to override the path to the mono
checkout.

**NOTE**: A specific mono commit is expected to be checked out. Check the
`MonoCommit` property in the `Xamarin.Windows.props` file.

Run from within cygwin:
```bash
cd path/to/mono
git checkout <commit>
./autogen.sh --host=x86_64-w64-mingw32 --disable-boehm --with-runtime_preset=winaot
export MONO_EXECUTABLE="`cygpath -u -a msvc\\\build\\\sgen\\\x64\\\bin\\\Release\\\mono-sgen.exe`"
export VC_ROOT="/cygdrive/c/Program Files (x86)/Microsoft Visual Studio/2017/Enterprise/VC"
export CLANG2_VERSION=$(sed -n 1p "$VC_ROOT/Auxiliary/Build/Microsoft.ClangC2Version.default.txt" | sed 's/\s//g')
export VCTOOLS_VERSION=$(sed -n 1p "$VC_ROOT/Auxiliary/Build/Microsoft.VCToolsVersion.default.txt" | sed 's/\s//g')
export PATH="$VC_ROOT/Tools/ClangC2/$CLANG2_VERSION/bin/HostX64":"$VC_ROOT/Tools/MSVC/$VCTOOLS_VERSION/bin/HostX64/x64":$PATH
/cygdrive/c/Program\ Files\ \(x86\)/Microsoft\ Visual\ Studio/2017/Enterprise/MSBuild/15.0/Bin/MSBuild.exe /p:PlatformToolset=v140 /p:Platform=x64 /p:Configuration=Release /p:MONO_TARGET_GC=sgen msvc/mono.sln
make -C mcs/
```

### Copy reference assemblies to the build folder

The VS extension relies on the Mono BCL to be present in the
`build/ReferenceAssemblies/` folder. Run the following to copy the relevant
files:
```
cd C:\xw
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" msbuild\CopyReferenceAssembliesFromMonoDevRoot.proj
```

### Create the MSBuild toolchain in the build folder

The VS extension relies on the MSBuild files and the mono installation being
installed in `build/MSBuild/`. Run the following to copy the relevant files:
```
cd C:\xw
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" msbuild\CreateToolchain.proj
```

### Build the MSBuild tasks and VS extension

You should now be able to open the `Xamarin.Windows.sln` solution and build
it.


### Debugging the VS extension

**NOTE!** For now, Xamarin needs to be installed in Visual Studio. Make sure
to enable that in the Visual Studio setup.

At this time VS extensions with embedded MSBuild files or Reference Assemblies
are not properly deployed to the experimental instance. We need to setup
symbolic linkes to make sure the VS process correctly loads the MSBuild and
Reference Assemblies from the `$MSBuild` and `$ReferenceAssemblies` folders
located inside the extension folder in the experimental instance folder. The
`mk-vs-experimental-links.bat` script in the root of the source tree creates
these links (must be run as Administrator):

```
cd C:\xw
mk-vs-experimental-links.bat
```

It should now be possible to debug the extension in VS. **NOTE!** These links
have to be recreated whenever the VS extension's verison number changes. The
version number is based on the number of commits so make sure to run the
script after new files have been committed or pulled.


# Converting an ordinary C# project to a Xamarin.Windows project

Edit the `.csproj` file to make it a Xamarin.Windows project. The line

```xml
<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
```

must be changed to 

```xml
<Import Project="$(MSBuildExtensionsPath)\Xamarin\Windows\Xamarin.Windows.CSharp.targets" />
```

Then add the line

```xml
<ProjectTypeGuids>{8F3E2DF0-C35C-4265-82FC-BEA011F4A7ED};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
```

to the first `<ProjectGroup>` section in the `.csproj` file.


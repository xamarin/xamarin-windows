Xamarin Windows
===============

Goals for this project:

 * Provide MSBuild and VS tooling for building, launching, debugging Windows
   x64 full AOT binaries.
 * Provide reusable MSBuild tasks and targets for other platforms which also
   compile down to full AOT binaries, e.g. Xbox One and PS4.

# Build instructions

You will need Cygwin, Visual Studio 2015 and a Mono for Windows release
installed in order to build this project. Consult the [Compiling Mono on
Windows](http://www.mono-project.com/docs/compiling-mono/windows/)
documentation for more details on the prerequisites.

**NOTE**: Windows has a problem with long (> 260 characters) paths. To avoid
any issues related to this limitation when building this project you should
make sure to checkout this repository in a shallow path on your system. E.g.
to `C:\xw`:

```bash
cd /cygdrive/c/
git clone git@github.com:xamarin/xamarin-windows.git xw
cd xw/
```

## Fetch submodules

```bash
git submodule update --init --recursive
```

## Build the mono winaot BCL profile

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
export VC_ROOT="/cygdrive/c/Program Files (x86)/Microsoft Visual Studio 14.0/VC"
export PATH="$VC_ROOT/ClangC2/bin/amd64:$VC_ROOT/bin/amd64":$PATH
/cygdrive/c/Program\ Files\ \(x86\)/MSBuild/14.0/Bin/MSBuild.exe /p:PlatformToolset=v140 /p:Platform=x64 /p:Configuration=Release /p:MONO_TARGET_GC=sgen msvc/mono.sln
make -C mcs/
```

## Copy reference assemblies

**NOTE**: Visual Studio typically locks the files which will be copied here.
You may have to close VS before running this command.

Run the following in an Administrator `cmd.exe`:
```
cd C:\xw
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" msbuild\CopyReferenceAssembliesFromMonoDevRoot.proj
```

## Create the MSBuild toolchain

The VS extension relies on the MSBuild files and the mono installation being
installed under `C:\Program Files (x86)\MSBuild\Xamarin\Windows`.

Run the following in an Administrator `cmd.exe`:
```
cd C:\xw
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" msbuild\CreateToolchain.proj
```

This step has to be repeated whenever you change the MSBuild files or tasks
and want the changes to be picked up by the VS extension.

## Build the Xamarin.Windows solution

You should now be able to open the `Xamarin.Windows.sln` solution and build
it.

# Usage

**NOTE!** For now, Xamarin needs to be installed in Visual Studio. Make sure to enable that in the Visual Studio setup.

Launch the Xamarin.Windows.VisualStudio.Vsix project from within VS. Look for
the Xamarin Windows templates when creating new projects.

To run a full AOT compile of a `.csproj` from the command-line run:

```
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" /v:Detailed /t:BuildNative <csproj-file>
```

The native `.exe` should now be in the project's `bin/Debug/Native/` folder.


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


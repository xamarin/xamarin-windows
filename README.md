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

## Build `external/mono`

```bash
cd external/mono/
./autogen.sh --host=x86_64-w64-mingw32 --disable-boehm
export MONO_EXECUTABLE="`cygpath -u -a msvc\\\build\\\sgen\\\x64\\\bin\\\Release\\\mono-sgen.exe`"
/cygdrive/c/Program\ Files\ \(x86\)/MSBuild/14.0/Bin/MSBuild.exe /p:PlatformToolset=v140 /p:Platform=x64 /p:Configuration=Release /p:MONO_TARGET_GC=sgen msvc/mono.sln
make
```

## Copy reference assemblies

**NOTE**: Visual Studio typically locks the files which will be copied here.
You may have to close VS before running this command.

Run the following in an Administrator `cmd.exe`:
```
cd C:\xw
"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" /p:MonoDevRoot=%cd%\external\mono msbuild\CopyReferenceAssembliesFromMonoDevRoot.proj
```

## Build the MSBuild tasks

You should now be able to open the `Xamarin.Windows.sln` solution. Build it
and try running the tests.

# Usage

For now one has to edit the `.csproj` file to make it a Xamarin.Windows
project. The line

```xml
<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
```

must be changed to 

```xml
<Import Project="$(PathToXamarinWindows)\Xamarin.Windows.CSharp.targets" />
```

To run a full AOT compile of a `.csproj` run:

```
MSBuild.exe /p:MonoDevRoot=C:\xw\external\mono /p:PathToXamarinWindows=C:\xw\msbuild /v:Detailed <csproj-file>
```

The `.exe` should now be in the project's `bin/Debug/Native/` folder.

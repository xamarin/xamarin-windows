@echo off

setlocal ENABLEDELAYEDEXPANSION

set BASE_DIR=%~dp0
set BUILD_TOOLS_DIR=%BASE_DIR%\buildtools
set CYGWIN_DIR=%BUILD_TOOLS_DIR%\cygwin
set BASH_EXE="%CYGWIN_DIR%\bin\bash.exe"

pushd %BASE_DIR%

call fetch-build-tools.cmd

set PATH=%PATH%;%CYGWIN_DIR%\bin
set SHELLOPTS=igncr
%BASH_EXE% --login -c "`cygpath -u '%BASE_DIR%'`/build.sh" 2>&1
set RESULT=!ERRORLEVEL!

popd

if not %RESULT% == 0 (
	echo "Build failed with code %RESULT%!" 1>&2
)
exit /b %RESULT%

endlocal

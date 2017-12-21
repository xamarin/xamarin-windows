@echo off

setlocal ENABLEDELAYEDEXPANSION

set BASE_DIR=%~dp0
set BUILD_TOOLS_DIR=%BASE_DIR%\buildtools
set CYGWIN_DIR=%BUILD_TOOLS_DIR%\cygwin
set CYGWIN_SETUP_EXE="%CYGWIN_DIR%\setup-x86_64.exe"
set CYGWIN_MIRROR=http://www.mirrorservice.org/sites/sourceware.org/pub/cygwin/
set PACKAGES=autoconf,automake,make,libtool,gcc-core,gcc-g++,mingw64-x86_64-runtime,mingw64-x86_64-gcc-g++,wget,zip
set BASH_EXE="%CYGWIN_DIR%\bin\bash.exe"
set NUGET_EXE="%BUILD_TOOLS_DIR%\nuget.exe"
set PATH=%CYGWIN_DIR%\bin;%PATH%

pushd %BASE_DIR%

if not exist %CYGWIN_DIR% (
    md %CYGWIN_DIR%
)

if not exist %CYGWIN_SETUP_EXE% (
    echo Downloading %CYGWIN_SETUP_EXE%...
    powershell -Command "(new-object System.Net.WebClient).Downloadfile('https://cygwin.com/setup-x86_64.exe', '%CYGWIN_SETUP_EXE%')"
    echo Download finished.
)

if not exist %BASH_EXE% (
    echo Cygwin not installed. Running cygwin setup...
    %CYGWIN_SETUP_EXE% -s %CYGWIN_MIRROR% -n --no-admin -R %CYGWIN_DIR% -l %CYGWIN_DIR%\packages -q -P %PACKAGES%
    :: Make sure the home folder for the current user is created
    %BASH_EXE% --login -c "exit"
    echo Setup finished.
) else (
    :: Check whether all required packages are installed. If not we need to rerun the cygwin setup.
    %BASH_EXE% -c "cygcheck -c -d | tail -n +3 | cut -d' ' -f1 | sort" > "%CYGWIN_DIR%"\installed_packages
    %BASH_EXE% -c "echo '%PACKAGES%' | sed 's/,/\n/g' | sort" > "%CYGWIN_DIR%"\required_packages
    %BASH_EXE% -c "if [[ `comm -23 buildtools/cygwin/required_packages buildtools/cygwin/installed_packages` ]]; then exit 1; else exit 0; fi"
    if ERRORLEVEL 1 (
        echo Outdated cygwin install. Running cygwin setup...
        %CYGWIN_SETUP_EXE% -n --no-admin -R %CYGWIN_DIR% -l %CYGWIN_DIR%\packages -q -P %PACKAGES%
        echo Setup finished.
    )
)

if not exist %NUGET_EXE% (
    echo Downloading %NUGET_EXE%...
    powershell -Command "(new-object System.Net.WebClient).Downloadfile('https://dist.nuget.org/win-x86-commandline/v4.1.0/nuget.exe', '%NUGET_EXE%')"
    echo Download finished.
)

popd

endlocal

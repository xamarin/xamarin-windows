@echo off

setlocal ENABLEDELAYEDEXPANSION

set VS_VERSION=15
set EXT_NAME=Xamarin SDK for Windows
set MSBUILD_SUB_DIR=Xamarin\Windows
set REF_ASSEMBLIES_SUB_DIR=Xamarin.Windows
set VSWHERE=C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe
set VS_APPDATA_DIR=%USERPROFILE%\AppData\Local\Microsoft\VisualStudio

if not exist "%VSWHERE%" (
  echo vswhere.exe could not be found. Is Visual Studio 2017 installed?
  exit /B 1
)

for /F "tokens=* USEBACKQ" %%F IN (`"%VSWHERE%" -property installationPath`) do (
  set VSINSTALLPATH=%%F
)
if not exist "%VSINSTALLPATH%" (
  echo No Visual Studio installation found
  exit /B 1
)
echo Found Visual Studio installation: '%VSINSTALLPATH%'

for /D %%F IN (%VS_APPDATA_DIR%\%VS_VERSION%*Exp) do (
 set EXP_DIR=%%F
 goto exp_dir_found
)
echo No experimental instance dir found in '%VS_APPDATA_DIR%'
exit /B 1

:exp_dir_found
echo Found experimental instance dir '%EXP_DIR%'

set EXT_BASE_DIR=%EXP_DIR%\Extensions\Xamarin\%EXT_NAME%

for /D %%F IN ("%EXT_BASE_DIR%"\*) do (
 set EXT_DIR=%%F
 goto version_found
)
echo No '%EXT_NAME%' extension found in '%EXT_BASE_DIR%'
exit /B 1

:version_found
echo Found extension dir '%EXT_DIR%'

set TARGET=%EXT_DIR%\$MSBuild\%MSBUILD_SUB_DIR%
set LINK=%VSINSTALLPATH%\MSBuild\%MSBUILD_SUB_DIR%
echo Linking '%LINK%' to '%TARGET%'
if exist "%LINK%" (
  rmdir "%LINK%"
)
mklink /D "%LINK%" "%TARGET%"

set TARGET=%EXT_DIR%\$ReferenceAssemblies\Microsoft\Framework\%REF_ASSEMBLIES_SUB_DIR%
set LINK=%VSINSTALLPATH%\Common7\IDE\ReferenceAssemblies\Microsoft\Framework\%REF_ASSEMBLIES_SUB_DIR%
echo Linking '%LINK%' to '%TARGET%'
if exist "%LINK%" (
  rmdir "%LINK%"
)
mklink /D "%LINK%" "%TARGET%"

endlocal

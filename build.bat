@echo off
chcp 1252
if "%VisualStudioVersion%"=="" call "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat"
if "%VisualStudioVersion%"=="" call "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\VsDevCmd.bat"
if "%VisualStudioVersion%"=="" call "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\Common7\Tools\VsDevCmd.bat"

msbuild /t:Clean
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet restore
if %errorlevel% neq 0 exit /b %errorlevel%

msbuild /p:Configuration=Release /p:Platform="Any CPU"
if %errorlevel% neq 0 exit /b %errorlevel%

msbuild /p:Configuration=Debug /p:Platform="Any CPU"
if %errorlevel% neq 0 exit /b %errorlevel%

rem msbuild /p:Configuration=Release /p:Platform="Any CPU" documentation.shfbproj
rem if %errorlevel% neq 0 exit /b %errorlevel%

for /r %%f in ("Test*.exe") do (
  vstest.console "%%f"
  if %errorlevel% neq 0 exit /b %errorlevel%
)

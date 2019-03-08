@echo off
chcp 1252
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\WDExpress\Common7\Tools\VsDevCmd.bat" 2> nul

dotnet restore
if %errorlevel% neq 0 exit /b %errorlevel%

msbuild /p:Configuration=Release /p:Platform="Any CPU"
if %errorlevel% neq 0 exit /b %errorlevel%

msbuild /p:Configuration=Debug /p:Platform="Any CPU"
if %errorlevel% neq 0 exit /b %errorlevel%

rem msbuild /p:Configuration=Release /p:Platform="Any CPU" documentation.shfbproj
rem if %errorlevel% neq 0 exit /b %errorlevel%

Test\bin\Release\net45\Test.exe

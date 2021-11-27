@echo off
chcp 1252
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\WDExpress\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\BuildTools\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\Common7\Tools\VsDevCmd.bat" 2> nul
if "%VisualStudioVersion%"=="" call "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\WDExpress\Common7\Tools\VsDevCmd.bat" 2> nul

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

vstest.console Test\bin\Release\net5.0\Test.dll
vstest.console Test\bin\Release\netcoreapp2.1\Test.dll
vstest.console Test\bin\Release\netcoreapp3.1\Test.dll
vstest.console Test\bin\Release\net20\Test.exe
vstest.console Test\bin\Release\net35\Test.exe
vstest.console Test\bin\Release\net40\Test.exe
vstest.console Test\bin\Release\net45\Test.exe
vstest.console Test\bin\Release\net46\Test.exe
vstest.console Test\bin\Release\net47\Test.exe
vstest.console Test\bin\Release\net48\Test.exe

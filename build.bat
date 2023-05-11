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

dotnet run Tests\bin\Release\netcoreapp1.0\Tests.dll
dotnet run Tests\bin\Release\netcoreapp1.1\Tests.dll
dotnet run Tests\bin\Release\netcoreapp2.0\Tests.dll
dotnet run Tests\bin\Release\netcoreapp2.1\Tests.dll
dotnet run Tests\bin\Release\netcoreapp3.0\Tests.dll
dotnet run Tests\bin\Release\netcoreapp3.1\Tests.dll
Tests\bin\Release\net20\Tests.exe
Tests\bin\Release\net35\Tests.exe
Tests\bin\Release\net40\Tests.exe
Tests\bin\Release\net45\Tests.exe
Tests\bin\Release\net50\Tests.exe
Tests\bin\Release\net60\Tests.exe
Tests\bin\Release\net70\Tests.exe

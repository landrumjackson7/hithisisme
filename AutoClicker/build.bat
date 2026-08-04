@echo off
REM Builds AstryxAutoClicker.exe without needing the .NET SDK or Visual Studio.
REM Uses the C# compiler that ships with the .NET Framework 4.x runtime.
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Could not find csc.exe under %WINDIR%\Microsoft.NET. Install .NET Framework 4.8 or build with: dotnet build AutoClicker.csproj -c Release
  exit /b 1
)

"%CSC%" /nologo /target:winexe /out:AstryxAutoClicker.exe ^
  /reference:System.dll /reference:System.Core.dll ^
  /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
  "%~dp0Program.cs" "%~dp0MainForm.cs" "%~dp0ClickEngine.cs" "%~dp0NativeMethods.cs"

if errorlevel 1 (
  echo Build failed.
  exit /b 1
)
echo Built AstryxAutoClicker.exe
endlocal

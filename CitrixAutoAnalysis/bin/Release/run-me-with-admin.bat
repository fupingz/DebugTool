@echo off

cd /d %~dp0
set /p first="Input 1 to install the service, and 2 to unstall(default 1):"

if %first% == 2 (
  InstallUtil.exe /uninstall CitrixAutoAnalysis.exe
) else (
	  
  if not exist "C:\\CAD2\\Patterns" md C:\CAD2\Patterns
	xcopy .\Patterns\*.xml C:\CAD2\Patterns\ /y
	
  InstallUtil.exe /install CitrixAutoAnalysis.exe
)

set /p first="Press any key to complete the install/uninstall"
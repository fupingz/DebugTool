@echo off

cd /d %~dp0
set /p first="Input 1 to install the service, and 2 to unstall(default 1):"

if not exist "C:\\CAD\\Engine" md C:\CAD\Engine

xcopy CitrixAutoAnalysis.exe C:\CAD\Engine\ /y
xcopy CitrixAutoAnalysis.pdb C:\CAD\Engine\ /y
xcopy CitrixAutoAnalysis.exe.config C:\CAD\Engine\ /y

if %first% == 2 (
  InstallUtil.exe /uninstall C:\CAD\Engine\CitrixAutoAnalysis.exe
) else (
	  
  if not exist "C:\\CAD\\Patterns" md C:\CAD\Patterns
	xcopy .\Patterns\*.xml C:\CAD\Patterns\ /y
	
  InstallUtil.exe /install C:\CAD\Engine\CitrixAutoAnalysis.exe
)

set /p first="Press any key to complete the install/uninstall"
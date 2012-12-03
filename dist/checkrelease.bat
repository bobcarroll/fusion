@ECHO OFF

"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -q -vf ..\bin\Release\bootstrap.exe
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -q -vf ..\bin\Release\fuse.exe
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -q -vf ..\bin\Release\fusion-config.exe
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -q -vf ..\bin\Release\lib7ztasks.dll
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -q -vf ..\bin\Release\libfusion.dll
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -q -vf ..\bin\Release\libfusiontasks.dll
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -q -vf ..\bin\Release\libmsitasks.dll
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -q -vf ..\bin\Release\sandbox.exe

pause
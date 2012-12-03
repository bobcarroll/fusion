@ECHO OFF

"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Release\bootstrap.exe rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Release\fuse.exe rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Release\fusion-config.exe rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Release\lib7ztasks.dll rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Release\libfusion.dll rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Release\libfusiontasks.dll rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Release\libmsitasks.dll rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Release\sandbox.exe rcarz.snk

pause
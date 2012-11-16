@ECHO OFF

"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Debug\bootstrap.exe rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Debug\fuse.exe rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Debug\fusion-config.exe rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Debug\lib7ztasks.dll rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Debug\libfusion.dll rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Debug\libfusiontasks.dll rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Debug\libmsitasks.dll rcarz.snk
"%ProgramFiles(x86)%\Microsoft SDKs\Windows\v7.0A\Bin\x64\sn.exe" -R ..\bin\Debug\xtmake.exe rcarz.snk

pause
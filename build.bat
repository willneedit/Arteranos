@echo off

set projectpath=%cd%
set unity="D:\Unity_Editors\2021.3.15f1\Editor\Unity.exe"

rmdir /s /q build

rmdir /s /q Library\ScriptAssemblies
rmdir /s /q Library\BurstCache

%unity% -quit -batchmode -projectpath %projectpath% -executeMethod Arteranos.BuildPlayers.BuildWin64DedServ -logFile build\build64DedServ.log

rmdir /s /q Library\ScriptAssemblies
rmdir /s /q Library\BurstCache

%unity% -quit -batchmode -projectpath %projectpath% -executeMethod Arteranos.BuildPlayers.BuildWin64 -logFile build\build64.log

rmdir /s /q Library\ScriptAssemblies
rmdir /s /q Library\BurstCache

"%wix%"\bin\heat dir Win64 -out Win64.wxi -scom -sfrag -sreg -svb6 -ag -dr INSTALLDIR -cg Pack_Win64 -var var.BinDir

"%wix%"\bin\heat dir Win64-Server -out Win64-Server.wxi -scom -sfrag -sreg -svb6 -ag -dr INSTALLDIR -cg Pack_Win64_Server -var var.SrvBinDir

"%wix%"\bin\candle ..\Setup\Main.wxs Win64-Server.wxi Win64.wxi -dBinDir=Win64 -dSrvBinDir=Win64-Server -arch x64

"%wix%"\bin\light Main.wixobj Win64.wixobj Win64-Server.wixobj -ext WixUIExtension -o Arteranos

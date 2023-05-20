@echo off

set projectpath=%cd%
set unity="D:\Unity_Editors\2021.3.15f1\Editor\Unity.exe"

rmdir /s /q build

:: rmdir /s /q Library\ScriptAssemblies
:: rmdir /s /q Library\BurstCache

%unity% -quit -batchmode -projectpath %projectpath% -executeMethod Arteranos.BuildPlayers.BuildWin64DedServ -logFile build\build64DedServ.log

:: rmdir /s /q Library\ScriptAssemblies
:: rmdir /s /q Library\BurstCache

%unity% -quit -batchmode -projectpath %projectpath% -executeMethod Arteranos.BuildPlayers.BuildWin64 -logFile build\build64.log

:: rmdir /s /q Library\ScriptAssemblies
:: rmdir /s /q Library\BurstCache

cd build

move Win64\Arteranos_BurstDebugInformation_DoNotShip .
move Win64-Server\Arteranos-Server_BurstDebugInformation_DoNotShip .

move Win64\Arteranos.exe .

"%wix%"\bin\heat dir Win64 -out Win64.wxi -scom -sfrag -sreg -svb6 -ag -dr AppDir -cg Pack_Win64 -srd -var var.BinDir

"%wix%"\bin\heat dir Win64-Server -out Win64-Server.wxi -scom -sfrag -sreg -svb6 -ag -dr ServerDir -cg Pack_Win64_Server -srd -var var.SrvBinDir

"%wix%"\bin\candle ..\Setup\Main.wxs Win64-Server.wxi Win64.wxi -dBinDir=Win64 -dSrvBinDir=Win64-Server -arch x64

"%wix%"\bin\light Main.wixobj Win64.wixobj Win64-Server.wixobj -ext WixUIExtension -o ArteranosSetup

move Arteranos.exe Win64

"%wix%"\bin\candle.exe -ext WixNetFxExtension -ext WixBalExtension -ext WixUtilExtension ..\Setup\MainBurn.wxs

"%wix%"\bin\light.exe -ext WixNetFxExtension -ext WixBalExtension -ext WixUtilExtension MainBurn.wixobj -o ArteranosSetup


cd ..

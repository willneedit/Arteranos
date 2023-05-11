@echo off

set projectpath=%cd%
set unity="D:\Unity_Editors\2021.3.15f1\Editor\Unity.exe"

rmdir /s /q build

rmdir /s /q Library\ScriptAssemblies
rmdir /s /q Library\BurstCache

%unity% -quit -batchmode -projectpath %projectpath% -executeMethod Arteranos.BuildPlayers.BuildWin64DedServ -logFile build\build64DedServ.log

rmdir /s /q Library\ScriptAssemblies
rmdir /s /q Library\BurstCache

%unity% -quit -batchmode -projectpath %projectpath% -executeMethod Arteranos.BuildPlayers.BuildWin64DedServ -logFile build\build64DedServ.log

rmdir /s /q Library\ScriptAssemblies
rmdir /s /q Library\BurstCache

%unity% -quit -batchmode -projectpath %projectpath% -executeMethod Arteranos.BuildPlayers.BuildWin64 -logFile build\build64.log

rmdir /s /q Library\ScriptAssemblies
rmdir /s /q Library\BurstCache

%unity% -quit -batchmode -projectpath %projectpath% -executeMethod Arteranos.BuildPlayers.BuildWin64 -logFile build\build64.log

rmdir /s /q Library\ScriptAssemblies
rmdir /s /q Library\BurstCache


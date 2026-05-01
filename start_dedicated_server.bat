@echo off
setlocal

set "PROJECT_FILE=%~dp0doom_port.sbproj"
set "SERVER_TITLE=Akuji Doom Port Dedicated Server"
set "SERVER_HOSTNAME=Akuji Doom Port Dedicated"
set "SERVER_TOKEN=%SBOX_SERVER_TOKEN%"
set "SERVER_EXE="
set "SERVER_EXE_X86=C:\Program Files (x86)\Steam\steamapps\common\sbox\sbox-server.exe"
set "SERVER_EXE_X64=C:\Program Files\Steam\steamapps\common\sbox\sbox-server.exe"

if exist "%SERVER_EXE_X86%" goto use_x86
if exist "%SERVER_EXE_X64%" goto use_x64
goto missing

:use_x86
set "SERVER_EXE=%SERVER_EXE_X86%"
goto launch

:use_x64
set "SERVER_EXE=%SERVER_EXE_X64%"
goto launch

:missing
echo Could not find sbox-server.exe in the default Steam install paths.
echo Expected one of:
echo   %SERVER_EXE_X86%
echo   %SERVER_EXE_X64%
echo.
echo Update start_dedicated_server.bat with your real sbox-server.exe path and try again.
pause
exit /b 1

:launch
echo Starting dedicated server for "%PROJECT_FILE%"
echo Using executable: %SERVER_EXE%
echo Hostname: %SERVER_HOSTNAME%
if defined SERVER_TOKEN (
echo Using game server token from SBOX_SERVER_TOKEN.
) else (
echo No game server token configured. Set SBOX_SERVER_TOKEN to give the server a stable public identity.
)
echo.
if defined SERVER_TOKEN (
start "%SERVER_TITLE%" "%SERVER_EXE%" +game "%PROJECT_FILE%" +hostname "%SERVER_HOSTNAME%" +net_game_server_token "%SERVER_TOKEN%"
) else (
start "%SERVER_TITLE%" "%SERVER_EXE%" +game "%PROJECT_FILE%" +hostname "%SERVER_HOSTNAME%"
)
exit /b 0

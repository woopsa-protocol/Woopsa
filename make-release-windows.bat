REM Woopsa make-release-windows script
REM This script will make a complete release (Woopsa.NET, JavaScript as well as examples) on Windows
REM It will first make a build and then copy the files to the adequate directories
REM Make sure you run it from its own directory

REM Some useful variables
set RELEASE_DIR=Release
set DOT_NET_RELEASE_DIR=DotNet
set DOT_NET_PROJECT_DIR=Woopsa
set DOT_NET_SERVER_EXAMPLE_DIR=WoopsaDemoServer
set DOT_NET_CLIENT_EXAMPLE_DIR=WoopsaDemoClient
set JAVASCRIPT_RELEASE_DIR=JavaScript
set JAVASCRIPT_PROJECT_DIR=WoopsaWeb

REM .NET Library
if not exist %RELEASE_DIR% mkdir %RELEASE_DIR%
if not exist %RELEASE_DIR%\%DOT_NET_RELEASE_DIR% mkdir %RELEASE_DIR%\%DOT_NET_RELEASE_DIR%
cd %DOT_NET_PROJECT_DIR%
call build-windows.bat
cd ..
copy %DOT_NET_PROJECT_DIR%\bin\Release\* %RELEASE_DIR%\%DOT_NET_RELEASE_DIR%\

REM .NET Demo Server
cd %DOT_NET_SERVER_EXAMPLE_DIR%
call build-windows.bat
cd ..
copy %DOT_NET_SERVER_EXAMPLE_DIR%\bin\Release\WoopsaDemoServer.exe %RELEASE_DIR%\%DOT_NET_RELEASE_DIR%\

REM .NET Demo Client
cd %DOT_NET_CLIENT_EXAMPLE_DIR%
call build-windows.bat
cd ..
copy %DOT_NET_CLIENT_EXAMPLE_DIR%\bin\Release\WoopsaDemoClient.exe %RELEASE_DIR%\%DOT_NET_RELEASE_DIR%\

REM JavaScript library
if not exist %RELEASE_DIR%\%JAVASCRIPT_RELEASE_DIR% mkdir %RELEASE_DIR%\%JAVASCRIPT_RELEASE_DIR%
cd %JAVASCRIPT_PROJECT_DIR%
call build-windows.bat
cd ..
copy %JAVASCRIPT_PROJECT_DIR%\dist\* %RELEASE_DIR%\%JAVASCRIPT_RELEASE_DIR%\
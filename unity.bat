@echo off
chcp 65001 >nul
REM Spirit Merge - Unity CLI Helper (CMD/Batch)
REM Usage: unity.bat [command]

set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe
set PROJECT_PATH=C:\Users\user\Spiritia
set LOG_FILE=Logs\unity-cli.log

if not exist Logs mkdir Logs

if "%1"=="build-webgl" goto build_webgl
if "%1"=="build-windows" goto build_windows
if "%1"=="setup" goto setup
if "%1"=="log" goto log
if "%1"=="open" goto open
if "%1"=="clean" goto clean
if "%1"=="method" goto method
goto help

:build_webgl
echo 🎮 Building WebGL...
"%UNITY_PATH%" -batchmode -quit -projectPath "%PROJECT_PATH%" -buildTarget WebGL -logFile "%LOG_FILE%" -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"
echo ✅ Build complete!
goto end

:build_windows
echo 🪟 Building Windows...
"%UNITY_PATH%" -batchmode -quit -projectPath "%PROJECT_PATH%" -buildTarget Win64 -logFile "%LOG_FILE%" -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"
echo ✅ Build complete!
goto end

:setup
echo 🔧 Running project setup...
"%UNITY_PATH%" -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "%LOG_FILE%" -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"
echo ✅ Setup complete!
goto end

:log
echo 📋 Last build log:
type "%LOG_FILE%" 2>nul | findstr /n "." | more
if errorlevel 1 echo No log file found
goto end

:open
echo 🚀 Opening Unity Editor...
start "" "%UNITY_PATH%" -projectPath "%PROJECT_PATH%"
goto end

:clean
echo 🧹 Cleaning Library/Temp...
if exist Library rmdir /s /q Library
if exist Temp rmdir /s /q Temp
echo ✅ Cleaned! Open Unity to regenerate.
goto end

:method
if "%2"=="" (
    echo Usage: unity.bat method ^<Class.Method^>
    goto end
)
echo ⚡ Executing: %2
"%UNITY_PATH%" -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "%LOG_FILE%" -executeMethod "%2%3%4%5%6%7%8%9"
echo ✅ Done!
goto end

:help
echo Spirit Merge - Unity CLI Helper
echo.
echo Usage: unity.bat [command]
echo.
echo Commands:
echo   build-webgl      Build for WebGL
echo   build-windows    Build for Windows
echo   setup            Initialize project (scenes, prefabs)
echo   log              Show last build log
echo   open             Open project in Unity Editor
echo   clean            Delete Library/Temp
echo   method ^<name^>    Execute custom method
echo.

:end

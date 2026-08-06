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
if "%1"=="close" goto close
if "%1"=="clean" goto clean
if "%1"=="method" goto method
if "%1"=="verify" goto verify
if "%1"=="test" goto test
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

:verify
echo 🔍 Batchmode 컴파일 검증 중...
tasklist /FI "IMAGENAME eq Unity.exe" 2>nul | findstr /I "Unity.exe" >nul
if not errorlevel 1 (
    echo ❌ Unity Editor가 실행 중입니다. unity.bat close로 종료 후 다시 실행하세요.
    goto end
)
"%UNITY_PATH%" -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "Logs\compile-check.log" -nographics
if exist "Logs\compile-check.log" (
    findstr /C:"error CS" "Logs\compile-check.log" | findstr /C:"Assets/Scripts" > "Logs\compile-errors.tmp" 2>nul
    if not errorlevel 1 (
        echo ❌ 컴파일 에러 발견!
        type "Logs\compile-errors.tmp"
        del "Logs\compile-errors.tmp" 2>nul
        goto end
    )
    del "Logs\compile-errors.tmp" 2>nul
)
echo ✅ 컴파일 성공 (에러 0개)
goto end

:test
echo 🔍 하이브리드 테스트 세션 시작...
tasklist /FI "IMAGENAME eq Unity.exe" 2>nul | findstr /I "Unity.exe" >nul
if not errorlevel 1 (
    echo ℹ️ Unity 이미 실행 중 - 컴파일 검증 건너뜀
) else (
    echo 🔍 배치 컴파일 검증...
    "%UNITY_PATH%" -batchmode -quit -projectPath "%PROJECT_PATH%" -logFile "Logs\compile-check.log" -nographics
    if exist "Logs\compile-check.log" (
        findstr /C:"error CS" "Logs\compile-check.log" | findstr /C:"Assets/Scripts" > "Logs\compile-errors.tmp" 2>nul
        if not errorlevel 1 (
            echo ❌ 컴파일 에러 발견 - 테스트 중단
            type "Logs\compile-errors.tmp"
            del "Logs\compile-errors.tmp" 2>nul
            goto end
        )
        del "Logs\compile-errors.tmp" 2>nul
    )
    echo ✅ 컴파일 검증 통과
)
echo 🚀 Unity Editor 열기...
start "" "%UNITY_PATH%" -projectPath "%PROJECT_PATH%"
echo ⏳ CliServer 연결 대기 권장: 파워셸에서 .\unity.ps1 test 사용
echo    또는 준비 후: python cli-client.py ping / play / exec CmdBattleStatus
goto end

:close
echo 🚫 Unity Editor 종료 중...
taskkill /F /IM Unity.exe >nul 2>&1
echo ✅ 종료 완료
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
echo   close            Force close Unity Editor
echo   clean            Delete Library/Temp
echo   method ^<name^>    Execute custom method
echo   verify           Batchmode 컴파일 검증 (Unity 종료 후 실행)
echo.

:end

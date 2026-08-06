<#
.SYNOPSIS
    Spirit Merge - Unity CLI Helper (PowerShell)
.DESCRIPTION
    Unity CLI 자동화 도우미
.PARAMETER Command
    build-webgl, build-windows, setup, log, open, clean, method
.EXAMPLE
    .\unity.ps1 build-webgl
    .\unity.ps1 setup
#>

param(
    [Parameter(Position=0)]
    [string]$Command = "help",

    [Parameter(Position=1)]
    [string]$MethodName
)

$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe"
$ProjectPath = "C:\Users\user\Spiritia"
$LogFile = "Logs/unity-cli.log"

# 로그 디렉토리 확인
New-Item -ItemType Directory -Force -Path "Logs" | Out-Null

switch ($Command) {
    "build-webgl" {
        Write-Host "🎮 Building WebGL..." -ForegroundColor Cyan
        & $UnityPath -batchmode -quit -projectPath $ProjectPath -buildTarget WebGL -logFile $LogFile -executeMethod "SpiritMerge.Editor.ProjectSetup.BuildForWebGL"
        Write-Host "✅ Build complete! (exit code: $LASTEXITCODE)" -ForegroundColor Green
    }
    "build-android" {
        Write-Host "🤖 Building Android APK..." -ForegroundColor Cyan
        & $UnityPath -batchmode -quit -projectPath $ProjectPath -buildTarget Android -logFile $LogFile -executeMethod "SpiritMerge.Editor.ProjectSetup.BuildForAndroid"
        Write-Host "✅ Build complete! (exit code: $LASTEXITCODE)" -ForegroundColor Green
    }
    "build-windows" {
        Write-Host "🪟 Building Windows..." -ForegroundColor Cyan
        & $UnityPath -batchmode -quit -projectPath $ProjectPath -buildTarget Win64 -logFile $LogFile -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"
        Write-Host "✅ Build complete! (exit code: $LASTEXITCODE)" -ForegroundColor Green
    }
    "setup" {
        Write-Host "🔧 Running project setup..." -ForegroundColor Cyan
        & $UnityPath -batchmode -quit -projectPath $ProjectPath -logFile $LogFile -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"
        Write-Host "✅ Setup complete! (exit code: $LASTEXITCODE)" -ForegroundColor Green
    }
    "log" {
        Write-Host "📋 Last build log:" -ForegroundColor Cyan
        if (Test-Path $LogFile) {
            Get-Content $LogFile -Tail 50
        } else {
            Write-Host "No log file found" -ForegroundColor Yellow
        }
    }
    "open" {
        Write-Host "🚀 Opening Unity Editor..." -ForegroundColor Cyan
        # ⭐ 인자 전달: 배열 사용 — 단일 문자열+따옴표는 PS 5.1에서 인자가 깨져
        #    Unity가 -projectPath를 못 받고 '새 프로젝트' 창이 뜨는 문제 방지
        Start-Process -FilePath $UnityPath -ArgumentList @("-projectPath", $ProjectPath)
    }
    "clean" {
        Write-Host "🧹 Cleaning Library/Temp..." -ForegroundColor Yellow
        if (Test-Path "Library") { Remove-Item -Recurse -Force "Library" }
        if (Test-Path "Temp") { Remove-Item -Recurse -Force "Temp" }
        Write-Host "✅ Cleaned! Open Unity to regenerate." -ForegroundColor Green
    }
    "method" {
        if (-not $MethodName) {
            Write-Host "Usage: .\unity.ps1 method <Class.Method>" -ForegroundColor Red
            exit 1
        }
        Write-Host "⚡ Executing: $MethodName" -ForegroundColor Cyan
        & $UnityPath -batchmode -quit -projectPath $ProjectPath -logFile $LogFile -executeMethod $MethodName
        Write-Host "✅ Done! (exit code: $LASTEXITCODE)" -ForegroundColor Green
    }
    "verify" {
        # ⭐ 배치모드 컴파일 검증 — GUI를 직접 켜지 않고 컴파일 성공/에러 확인
        # 사용 전에 반드시 Unity Editor를 종료할 것!
        $VerifyLog = "Logs/compile-check.log"
        $unityProc = Get-Process -Name "Unity" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -ne "" -and $_.Path -like "*Editor*" }
        if ($unityProc) {
            Write-Host "❌ Unity Editor가 실행 중입니다. 먼저 종료해주세요: .\unity.ps1 close" -ForegroundColor Red
            exit 1
        }
        Write-Host "🔍 배치모드 컴파일 검증 중..." -ForegroundColor Cyan
        & $UnityPath -batchmode -quit -projectPath $ProjectPath -logFile $VerifyLog -nographics
        $exitCode = $LASTEXITCODE
        $errors = @()
        if (Test-Path $VerifyLog) {
            $errors = Select-String -Path $VerifyLog -Pattern "error CS" -Encoding UTF8 |
                Where-Object { $_.Line -match "Assets/Scripts" } |
                ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
        }
        if ($errors.Count -gt 0) {
            Write-Host "❌ 컴파일 에러 $($errors.Count) 개 발견" -ForegroundColor Red
            $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
            exit 1
        }
        Write-Host "✅ 컴파일 성공 (exit code: $exitCode, 에러 0개)" -ForegroundColor Green
    }
    "close" {
        Write-Host "🚫 Unity Editor 종료 중..." -ForegroundColor Cyan
        Get-Process -Name "Unity" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -ne "" } | Stop-Process -Force
        Write-Host "✅ 종료 완료" -ForegroundColor Green
    }
    "test" {
        # ═══ 하이브리드 테스트 세션 ═══
        # 1) Unity 꺼져 있으면 → 배치 컴파일 검증 (verify)
        # 2) 검증 통과하면 → GUI 열기 + CliServer 연결 대기
        # 이후: python cli-client.py play → exec CmdXXX → cat
        $unityProc = Get-Process -Name "Unity" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowTitle -ne "" }
        if (-not $unityProc) {
            Write-Host "🔍 Unity 꺼짐 확인 — 배치 컴파일 검증 먼저 실행" -ForegroundColor Cyan
            $VerifyLog = "Logs/compile-check.log"
            & $UnityPath -batchmode -quit -projectPath $ProjectPath -logFile $VerifyLog -nographics
            $errors = @()
            if (Test-Path $VerifyLog) {
                $errors = Select-String -Path $VerifyLog -Pattern "error CS" -Encoding UTF8 |
                    Where-Object { $_.Line -match "Assets/Scripts" } |
                    ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
            }
            if ($errors.Count -gt 0) {
                Write-Host "❌ 컴파일 에러 $($errors.Count) 개 — 테스트 중단" -ForegroundColor Red
                $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
                exit 1
            }
            Write-Host "✅ 컴파일 검증 통과" -ForegroundColor Green
        }
        else {
            Write-Host "ℹ️ Unity 이미 실행 중 — 컴파일 검증 건너뜀" -ForegroundColor Yellow
        }

        Write-Host "🚀 Unity Editor 열기..." -ForegroundColor Cyan
        Start-Process -FilePath $UnityPath -ArgumentList @("-projectPath", $ProjectPath)

        Write-Host "⏳ CliServer 연결 대기 (최대 2분)..." -ForegroundColor Cyan
        for ($i = 1; $i -le 24; $i++) {
            Start-Sleep -Seconds 5
            $r = python cli-client.py ping 2>$null
            if ($r -match "pong") {
                Write-Host "✅ CliServer 연결 완료 ($($i*5))초 — 테스트 준비!" -ForegroundColor Green
                Write-Host "" -ForegroundColor Gray
                Write-Host "   다음 단계:" -ForegroundColor Gray
                Write-Host "     python cli-client.py play" -ForegroundColor Gray
                Write-Host "     python cli-client.py exec SpiritMerge.Cli.CliTestSuite.CmdBattleStatus" -ForegroundColor Gray
                Write-Host "     python cli-client.py cat system 20" -ForegroundColor Gray
                Write-Host "" -ForegroundColor Gray
                exit 0
            }
        }
        Write-Host "⚠️ CliServer 연결 실패 (2분 초과) — Unity 로드 상태 확인 필요" -ForegroundColor Red
        exit 2
    }
    default {
        Write-Host @"
Spirit Merge - Unity CLI Helper (PowerShell)

Usage: .\unity.ps1 [command]

Commands:
  build-webgl      Build for WebGL
  build-windows    Build for Windows
  setup            Initialize project (scenes, prefabs)
  log              Show last build log
  open             Open project in Unity Editor
  close            Force close Unity Editor
  test             하이브리드: 검증(배치) → GUI 열기 → CliServer 대기
  clean            Delete Library/Temp
  method <name>    Execute custom method
  verify           Batchmode 컴파일 검증 (Unity 종료 후 실행, 에러 유무 확인)
"@ -ForegroundColor White
    }
}

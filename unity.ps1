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
        & $UnityPath -batchmode -quit -projectPath $ProjectPath -buildTarget WebGL -logFile $LogFile -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"
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
        Start-Process -FilePath $UnityPath -ArgumentList "-projectPath `"$ProjectPath`""
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
  clean            Delete Library/Temp
  method <name>    Execute custom method
"@ -ForegroundColor White
    }
}

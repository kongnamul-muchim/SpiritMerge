#!/bin/bash
# Unity CLI Helper - Spirit Merge 프로젝트용
# 사용법: ./unity.sh [command]
#   exec <method>  - (NEW!) Unity 실행 중에 메서드 실행 (TCP)
#   errors         - (NEW!) 컴파일 에러 확인
#   build-webgl    - WebGL 빌드 (batchmode)
#   build-windows  - Windows 빌드 (batchmode)
#   setup          - 프로젝트 초기 셋업
#   log            - 빌드 로그 확인
#   open           - Unity 에디터 열기
#   clean          - Library/Temp 삭제
#   method <name>  - 배치모드 메서드 실행

UNITY_PATH="/c/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe"
PROJECT_PATH="C:/Users/user/Spiritia"
LOG_FILE="Logs/unity-cli.log"
CLI_CLIENT="python3 cli-client.py"

mkdir -p Logs

# ─── Unity 실행 여부 확인 ──────────────────────
unity_running() {
  tasklist 2>/dev/null | grep -q "Unity.exe" && return 0 || return 1
}

# ─── TCP 서버 연결 확인 ────────────────────────
tcp_alive() {
  $CLI_CLIENT ping 2>/dev/null | grep -q "pong|ok:"
}

case "${1:-help}" in
  exec)
    shift
    if [ -z "$1" ]; then
      echo "Usage: ./unity.sh exec <Class.Method>"
      echo "  e.g. ./unity.sh exec SpiritMerge.Editor.SpiritDataGenerator.CreateAllSpiritData"
      exit 1
    fi
    if ! unity_running; then
      echo "❌ Unity Editor가 실행 중이 아닙니다"
      exit 1
    fi
    if ! tcp_alive; then
      echo "⏳ CliServer 응답 대기 중... (Unity 컴파일 중일 수 있음)"
      sleep 3
    fi
    echo "⚡ Sending: method:$1"
    $CLI_CLIENT exec "$1"
    ;;

  errors)
    if ! unity_running; then
      echo "Unity가 실행 중이 아닙니다. batchmode로 검사합니다..."
      "$UNITY_PATH" -batchmode -quit -projectPath "$PROJECT_PATH" -logFile "$LOG_FILE" 2>&1
      if grep -q "error CS" "$LOG_FILE" 2>/dev/null; then
        echo "❌ 컴파일 에러:"
        grep "error CS" "$LOG_FILE" | sort -u
      else
        echo "✅ 에러 없음"
      fi
    else
      if tcp_alive; then
        $CLI_CLIENT errors
      else
        echo "⚠️  CliServer가 아직 응답하지 않습니다 (Unity 컴파일 중)"
        EDITOR_LOG="/c/Users/user/AppData/Local/Unity/Editor/Editor.log"
        if [ -f "$EDITOR_LOG" ]; then
          ERR=$(grep "error CS" "$EDITOR_LOG" | grep "Assets/Scripts" | sort -u)
          if [ -n "$ERR" ]; then echo "$ERR"; else echo "✅ 에러 없음"; fi
        fi
      fi
    fi
    ;;

  build-webgl)
    echo "🎮 Building WebGL..."
    "$UNITY_PATH" -batchmode -quit \
      -projectPath "$PROJECT_PATH" \
      -buildTarget WebGL \
      -logFile "$LOG_FILE" \
      -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"
    echo "✅ Build complete! (exit code: $?)"
    ;;
    
  build-windows)
    echo "🪟 Building Windows..."
    "$UNITY_PATH" -batchmode -quit \
      -projectPath "$PROJECT_PATH" \
      -buildTarget Win64 \
      -logFile "$LOG_FILE" \
      -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"
    echo "✅ Build complete! (exit code: $?)"
    ;;
    
  setup)
    echo "🔧 Running project setup..."
    "$UNITY_PATH" -batchmode -quit \
      -projectPath "$PROJECT_PATH" \
      -logFile "$LOG_FILE" \
      -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"
    echo "✅ Setup complete! (exit code: $?)"
    ;;
    
  log)
    echo "📋 Last build log:"
    tail -50 "$LOG_FILE" 2>/dev/null || echo "No log file found"
    ;;

  open)
    echo "🚀 Opening Unity Editor..."
    "$UNITY_PATH" -projectPath "$PROJECT_PATH" &
    echo "⏳ CliServer 자동 시작 대기 (5초)..."
    sleep 5
    if tcp_alive; then
      echo "✅ CliServer connected!"
    fi
    ;;
    
  clean)
    echo "🧹 Cleaning Library/Temp..."
    rm -rf Library Temp
    echo "✅ Cleaned! Open Unity to regenerate."
    ;;

  method)
    shift
    if [ -z "$1" ]; then
      echo "Usage: ./unity.sh method <Class.Method>"
      exit 1
    fi
    echo "⚡ Executing via batchmode: $1"
    "$UNITY_PATH" -batchmode -quit \
      -projectPath "$PROJECT_PATH" \
      -logFile "$LOG_FILE" \
      -executeMethod "$1"
    echo "✅ Done! (exit code: $?)"
    ;;
    
  *)
    echo "Spirit Merge - Unity CLI Helper"
    echo ""
    echo "Usage: ./unity.sh [command]"
    echo ""
    echo "=== 실시간 명령 (Unity 실행 중 필요) ==="
    echo "  exec <method>      Unity에 메서드 실행 요청 (TCP)"
    echo "  errors             컴파일 에러 확인"
    echo ""
    echo "=== 배치모드 명령 (Unity 종료 필요) ==="
    echo "  build-webgl        Build for WebGL"
    echo "  build-windows      Build for Windows"
    echo "  setup              Initialize project"
    echo "  method <name>      Execute custom method"
    echo ""
    echo "=== 기타 ==="
    echo "  log                Show build log"
    echo "  open               Open project + connect CLI"
    echo "  clean              Delete Library/Temp"
    echo ""
    if unity_running; then
      echo "🟢 Unity: RUNNING"
      if tcp_alive; then echo "🔌 CLI Server: CONNECTED"; else echo "🔌 CLI Server: WAITING..."; fi
    else
      echo "🔴 Unity: STOPPED"
    fi
    ;;
esac

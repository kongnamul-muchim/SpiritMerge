#!/bin/bash
# check-errors.sh — Unity 컴파일 에러 CLI 검증 도구
# 사용법:
#   ./check-errors.sh          # 현재 에러 확인
#   ./check-errors.sh --watch  # 3초마다 자동 감시
#   ./check-errors.sh --all    # 전체 로그에서 모든 에러 검색

EDITOR_LOG="/c/Users/user/AppData/Local/Unity/Editor/Editor.log"
PROJECT_LOG="Logs/compile-check.log"

check_errors() {
  local label="$1"
  local errors
  
  # 방법 1: Unity Editor.log (Unity 실행 중일 때)
  if [ -f "$EDITOR_LOG" ]; then
    errors=$(grep "error CS" "$EDITOR_LOG" 2>/dev/null | grep "Assets/Scripts" | sort -u)
    if [ -n "$errors" ]; then
      echo "❌ [$label] ${errors}" | head -30
      echo "$errors" | wc -l | xargs -I{} echo "   → 총 {}개의 컴파일 에러"
      return 1
    fi
  fi
  
  echo "✅ [$label] 컴파일 에러 0개"
  return 0
}

case "${1:-check}" in
  check)
    check_errors "Editor.Log"
    ;;
    
  --watch|-w)
    echo "🔍 Unity Editor.log 감시 시작 (3초 간격, Ctrl+C 종료)"
    while true; do
      clear
      date "+%H:%M:%S"
      check_errors "Watch"
      sleep 3
    done
    ;;
    
  --all|-a)
    echo "=== 전체 에러 로그 ==="
    if [ -f "$EDITOR_LOG" ]; then
      grep -i "error" "$EDITOR_LOG" | grep -i "Assets/Scripts" | sort -u | head -50
    else
      echo "Editor.log not found"
    fi
    ;;
    
  --batch)
    echo "=== Batchmode 컴파일 검증 (Unity 종료 필요) ==="
    UNITY_PATH="/c/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe"
    PROJECT_PATH="C:/Users/user/Spiritia"
    "$UNITY_PATH" -batchmode -quit -projectPath "$PROJECT_PATH" -logFile "$PROJECT_LOG" 2>&1
    if grep -q "error CS" "$PROJECT_LOG" 2>/dev/null; then
      echo "❌ Batchmode 에러 발견!"
      grep "error CS" "$PROJECT_LOG" | sort -u
      return 1
    else
      echo "✅ Batchmode 컴파일 성공"
    fi
    ;;
    
  *)
    echo "Unity 컴파일 에러 검증 도구"
    echo ""
    echo "Usage:"
    echo "  ./check-errors.sh              # 현재 에러 확인"
    echo "  ./check-errors.sh --watch      # 실시간 감시"
    echo "  ./check-errors.sh --all        # 전체 에러 로그"
    echo "  ./check-errors.sh --batch      # 배치모드 검증"
    ;;
esac

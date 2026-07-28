#!/bin/bash
# compile-watch.sh — 컴파일 에러 자동 감지 + 알림
# 사용: Unity 실행 중에 ./compile-watch.sh
# 에러가 발견되면 errors.txt에 저장하고 알려줌

WATCH_LOG="Logs/Editor.log"
ERROR_FILE="compile-errors.txt"

check() {
  # 프로젝트 로컬 로그에서 CS 에러 추출
  if [ -f "$WATCH_LOG" ]; then
    grep "error CS" "$WATCH_LOG" 2>/dev/null | grep "Assets/Scripts" | sort -u > "$ERROR_FILE"
    COUNT=$(wc -l < "$ERROR_FILE")
    if [ "$COUNT" -gt 0 ]; then
      echo "❌ 컴파일 에러 $COUNT 개 발견 → $ERROR_FILE"
      cat "$ERROR_FILE"
    else
      echo "✅ 에러 0개"
      rm -f "$ERROR_FILE"
    fi
  fi
}

case "${1:-check}" in
  check)
    check
    ;;
  --watch|-w)
    echo "🔍 Compile Watch 시작 (5초 간격)"
    while true; do
      clear
      date "+%H:%M:%S"
      check
      sleep 5
    done
    ;;
  --fix)
    # 자동 수정 제안
    if [ -f "$ERROR_FILE" ]; then
      echo "=== 수정해야 할 에러 ==="
      cat "$ERROR_FILE"
    else
      echo "✅ 에러 없음"
    fi
    ;;
  *)
    echo "Usage:"
    echo "  ./compile-watch.sh            # 현재 에러 확인"
    echo "  ./compile-watch.sh --watch    # 실시간 감시"
    echo "  ./compile-watch.sh --fix      # 수정 필요 목록"
    ;;
esac

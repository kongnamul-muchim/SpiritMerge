#!/bin/bash
# verify.sh — 진짜 검증기. 결과가 확실할 때만 ✅를 표시함.
# 사용 전: 반드시 Unity가 컴파일을 마칠 시간을 줘야 함

LOG_FILE="Logs/Editor.log"
ERROR_CACHE="compile-errors.txt"

verify() {
  local label="$1"
  
  # 1. Editor.log 존재 확인
  if [ ! -f "$LOG_FILE" ]; then
    echo "⚠️ [$label] Editor.log 없음 — 검증 불가"
    return 2
  fi

  # 2. CS 에러 추출
  grep "error CS" "$LOG_FILE" 2>/dev/null | grep "Assets/Scripts" | sort -u > "$ERROR_CACHE"
  local count=$(wc -l < "$ERROR_CACHE" 2>/dev/null || echo 0)

  if [ "$count" -gt 0 ]; then
    echo "❌ [$label] 컴파일 에러 $count 개"
    cat "$ERROR_CACHE"
    return 1
  fi

  # 3. 마지막 컴파일이 언제인지 확인 (5분 이상 지난 로그는 신뢰 불가)
  local log_time=$(stat -c %Y "$LOG_FILE" 2>/dev/null || echo 0)
  local now=$(date +%s)
  local age=$((now - log_time))
  
  if [ $age -gt 300 ]; then
    echo "⚠️ [$label] 로그가 5분 이상 지남 ($age 초) — 재검증 필요"
    return 2
  fi

  echo "✅ [$label] 검증 완료 — 에러 0개"
  rm -f "$ERROR_CACHE"
  return 0
}

case "${1:-check}" in
  check)
    verify "verify.sh"
    ;;
  --wait)
    # 최대 30초 대기하며 검증
    for i in $(seq 1 30); do
      result=$(verify "attempt $i" 2>&1)
      if echo "$result" | grep -q "✅"; then
        echo "$result"
        exit 0
      fi
      if echo "$result" | grep -q "❌"; then
        echo "$result"
        exit 1
      fi
      sleep 1
    done
    echo "⏱ 30초 대기 초과 — 검증 불가"
    exit 2
    ;;
  --force)
    # 경고 무시하고 강제 확인
    grep "error CS" "$LOG_FILE" 2>/dev/null | grep "Assets/Scripts" | sort -u
    ;;
  *)
    echo "Usage:"
    echo "  ./verify.sh           # 현재 상태 검증"
    echo "  ./verify.sh --wait    # 최대 30초 대기 후 검증"
    echo "  ./verify.sh --force   # 강제 확인"
    ;;
esac

#!/bin/bash
# ═══════════════════════════════════════════════════
# Spirit Merge — Mega Setup Script
# Unity를 종료한 후 실행하세요!
# 이 스크립트 한 번으로:
#   1. Scene 2종 생성 (InitScene, MainScene) — 전투는 MainScene 내 패널
#   2. GameManager 프리팹 생성
#   3. Build Settings 구성
#   4. SpiritData 30종 ScriptableObject 생성
#   5. Unity 프로젝트 Refresh
# ═══════════════════════════════════════════════════

UNITY_PATH="/c/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Unity.exe"
PROJECT_PATH="C:/Users/user/Spiritia"
LOG_FILE="Logs/mega-setup.log"

mkdir -p "$(dirname "$LOG_FILE")"

echo "╔══════════════════════════════════════════════╗"
echo "║   Spirit Merge — Mega CLI Auto Setup        ║"
echo "╚══════════════════════════════════════════════╝"
echo ""

# 1. Unity 프로세스 확인
echo "🔍 Checking if Unity Editor is running..."
UNITY_PIDS=$(tasklist //NH 2>/dev/null | grep "Unity.exe" | grep -v "Hub\|Shader\|Crash\|Package\|License\|ILPP\|AutoQuit" | awk '{print $2}')
if [ -n "$UNITY_PIDS" ]; then
  echo "❌ Unity Editor가 실행 중입니다. 먼저 종료해주세요!"
  echo "   종료 방법: Unity 에디터를 닫거나, 아래 명령어로 강제 종료:"
  echo "   taskkill /F /IM Unity.exe"
  echo "   (단, 작업 내용이 저장되지 않을 수 있습니다)"
  exit 1
fi
echo "✅ Unity Editor not running. Proceeding..."
echo ""

# 2. Phase 1: Scene 생성 + Manager 프리팹 + Build Settings
echo "═══════════════════════════════════════════════"
echo "📦 Phase 1: Project Setup (Scenes + Prefab + Build)"
echo "═══════════════════════════════════════════════"
echo "Running: ProjectSetup.BatchSetup..."
"$UNITY_PATH" -batchmode -quit \
  -projectPath "$PROJECT_PATH" \
  -logFile "$LOG_FILE" \
  -executeMethod "SpiritMerge.Editor.ProjectSetup.BatchSetup"

EXIT1=$?
if [ $EXIT1 -eq 0 ]; then
  echo "✅ Phase 1 complete!"
else
  echo "⚠️  Phase 1 exit code: $EXIT1 (check log)"
fi
echo ""

# 3. Phase 2: 30종 SpiritData 생성
echo "═══════════════════════════════════════════════"
echo "📦 Phase 2: Create 30 SpiritData ScriptableObjects"
echo "═══════════════════════════════════════════════"
echo "Running: SpiritDataGenerator.CreateAllSpiritData..."
"$UNITY_PATH" -batchmode -quit \
  -projectPath "$PROJECT_PATH" \
  -logFile "$LOG_FILE" \
  -executeMethod "SpiritMerge.Editor.SpiritDataGenerator.CreateAllSpiritData"

EXIT2=$?
if [ $EXIT2 -eq 0 ]; then
  echo "✅ Phase 2 complete! 30 SpiritData SO created."
else
  echo "⚠️  Phase 2 exit code: $EXIT2 (check log)"
fi
echo ""

# 4. 완료
echo "═══════════════════════════════════════════════"
echo "🏁 Mega Setup Finished!"
echo "═══════════════════════════════════════════════"
echo "📋 Log: $LOG_FILE"
echo ""
echo "생성된 파일:"
echo "  - Assets/Scenes/InitScene.unity"
echo "  - Assets/Scenes/MainScene.unity"
echo "  - Assets/Prefabs/UI/GameManager.prefab"
echo "  - Assets/Resources/Data/Spirits/*.asset (30개)"
echo ""
echo "👉 Unity Editor를 열어서 확인하세요!"
echo ""
./unity.sh log

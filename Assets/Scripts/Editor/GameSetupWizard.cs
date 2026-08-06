using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SpiritMerge;
using SpiritMerge.Battle;
using SpiritMerge.Presentation.UI.HUD;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// 게임 시스템 1회 설정: Monster/Spirit 프리팹 생성 + 씬 연결
    /// 실행: SpiritMerge > Setup > Full Game Setup
    /// </summary>
    public static class GameSetupWizard
    {
        static string prefabDir = "Assets/Resources/Prefabs";

        [MenuItem("SpiritMerge/Setup/Full Game Setup")]
        public static void RunFullSetup()
        {
            CreateDirectories();
            CreateMonsterPrefab();
            CreateSpiritPrefab();
            SetupBattleScene();
            SetupMergeBoard();
            Debug.Log("[GameSetup] ✅ 전체 게임 셋업 완료!");
            EditorUtility.DisplayDialog("Game Setup", "✅ 전체 게임 셋업 완료!\nMonster/Spirit 프리팹 + 씬 설정", "OK");
        }

        static void CreateDirectories()
        {
            System.IO.Directory.CreateDirectory(prefabDir);
        }

        static void CreateMonsterPrefab()
        {
            var go = new GameObject("Monster", typeof(SpriteRenderer), typeof(Monster));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            sr.color = Color.white;

            // HP Bar Canvas (World Space)
            var hpCanvas = new GameObject("HpBarCanvas", typeof(Canvas), typeof(CanvasScaler));
            hpCanvas.transform.SetParent(go.transform, false);
            hpCanvas.transform.localPosition = new Vector3(0, 0.6f, 0);
            var canvas = hpCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            var scaler = hpCanvas.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // HP Bar Image
            var hpBar = new GameObject("HpBar", typeof(UnityEngine.UI.Image));
            hpBar.transform.SetParent(hpCanvas.transform, false);
            var rt = hpBar.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0.6f, 0.08f);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = hpBar.GetComponent<UnityEngine.UI.Image>();
            img.color = Color.red;
            img.type = UnityEngine.UI.Image.Type.Filled;
            img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            img.fillAmount = 1f;

            // HP Bar 배경
            var bg = new GameObject("Bg", typeof(UnityEngine.UI.Image));
            bg.transform.SetParent(hpCanvas.transform, false);
            var brt = bg.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(0.6f, 0.08f);
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            // Monster 컴포넌트 연결 (레거시 프리팹 — HP바는 Slider 기반)
            var monster = go.GetComponent<Monster>();
            monster.spriteRenderer = sr;
            monster.hpSlider = CreateWorldBar(hpBar, img);
            monster.hpBarCanvas = canvas;

            string path = $"{prefabDir}/Monster.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[GameSetup] 🎯 Monster 프리팹 생성: {path}");
        }

        static void CreateSpiritPrefab()
        {
            var go = new GameObject("Spirit", typeof(SpriteRenderer), typeof(SpiritUnit));
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            sr.color = Color.white;

            // HP Bar
            var hpCanvas = new GameObject("HpBarCanvas", typeof(Canvas), typeof(CanvasScaler));
            hpCanvas.transform.SetParent(go.transform, false);
            hpCanvas.transform.localPosition = new Vector3(0, 0.6f, 0);
            var canvas = hpCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            hpCanvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 100;

            var hpBar = new GameObject("HpBar", typeof(UnityEngine.UI.Image));
            hpBar.transform.SetParent(hpCanvas.transform, false);
            var rt = hpBar.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0.6f, 0.08f);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = hpBar.GetComponent<UnityEngine.UI.Image>();
            img.color = Color.green;
            img.type = UnityEngine.UI.Image.Type.Filled;
            img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            img.fillAmount = 1f;

            var bg = new GameObject("Bg", typeof(UnityEngine.UI.Image));
            bg.transform.SetParent(hpCanvas.transform, false);
            var brt = bg.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(0.6f, 0.08f);
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            bg.GetComponent<UnityEngine.UI.Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            var spirit = go.GetComponent<SpiritUnit>();
            spirit.spriteRenderer = sr;
            spirit.hpSlider = CreateWorldBar(hpBar, img);

            string path = $"{prefabDir}/Spirit.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[GameSetup] 🎯 Spirit 프리팹 생성: {path}");
        }

        /// <summary>
        /// 레거시 WorldSpace HP바를 Slider로 변환해 반환
        /// (fillRect=Fill 이미지, value=1 시작)
        /// </summary>
        static UnityEngine.UI.Slider CreateWorldBar(GameObject barGo, UnityEngine.UI.Image fill)
        {
            barGo.AddComponent<UnityEngine.UI.Slider>();
            var slider = barGo.GetComponent<UnityEngine.UI.Slider>();
            slider.interactable = false;
            slider.transition = UnityEngine.UI.Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.value = 1f;
            return slider;
        }

        static void SetupBattleScene()
        {
            var battleArea = GameObject.Find("BattleArea");
            if (battleArea == null) { Debug.LogWarning("[GameSetup] BattleArea 없음"); return; }

            // MonsterSpawner 추가
            // ⭐ SpawnPoint/Monster 프리팹 참조 제거: EnemySlot UI 슬롯을 유닛으로 재사용
            var spawner = battleArea.GetComponent<MonsterSpawner>();
            if (spawner == null) spawner = battleArea.AddComponent<MonsterSpawner>();

            // WaveController 추가
            var waveCtrl = battleArea.GetComponent<WaveController>();
            if (waveCtrl == null) waveCtrl = battleArea.AddComponent<WaveController>();
            {
                var so = new SerializedObject(waveCtrl);
                so.FindProperty("spawner").objectReferenceValue = spawner;
                so.ApplyModifiedProperties();
            }

            // BattleManager 추가 (없으면)
            var bm = battleArea.GetComponent<BattleManager>();
            if (bm == null) bm = battleArea.AddComponent<BattleManager>();

            // ⭐ SpiritSpawnRoot/EnemySpawnRoot 생성 제거:
            // 정령은 SpiritGroup/SpiritSlot, 몬스터는 EnemyGroup/EnemySlot UI 슬롯 기반 배치
            var bmSo = new SerializedObject(bm);
            bmSo.FindProperty("battleField").objectReferenceValue = battleArea.transform;
            bmSo.ApplyModifiedProperties();

            // ※ 전투 시작 버튼 불필요 — GameManager.Start()가 자동 전투 시작
            // (방치형 게임에 시작 버튼은 없음)

            Debug.Log("[GameSetup] 🎮 배틀 씬 설정 완료 (자동 전투 모드)");
        }

        static void SetupMergeBoard()
        {
            var mergeArea = GameObject.Find("MergeArea");
            if (mergeArea == null)
            {
                Debug.LogWarning("[GameSetup] MergeArea 없음");
                return;
            }

            // MergeBoard는 MergeBoardManager가 처리 (구 MergeUI → 제거됨)

            Debug.Log("[GameSetup] 🔄 머지보드 설정 완료 (MergeBoardManager 사용)");
        }
    }
}

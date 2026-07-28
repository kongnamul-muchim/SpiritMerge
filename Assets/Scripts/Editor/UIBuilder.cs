using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SpiritMerge.Presentation.UI.HUD;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// UI 자동 생성기 — 메뉴 한 번으로 전체 UI 배치
    /// 사용: SpiritMerge > UI > Build Main UI
    /// MainUI.Initialize()를 통해 참조 연결 (SOLID: 캡슐화 유지)
    /// </summary>
    public static class UIBuilder
    {
        private const string PrefabDir = "Assets/Prefabs/UI";

        [MenuItem("SpiritMerge/UI/Build Main UI")]
        public static void BuildMainUI()
        {
            System.IO.Directory.CreateDirectory(PrefabDir);

            // 1. Canvas 생성
            var canvas = CreateCanvas();
            var mainUI = canvas.gameObject.AddComponent<MainUI>();

            // 2. 상단 재화바 (텍스트 참조를 받아옴)
            CreateTopBar(canvas.transform, out var levelText, out var goldText, out var rubyText, out var stoneText);

            // 3. 하단 메뉴바
            CreateBottomMenu(canvas.transform);

            // 4. 각종 패널들
            var mergePanel    = CreatePanel("MergePanel",   canvas.transform, new Color(0.1f, 0.1f, 0.15f));
            var battlePanel   = CreatePanel("BattlePanel",  canvas.transform, new Color(0.15f, 0.1f, 0.1f));
            var partyPanel    = CreatePanel("PartyPanel",   canvas.transform, new Color(0.1f, 0.12f, 0.1f));
            var summonPanel   = CreatePanel("SummonPanel",  canvas.transform, new Color(0.12f, 0.1f, 0.15f));
            var codexPanel    = CreatePanel("CodexPanel",   canvas.transform, new Color(0.1f, 0.1f, 0.12f));
            var missionPanel  = CreatePanel("MissionPanel", canvas.transform, new Color(0.12f, 0.12f, 0.1f));
            var dispatchPanel = CreatePanel("DispatchPanel",canvas.transform, new Color(0.1f, 0.14f, 0.12f));
            var raidPanel     = CreatePanel("RaidPanel",    canvas.transform, new Color(0.14f, 0.1f, 0.1f));

            battlePanel.SetActive(false);
            partyPanel.SetActive(false);
            summonPanel.SetActive(false);
            codexPanel.SetActive(false);
            missionPanel.SetActive(false);
            dispatchPanel.SetActive(false);
            raidPanel.SetActive(false);

            // 5. MainUI.Initialize()로 모든 참조 전달
            mainUI.Initialize(mergePanel, battlePanel, partyPanel, summonPanel,
                codexPanel, missionPanel, dispatchPanel, raidPanel,
                levelText, goldText, rubyText, stoneText);

            // 6. MergePanel 내부 슬롯
            CreateGrid("SpiritSlots", mergePanel.transform, 6, 5);

            // 7. BattlePanel 기본 라벨
            CreateLabel("EnemyArea",  battlePanel.transform, "적 진영", 20);
            CreateLabel("SpiritArea", battlePanel.transform, "정령 라인", 20);

            // 8. 프리팹 저장
            var prefabPath = $"{PrefabDir}/MainCanvas.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvas.gameObject, prefabPath);
            Object.DestroyImmediate(canvas.gameObject);
            AssetDatabase.Refresh();

            Debug.Log($"[UIBuilder] ✅ UI 생성 완료! Prefab: {prefabPath}");
            EditorUtility.DisplayDialog("UI Builder",
                "Main UI 생성 완료!\nPrefabs/UI/MainCanvas.prefab\n\nMainScene에 배치 후 사용하세요.", "OK");
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateTopBar(Transform parent,
            out TMP_Text level, out TMP_Text gold, out TMP_Text ruby, out TMP_Text stone)
        {
            var bar = CreatePanel("TopBar", parent, new Color(0, 0, 0, 0.8f));
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(0, 60);

            level = CreateLabel("LevelText", bar.transform, "Lv.1", 24).GetComponent<TMP_Text>();
            gold  = CreateLabel("GoldText",  bar.transform, "0", 20).GetComponent<TMP_Text>();
            ruby  = CreateLabel("RubyText",  bar.transform, "0", 20).GetComponent<TMP_Text>();
            stone = CreateLabel("StoneText", bar.transform, "0", 20).GetComponent<TMP_Text>();
        }

        private static void CreateBottomMenu(Transform parent)
        {
            var bar = CreatePanel("BottomMenu", parent, new Color(0, 0, 0, 0.9f));
            var rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(0, 80);

            string[] labels = { "전투", "파티", "소환", "도감", "의뢰", "레이드" };
            for (int i = 0; i < labels.Length; i++)
            {
                var btn = CreateButton(labels[i], bar.transform, labels[i], 18);
                var btnRect = btn.GetComponent<RectTransform>();
                float x = (i + 0.5f) / labels.Length;
                btnRect.anchorMin = new Vector2(x - 0.12f, 0.1f);
                btnRect.anchorMax = new Vector2(x + 0.12f, 0.9f);
            }
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = new Vector2(0, 80);
            rect.offsetMax = new Vector2(0, -60);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static GameObject CreateLabel(string name, Transform parent, string text, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TMP_Text));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);

            var text = CreateLabel("Text", go.transform, label, fontSize);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            return go;
        }

        private static void CreateGrid(string name, Transform parent, int cols, int rows)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(GridLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.1f);
            rect.anchorMax = new Vector2(0.95f, 0.9f);
            rect.sizeDelta = Vector2.zero;

            var grid = go.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(140, 140);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;

            for (int i = 0; i < cols * rows; i++)
            {
                var slot = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(go.transform, false);
                slot.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.5f);
            }
        }
    }
}

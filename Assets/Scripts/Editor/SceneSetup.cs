using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// MainScene 설정 v3 — MergeUI 완전 연결 + 전투 영역
    /// </summary>
    public static class SceneSetup
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("SpiritMerge/Setup/Setup MainScene")]
        public static void SetupMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainScene";

            // Camera
            var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.transform.position = new Vector3(0, 0, -10);
            cam.tag = "MainCamera";
            cam.GetComponent<Camera>().backgroundColor = new Color(0.08f, 0.08f, 0.15f);

            // Light
            var light = new GameObject("Directional Light", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            // GameLifetimeScope
            var scope = new GameObject("GameLifetimeScope")
                .AddComponent<Infrastructure.DI.GameLifetimeScope>();
            AssignSpiritData(scope);

            // ─── Canvas ──────────────────────────────────
            var canvasObj = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            var canvasTrans = canvasObj.transform;

            // ─── TopBar ──────────────────────────────────
            var topBar = CreatePanel("TopBar", canvasTrans, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, 70), new Color(0, 0, 0, 0.85f));
            AddLine(topBar.transform, new Color(0.3f, 0.5f, 0.9f, 0.5f));
            CreateTextObj("LevelText", topBar.transform, "Lv.1", new Vector2(0.08f, 0.5f), 28);
            CreateTextObj("GoldText", topBar.transform, "Gold: 0", new Vector2(0.4f, 0.5f), 24);
            CreateTextObj("RubyText", topBar.transform, "Ruby: 0", new Vector2(0.7f, 0.5f), 24);

            // ─── BattleArea ──────────────────────────────
            var battleArea = CreatePanel("BattleArea", canvasTrans, new Vector2(0, 0.45f), new Vector2(1, 1),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Color(0.06f, 0.06f, 0.12f, 0.95f));
            battleArea.GetComponent<RectTransform>().offsetMax = new Vector2(0, -(70 + 5));

            // 적 스폰 위치 (6마리)
            for (int i = 0; i < 6; i++)
            {
                float x = (i + 0.5f) / 6f;
                var enemySlot = CreatePanel($"EnemyPos_{i}", battleArea.transform,
                    new Vector2(x, 0.65f), new Vector2(x, 0.65f),
                    new Vector2(0.5f, 0.5f), new Vector2(70, 70), new Color(0.5f, 0.1f, 0.1f, 0.6f));
            }

            // 정령 위치 (4마리)
            for (int i = 0; i < 4; i++)
            {
                float x = (i + 0.5f) / 4f;
                var spiritSlot = CreatePanel($"SpiritPos_{i}", battleArea.transform,
                    new Vector2(x, 0.25f), new Vector2(x, 0.25f),
                    new Vector2(0.5f, 0.5f), new Vector2(60, 60), new Color(0.2f, 0.4f, 0.8f, 0.6f));
            }

            // 스테이지 정보
            CreateTextObj("StageInfo", battleArea.transform, "Stage 1-1", new Vector2(0.5f, 0.92f), 26);

            // ─── MergeArea ───────────────────────────────
            var mergeArea = CreatePanel("MergeArea", canvasTrans, new Vector2(0, 0), new Vector2(1, 0.45f),
                new Vector2(0.5f, 0f), Vector2.zero, new Color(0.1f, 0.1f, 0.16f, 0.95f));
            mergeArea.GetComponent<RectTransform>().offsetMin = new Vector2(0, 90 + 5);

            // MergeUI 붙이기
            var mergeUI = mergeArea.AddComponent<Presentation.UI.HUD.MergeUI>();

            // spiritListRoot (보유 정령 목록 영역 — 왼쪽 60%)
            var listRoot = new GameObject("SpiritListRoot", typeof(RectTransform));
            listRoot.transform.SetParent(mergeArea.transform, false);
            var lr = listRoot.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0.01f, 0.05f);
            lr.anchorMax = new Vector2(0.58f, 0.95f);
            lr.sizeDelta = Vector2.zero;
            SetField(mergeUI, "spiritListRoot", listRoot.transform);

            // mergeBoardRoot (머지 보드 — 오른쪽 40%)
            var boardRoot = new GameObject("MergeBoardRoot", typeof(RectTransform));
            boardRoot.transform.SetParent(mergeArea.transform, false);
            var br = boardRoot.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.62f, 0.3f);
            br.anchorMax = new Vector2(0.98f, 0.95f);
            br.sizeDelta = Vector2.zero;
            SetField(mergeUI, "mergeBoardRoot", boardRoot.transform);

            // 머지 버튼
            var mergeBtn = CreateButton("MergeBtn", mergeArea.transform, new Vector2(0.7f, 0.15f));
            SetField(mergeUI, "mergeButton", mergeBtn);

            // 크로스 머지 버튼
            var crossBtn = CreateButton("CrossMergeBtn", mergeArea.transform, new Vector2(0.9f, 0.15f));
            SetField(mergeUI, "crossMergeButton", crossBtn);

            // mergeResultPopup (MergeResultPopup 컴포넌트 포함)
            var popupObj = new GameObject("MergeResultPopup", typeof(RectTransform), typeof(Image));
            popupObj.transform.SetParent(mergeArea.transform, false);
            var popupRt = popupObj.GetComponent<RectTransform>();
            popupRt.anchorMin = new Vector2(0.25f, 0.25f);
            popupRt.anchorMax = new Vector2(0.75f, 0.75f);
            popupRt.sizeDelta = Vector2.zero;
            popupObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            var popup = popupObj.AddComponent<Presentation.UI.HUD.MergeResultPopup>();
            SetField(mergeUI, "mergeResultPopup", popupObj);

            // infoText
            var infoObj = CreateTextObj("InfoText", mergeArea.transform, "정령을 선택하세요", new Vector2(0.5f, 0.08f), 20);
            var infoText = infoObj.GetComponent<TMPro.TMP_Text>();
            if (infoText != null) SetField(mergeUI, "infoText", infoText);

            // ─── BottomMenu ──────────────────────────────
            var bottomBar = CreatePanel("BottomMenu", canvasTrans, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0), new Vector2(0, 90), new Color(0.03f, 0.03f, 0.06f, 0.95f));
            AddLineTop(bottomBar.transform, new Color(0.3f, 0.5f, 0.9f, 0.25f));

            string[] tabs = { "전투", "파티", "소환", "도감", "의뢰", "레이드", "정령" };
            for (int i = 0; i < tabs.Length; i++)
            {
                float x = (i + 0.5f) / tabs.Length;
                var btn = CreateTabButton($"Tab_{tabs[i]}", bottomBar.transform, x, i == 0);
                CreateTextObj("Label", btn.transform, tabs[i], new Vector2(0.5f, 0.5f), 18);
            }

            // ─── 저장 ────────────────────────────────────
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[SceneSetup] ✅ MainScene: MergeUI connected + BattleArea");
        }

        // ─── 헬퍼 ────────────────────────────────────────

        static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.pivot = pivot; rt.sizeDelta = sizeDelta;
            go.GetComponent<Image>().color = color;
            return go;
        }

        static GameObject CreateTextObj(string name, Transform parent, string text, Vector2 anchor, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(200, 30);
            // ★ TextMeshProUGUI (TMP_Text는 추상클래스라 AddComponent 불가)
            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text; tmp.fontSize = fontSize; tmp.color = Color.white;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                // LiberationSans 사용 (CreateFontAsset 한글 폰트는 Unity 6 버그로 미지원)
                var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (font != null)
                {
                    // set_font 우회 (Unity 6 CreateFontAsset 버그)
                    try { tmp.font = font; }
                    catch { Debug.LogWarning("[SceneSetup] set_font failed, using default"); }
                }
                // 한글 폰트 대비 Fallback 활성화
                tmp.fontSizeMin = fontSize * 0.5f;
                tmp.fontSizeMax = fontSize * 1.2f;
                tmp.enableAutoSizing = true;
            }
            return go;
        }

        static GameObject CreateButton(string name, Transform parent, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(100, 40);
            go.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.9f, 0.8f);
            return go;
        }

        static GameObject CreateTabButton(string name, Transform parent, float x, bool active)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x - 0.06f, 0.05f);
            rt.anchorMax = new Vector2(x + 0.06f, 0.95f);
            rt.sizeDelta = Vector2.zero;
            go.GetComponent<Image>().color = active
                ? new Color(0.25f, 0.35f, 0.6f, 0.85f)
                : new Color(0.1f, 0.1f, 0.15f, 0.8f);
            return go;
        }

        static void AddLine(Transform parent, Color color)
        {
            var line = new GameObject("BottomLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(parent, false);
            line.GetComponent<Image>().color = color;
            var r = line.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0);
            r.pivot = new Vector2(0.5f, 0); r.sizeDelta = new Vector2(0, 2);
        }

        static void AddLineTop(Transform parent, Color color)
        {
            var line = new GameObject("TopLine", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(parent, false);
            line.GetComponent<Image>().color = color;
            var r = line.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0.5f, 1); r.sizeDelta = new Vector2(0, 1);
        }

        static void SetField<T>(object obj, string name, T value)
        {
            var field = obj.GetType().GetField(name,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(obj, value);
        }

        static void AssignSpiritData(Infrastructure.DI.GameLifetimeScope scope)
        {
            var guids = AssetDatabase.FindAssets("t:SpiritData");
            if (guids.Length == 0) return;
            var list = new System.Collections.Generic.List<SpiritData>();
            foreach (var g in guids)
            {
                var d = AssetDatabase.LoadAssetAtPath<SpiritData>(AssetDatabase.GUIDToAssetPath(g));
                if (d != null) list.Add(d);
            }
            var field = typeof(Infrastructure.DI.GameLifetimeScope)
                .GetField("spiritDatabase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(scope, list.ToArray());
            Debug.Log($"[SceneSetup] ✅ {list.Count} SpiritData, MergeUI ready");
        }
    }
}

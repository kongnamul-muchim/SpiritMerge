using UnityEditor;
using UnityEngine;
using TMPro;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// TopBar 리빌더 — 스테이지 정보 + 골드/다이아 표시
    /// 실행: SpiritMerge > UI > Rebuild TopBar
    /// </summary>
    public static class TopBarBuilder
    {
        [MenuItem("SpiritMerge/UI/Rebuild TopBar")]
        public static void Rebuild()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (font == null) { Debug.LogError("[TopBar] 폰트 없음!"); return; }

            var topBar = GameObject.Find("TopBar");
            if (topBar == null)
            {
                Debug.LogError("[TopBar] TopBar 없음! SceneCleanup 먼저 실행");
                return;
            }

            Undo.SetCurrentGroupName("Rebuild TopBar");
            int group = Undo.GetCurrentGroup();

            // 기존 자식 제거
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform c in topBar.transform) children.Add(c.gameObject);
            foreach (var c in children) Object.DestroyImmediate(c);

            // TopBar 앵커 재설정
            var tr = topBar.GetComponent<RectTransform>();
            Undo.RecordObject(tr, "TopBar anchor");
            tr.anchorMin = new Vector2(0, 0.92f);
            tr.anchorMax = new Vector2(1, 1);
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            // 배경
            var bg = topBar.GetComponent<UnityEngine.UI.Image>() ?? topBar.AddComponent<UnityEngine.UI.Image>();
            Undo.RecordObject(bg, "TopBar bg");
            bg.color = new Color(0, 0, 0, 0.85f);

            // ── Stage Info (좌측) ──
            var stageInfo = new GameObject("StageInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(stageInfo, "StageInfo");
            stageInfo.transform.SetParent(topBar.transform, false);
            var sr = stageInfo.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0.02f, 0.1f);
            sr.anchorMax = new Vector2(0.55f, 0.9f);
            sr.offsetMin = Vector2.zero;
            sr.offsetMax = Vector2.zero;
            var stmp = stageInfo.GetComponent<TextMeshProUGUI>();
            stmp.text = "Stage 1-8  불의 숲";
            stmp.font = font;
            stmp.fontSize = 22;
            stmp.fontStyle = FontStyles.Bold;
            stmp.color = new Color(0.5f, 0.7f, 1f);
            stmp.alignment = TextAlignmentOptions.MidlineLeft;

            // ── Gold (우측) ──
            var goldText = new GameObject("GoldText", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(goldText, "GoldText");
            goldText.transform.SetParent(topBar.transform, false);
            var gr = goldText.GetComponent<RectTransform>();
            gr.anchorMin = new Vector2(0.60f, 0.1f);
            gr.anchorMax = new Vector2(0.80f, 0.9f);
            gr.offsetMin = Vector2.zero;
            gr.offsetMax = Vector2.zero;
            var gtmp = goldText.GetComponent<TextMeshProUGUI>();
            gtmp.text = "500 Gold";
            gtmp.font = font;
            gtmp.fontSize = 16;
            gtmp.color = new Color(1f, 0.85f, 0.4f);
            gtmp.alignment = TextAlignmentOptions.MidlineRight;

            // ── Ruby (우측 끝) ──
            var rubyText = new GameObject("RubyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(rubyText, "RubyText");
            rubyText.transform.SetParent(topBar.transform, false);
            var rr = rubyText.GetComponent<RectTransform>();
            rr.anchorMin = new Vector2(0.82f, 0.1f);
            rr.anchorMax = new Vector2(0.98f, 0.9f);
            rr.offsetMin = Vector2.zero;
            rr.offsetMax = Vector2.zero;
            var rtmp = rubyText.GetComponent<TextMeshProUGUI>();
            rtmp.text = "100 Ruby";
            rtmp.font = font;
            rtmp.fontSize = 16;
            rtmp.color = new Color(1f, 0.4f, 0.6f);
            rtmp.alignment = TextAlignmentOptions.MidlineRight;

            Undo.CollapseUndoOperations(group);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[TopBar] ✅ TopBar 리빌드 완료!");
            EditorUtility.DisplayDialog("TopBar",
                "✅ TopBar 리빌드 완료!\n" +
                "- Stage Info (좌측)\n" +
                "- Gold (중앙 우측)\n" +
                "- Ruby (우측 끝)", "OK");
        }
    }
}

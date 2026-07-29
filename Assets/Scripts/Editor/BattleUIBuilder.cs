using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// BattleArea UI 리빌더 v2 — 웹 시안 기반, 폰트 키움, TopBar 레이아웃 맞춤
    /// 
    /// 실행: SpiritMerge > UI > Rebuild Battle UI
    /// </summary>
    public static class BattleUIBuilder
    {
        private static TMP_FontAsset _font;

        [MenuItem("SpiritMerge/UI/Rebuild Battle UI")]
        public static void Rebuild()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (_font == null)
            {
                Debug.LogError("[BattleUI] NotoSansKR 폰트 없음!");
                return;
            }

            var battleArea = GameObject.Find("BattleArea");
            if (battleArea == null)
            {
                Debug.LogError("[BattleUI] BattleArea 없음! SceneCleanup 먼저 실행하세요.");
                return;
            }

            Undo.SetCurrentGroupName("Rebuild Battle UI");
            int group = Undo.GetCurrentGroup();

            // 기존 자식 제거
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform c in battleArea.transform) children.Add(c.gameObject);
            foreach (var c in children) Object.DestroyImmediate(c);

            // ── 배경: 검은 계열 푸른색 ──
            var bgImg = battleArea.GetComponent<UnityEngine.UI.Image>();
            if (bgImg == null) bgImg = battleArea.AddComponent<UnityEngine.UI.Image>();
            Undo.RecordObject(bgImg, "Battle BG");
            bgImg.color = new Color(0.05f, 0.06f, 0.12f);  // 아주 어두운 푸른색
            bgImg.raycastTarget = false;

            // ── Wave Info (중앙 상단, 페이드 애니메이션용 CanvasGroup) ──
            var waveInfo = new GameObject("WaveInfo", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(waveInfo, "WaveInfo");
            waveInfo.transform.SetParent(battleArea.transform, false);
            var wr = waveInfo.GetComponent<RectTransform>();
            wr.anchorMin = new Vector2(0.1f, 0.85f);
            wr.anchorMax = new Vector2(0.9f, 1.0f);
            wr.offsetMin = Vector2.zero;
            wr.offsetMax = Vector2.zero;
            var wtmp = waveInfo.GetComponent<TextMeshProUGUI>();
            wtmp.text = "WAVE 1 / 5";
            wtmp.font = _font;
            wtmp.fontSize = 18;
            wtmp.fontStyle = FontStyles.Bold;
            wtmp.color = new Color(1f, 0.6f, 0.4f, 0f); // 투명 시작 (페이드 인)
            wtmp.alignment = TextAlignmentOptions.Center;
            var wcg = waveInfo.GetComponent<CanvasGroup>();
            wcg.alpha = 0f; // 초기 투명

            // ── Enemy Group (3마리) ──
            var enemyGroup = new GameObject("EnemyGroup", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(enemyGroup, "EnemyGroup");
            enemyGroup.transform.SetParent(battleArea.transform, false);
            var egRect = enemyGroup.GetComponent<RectTransform>();
            egRect.anchorMin = new Vector2(0.1f, 0.45f);
            egRect.anchorMax = new Vector2(0.9f, 0.85f);
            egRect.offsetMin = Vector2.zero;
            egRect.offsetMax = Vector2.zero;
            egRect.pivot = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < 3; i++)
            {
                float w = 1f / 3f;
                var slot = new GameObject($"EnemySlot_{i}", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(slot, $"EnemySlot_{i}");
                slot.transform.SetParent(enemyGroup.transform, false);

                var sr = slot.GetComponent<RectTransform>();
                sr.anchorMin = new Vector2(i * w + 0.06f, 0.05f);
                sr.anchorMax = new Vector2((i + 1) * w - 0.06f, 0.95f);
                sr.offsetMin = Vector2.zero;
                sr.offsetMax = Vector2.zero;

                var img = slot.GetComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = false;

                // Lv (좌측 상단)
                var lvl = CreateLabel("LvText", slot.transform,
                    "Lv.3", 13, new Color(0.8f, 0.3f, 0.3f, 0.8f), TextAlignmentOptions.TopLeft);
                SetAnchors(lvl, 0.05f, 0.7f, 0.5f, 1.0f);
                lvl.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

                // 속성 아이콘 (우측 상단, 임시 ●)
                var elem = CreateLabel("ElemIcon", slot.transform,
                    "●", 11, new Color(1f, 0.5f, 0.2f, 0.7f), TextAlignmentOptions.TopRight);
                SetAnchors(elem, 0.5f, 0.7f, 0.95f, 1.0f);

                // HP 바 (하단)
                var hpBg = new GameObject("HPBar", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(hpBg, "HPBar");
                hpBg.transform.SetParent(slot.transform, false);
                SetAnchors(hpBg, 0.1f, 0.0f, 0.9f, 0.1f);
                hpBg.GetComponent<Image>().color = new Color(0.3f, 0.1f, 0.1f, 0.5f);

                var hpFill = new GameObject("HPFill", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(hpFill, "HPFill");
                hpFill.transform.SetParent(hpBg.transform, false);
                SetAnchors(hpFill, 0f, 0f, 0.65f, 1f);
                hpFill.GetComponent<Image>().color = new Color(1f, 0.2f, 0.2f, 0.7f);
            }

            // ── Spirit Group (4마리, 하단) ──
            var spiritGroup = new GameObject("SpiritGroup", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(spiritGroup, "SpiritGroup");
            spiritGroup.transform.SetParent(battleArea.transform, false);
            var sgRect = spiritGroup.GetComponent<RectTransform>();
            sgRect.anchorMin = new Vector2(0.05f, 0.05f);
            sgRect.anchorMax = new Vector2(0.95f, 0.35f);
            sgRect.offsetMin = Vector2.zero;
            sgRect.offsetMax = Vector2.zero;
            sgRect.pivot = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < 4; i++)
            {
                float w = 1f / 4f;
                var slot = new GameObject($"SpiritSlot_{i}", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(slot, $"SpiritSlot_{i}");
                slot.transform.SetParent(spiritGroup.transform, false);

                var sr = slot.GetComponent<RectTransform>();
                sr.anchorMin = new Vector2(i * w + 0.04f, 0.05f);
                sr.anchorMax = new Vector2((i + 1) * w - 0.04f, 0.95f);
                sr.offsetMin = Vector2.zero;
                sr.offsetMax = Vector2.zero;

                var img = slot.GetComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = false;

                // 체력바 (하단)
                var hpBg = new GameObject("HPBar", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(hpBg, "HPBar");
                hpBg.transform.SetParent(slot.transform, false);
                SetAnchors(hpBg, 0.1f, 0.0f, 0.9f, 0.12f);
                hpBg.GetComponent<Image>().color = new Color(0.1f, 0.2f, 0.3f, 0.5f);

                var hpFill = new GameObject("HPFill", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(hpFill, "HPFill");
                hpFill.transform.SetParent(hpBg.transform, false);
                SetAnchors(hpFill, 0f, 0f, 1f, 1f);
                hpFill.GetComponent<Image>().color = new Color(0.3f, 0.6f, 1f, 0.7f);
            }

            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();

            Debug.Log("[BattleUI] ✅ Battle UI 리빌드 v2 완료!");
            EditorUtility.DisplayDialog("Battle UI",
                "Battle Area 리빌드 완료! (v2)\n\n" +
                "- 폰트 크기 1.5배 증가\n" +
                "- WaveInfo CanvasGroup (페이드 효과 준비)\n" +
                "- TopBar 영역 확보", "OK");
        }

        static void SetAnchors(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, yMin);
            r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        static GameObject CreateLabel(string name, Transform parent, string text, int size,
            Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = _font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            return go;
        }
    }
}

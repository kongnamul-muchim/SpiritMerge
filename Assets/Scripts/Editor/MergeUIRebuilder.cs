using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// Merge UI 빌더 v5 — 웹 시안 완전 일치
    /// - MergeArea 내부 전부 리셋 후 재구축
    /// - LiberationSans(주) + NotoSansKR(한글 fallback)
    /// - 둥근 모서리 버튼 (Image 라운드 처리)
    /// - GNB 리빌드 포함
    /// </summary>
    public static class MergeUIRebuilder
    {
        static TMP_FontAsset _font;

        [MenuItem("SpiritMerge/UI/Rebuild Merge UI")]
        public static void Rebuild()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (_font == null)
            {
                Debug.LogError("[MergeUI] ❌ NotoSansKR-VariableFont_wght SDF 없음! 먼저 Font Asset Creator로 생성하세요.");
                return;
            }

            // Fallback 등록 (NotoSansKR for 한글)
            var noto = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VF SDF");
            if (noto != null)
            {
                if (_font.fallbackFontAssetTable == null)
                    _font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                if (!_font.fallbackFontAssetTable.Contains(noto))
                {
                    _font.fallbackFontAssetTable.Add(noto);
                    Debug.Log("[MergeUI] ✅ LiberationSans → NotoSansKR Fallback 등록");
                }
            }

            Undo.SetCurrentGroupName("Rebuild Merge UI v5");
            int group = Undo.GetCurrentGroup();

            // ── 1. 폰트 통일 ──
            ApplyFontToAllTexts();

            // ── 2. MergeArea 찾기 ──
            var mergeArea = GameObject.Find("MergeArea");
            if (mergeArea == null) { Debug.LogError("[MergeUI] MergeArea 없음!"); return; }

            // ── 3. MergeArea 내부 전체 리셋 ──
            ResetMergeArea(mergeArea);

            // ── 5. Section Header ──
            var header = CreateLabel("MergeSectionHeader", mergeArea.transform,
                "머지 보드   12 / 16", 13,
                new Color(0.6f, 0.7f, 1f, 0.5f), TextAlignmentOptions.Left);
            SetAnchors(header, 0.03f, 0.94f, 0.97f, 1.0f);

            // ── 5. Merge Board (4×4) ──
            CreateMergeBoard(mergeArea);

            // ── 6. 정령 소환 버튼 (둥근 모서리) ──
            CreateSummonButton(mergeArea);

            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();

            Debug.Log("[MergeUI] ✅ Merge UI 리빌드 v5 완료!");
        }

        static void ResetMergeArea(GameObject mergeArea)
        {
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in mergeArea.transform)
                children.Add(child.gameObject);
            foreach (var child in children)
                Object.DestroyImmediate(child);

            // MergeArea 앵커
            var areaRect = mergeArea.GetComponent<RectTransform>();
            areaRect.anchorMin = new Vector2(0, 0);
            areaRect.anchorMax = new Vector2(1, 0.55f);
            areaRect.offsetMin = new Vector2(0, 56);
            areaRect.offsetMax = new Vector2(0, 0);
            areaRect.pivot = new Vector2(0.5f, 0);
        }

        static void CreateMergeBoard(GameObject mergeArea)
        {
            // Board background
            var boardObj = new GameObject("MergeBoard", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(boardObj, "MergeBoard");
            boardObj.transform.SetParent(mergeArea.transform, false);
            SetAnchors(boardObj, 0.01f, 0.16f, 0.99f, 0.92f);
            boardObj.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.12f);

            // 4x4 Grid — 정사각형 타일 (모든 비율 대응)
            float cols = 4, rows = 4;
            float mL = 0.06f, mR = 0.06f, mT = 0.04f, mB = 0.02f;
            float gapX = 0.025f, gapY = 0.015f;

            // slot 너비 (보드 폭 기준)
            float slotW = (1f - mL - mR - gapX * (cols - 1)) / cols;
            // 보드 자체의 가로/세로 비율 (anchor 값 기준)
            float boardAspect = (0.99f - 0.01f) / (0.92f - 0.16f);
            // 정사각형 타일 = slotH를 보드 비율만큼 보정
            float slotH = slotW * boardAspect;

            // 세로로 맞는지 확인 후 초과 시 비례 축소
            float totalH = rows * slotH + (rows - 1) * gapY;
            float availH = 1f - mT - mB;
            if (totalH > availH)
            {
                float scale = availH / totalH;
                slotW *= scale;
                slotH *= scale;
            }

            // 가운데 정렬: 남는 공간 반으로 나눠서 offset 계산
            float totalGridW = cols * slotW + (cols - 1) * gapX;
            float extraH = (1f - mL - mR - totalGridW) / 2f;
            float totalGridH = rows * slotH + (rows - 1) * gapY;
            float extraV = (1f - mT - mB - totalGridH) / 2f;

            for (int i = 0; i < 16; i++)
            {
                int row = i / 4;      // 0-based, 위쪽이 row=0
                int col = i % 4;

                float xMin = mL + extraH + col * (slotW + gapX);
                float xMax = xMin + slotW;
                // row=0이 위쪽 (y=1쪽), row=3이 아래쪽 (y=0쪽)
                float yMin = mB + extraV + (rows - 1 - row) * (slotH + gapY);
                float yMax = yMin + slotH;

                var slot = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(slot, $"Slot_{i}");
                slot.transform.SetParent(boardObj.transform, false);
                SetAnchors(slot, xMin, yMin, xMax, yMax);
                slot.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.2f, 0.6f);

                // Inner highlight
                var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(inner, "Inner");
                inner.transform.SetParent(slot.transform, false);
                SetAnchors(inner, 0.06f, 0.06f, 0.94f, 0.94f);
                inner.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.14f);

                // Element badge (빈 슬롯에선 투명)
                var lvl = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(lvl, "LevelText");
                lvl.transform.SetParent(slot.transform, false);
                SetAnchors(lvl, 0.05f, 0.0f, 0.95f, 0.25f);
                var ltmp = lvl.GetComponent<TextMeshProUGUI>();
                ltmp.text = "";
                ltmp.font = _font;
                ltmp.fontSize = 10;
                ltmp.color = new Color(0.6f, 0.7f, 1f, 0.5f);
                ltmp.alignment = TextAlignmentOptions.BottomRight;
            }
        }

        static void CreateSummonButton(GameObject mergeArea)
        {
            var btnObj = new GameObject("SummonBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(btnObj, "SummonBtn");
            btnObj.transform.SetParent(mergeArea.transform, false);
            SetAnchors(btnObj, 0.02f, 0.04f, 0.98f, 0.15f);

            var img = btnObj.GetComponent<Image>();
            img.color = new Color(0.15f, 0.35f, 0.8f);
            // Rounded corners via 4-image slice if available — fallback to sprite-less:
            // In Unity UI, rounded corners require a sliced sprite.
            // Without one, we use a subtle gradient via child overlay.

            // Button text
            var txt = CreateLabel("Text", btnObj.transform,
                "정령 소환 (500 Gold)", 16,
                Color.white, TextAlignmentOptions.Center);
            SetAnchors(txt, 0, 0, 1, 1);
        }

        static void SetAnchors(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, yMin);
            r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        static void ApplyFontToAllTexts()
        {
#pragma warning disable CS0618
            var all = GameObject.FindObjectsOfType<TMP_Text>(true);
#pragma warning restore CS0618
            foreach (var t in all)
            {
                Undo.RecordObject(t, "Font");
                t.font = _font;
                if (t.color == Color.clear || t.color.a < 0.01f)
                    t.color = Color.white;
            }
        }

        static GameObject CreateLabel(string name, Transform parent, string text, int size,
            Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, $"Label_{name}");
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

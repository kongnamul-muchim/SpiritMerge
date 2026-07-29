using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// GNB 리빌더 — 하단 네비게이션 바를 웹 시안과 동일하게 재구축
    /// 실행: SpiritMerge > UI > Rebuild GNB
    /// </summary>
    public static class GNBBuilder
    {
        static TMP_FontAsset _font;

        [MenuItem("SpiritMerge/UI/Rebuild GNB")]
        public static void Rebuild()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (_font == null)
            {
                Debug.LogError("[GNB] NotoSansKR 폰트 없음! Font Asset Creator로 생성하세요.");
                return;
            }

            var gnb = GameObject.Find("BottomMenu");
            if (gnb == null)
            {
                // 없으면 생성 (MainCanvas 아래)
                var mc = GameObject.Find("MainCanvas");
                if (mc == null) { Debug.LogError("[GNB] MainCanvas 없음!"); return; }
                gnb = new GameObject("BottomMenu", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(gnb, "GNB");
                gnb.transform.SetParent(mc.transform, false);
                var r = gnb.GetComponent<RectTransform>();
                r.anchorMin = new Vector2(0, 0);
                r.anchorMax = new Vector2(1, 0);
                r.pivot = new Vector2(0.5f, 0);
                r.sizeDelta = new Vector2(0, 60);
            }

            Undo.SetCurrentGroupName("Rebuild GNB");
            int group = Undo.GetCurrentGroup();

            // 기존 자식 제거
            var children = new System.Collections.Generic.List<GameObject>();
            foreach (Transform c in gnb.transform) children.Add(c.gameObject);
            foreach (var c in children) Object.DestroyImmediate(c);

            var gnbRect = gnb.GetComponent<RectTransform>();
            gnbRect.anchorMin = new Vector2(0, 0);
            gnbRect.anchorMax = new Vector2(1, 0);
            gnbRect.pivot = new Vector2(0.5f, 0);
            gnbRect.sizeDelta = new Vector2(0, 60);
            gnbRect.anchoredPosition = Vector2.zero;

            var gnbImg = gnb.GetComponent<Image>();
            if (gnbImg == null) gnbImg = gnb.AddComponent<Image>();
            gnbImg.color = new Color(0.02f, 0.02f, 0.04f, 0.95f);

            // Fallback 등록
            var noto = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (noto != null && _font.fallbackFontAssetTable != null && !_font.fallbackFontAssetTable.Contains(noto))
            {
                _font.fallbackFontAssetTable.Add(noto);
            }

            // 5 tabs (이모지 제거 - 나중에 Sprite로 교체)
            var tabs = new (string icon, string label)[]
            {
                ("전투", "전투"),
                ("파티", "파티"),
                ("업그레이드", "업그레이드"),
                ("도감", "도감"),
                ("의뢰", "의뢰"),
            };

            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = new GameObject($"Tab_{tabs[i].label}", typeof(RectTransform), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(tab, $"Tab_{tabs[i].label}");
                tab.transform.SetParent(gnb.transform, false);

                var tr = tab.GetComponent<RectTransform>();
                float w = 1f / tabs.Length;
                tr.anchorMin = new Vector2(i * w, 0);
                tr.anchorMax = new Vector2((i + 1) * w, 1);
                tr.offsetMin = Vector2.zero;
                tr.offsetMax = Vector2.zero;

                var timg = tab.GetComponent<Image>();
                timg.color = i == 0 ? new Color(0.08f, 0.10f, 0.20f) : new Color(0, 0, 0, 0);
                timg.raycastTarget = true;

                // Active indicator (top bar)
                if (i == 0)
                {
                    var ind = new GameObject("ActiveIndicator", typeof(RectTransform), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(ind, "ActiveIndicator");
                    ind.transform.SetParent(tab.transform, false);
                    var indRect = ind.GetComponent<RectTransform>();
                    indRect.anchorMin = new Vector2(0.25f, 0.92f);
                    indRect.anchorMax = new Vector2(0.75f, 1.0f);
                    indRect.offsetMin = Vector2.zero;
                    indRect.offsetMax = Vector2.zero;
                    ind.GetComponent<Image>().color = new Color(0.5f, 0.7f, 1f);
                }

                // Icon
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(icon, "Icon");
                icon.transform.SetParent(tab.transform, false);
                var iconR = icon.GetComponent<RectTransform>();
                iconR.anchorMin = new Vector2(0, 0.35f);
                iconR.anchorMax = new Vector2(1, 0.85f);
                iconR.offsetMin = Vector2.zero;
                iconR.offsetMax = Vector2.zero;
                var iconTmp = icon.GetComponent<TextMeshProUGUI>();
                iconTmp.text = tabs[i].icon; // 텍스트 아이콘 (나중에 Sprite로 교체)
                iconTmp.font = _font;
                iconTmp.fontSize = 20;
                iconTmp.color = i == 0 ? new Color(0.5f, 0.7f, 1f) : new Color(0.4f, 0.5f, 0.7f, 0.4f);
                iconTmp.alignment = TextAlignmentOptions.Center;

                // Label
                var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(label, "Label");
                label.transform.SetParent(tab.transform, false);
                var labelR = label.GetComponent<RectTransform>();
                labelR.anchorMin = new Vector2(0, 0.0f);
                labelR.anchorMax = new Vector2(1, 0.35f);
                labelR.offsetMin = Vector2.zero;
                labelR.offsetMax = Vector2.zero;
                var labelTmp = label.GetComponent<TextMeshProUGUI>();
                labelTmp.text = tabs[i].label;
                labelTmp.font = _font;
                labelTmp.fontSize = 9;
                labelTmp.color = i == 0 ? new Color(0.5f, 0.7f, 1f) : new Color(0.4f, 0.5f, 0.7f, 0.4f);
                labelTmp.alignment = TextAlignmentOptions.Center;
            }

            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();

            Debug.Log("[GNB] ✅ GNB 리빌드 완료! (5탭 + 아이콘 + 텍스트 + 인디케이터)");
        }
    }
}

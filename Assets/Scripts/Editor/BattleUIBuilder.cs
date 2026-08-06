using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// BattleArea UI 리빌더 v3 — Slider 기반 HP/CD 바
    /// 
    /// v3 변경점:
    /// - HPBar/CDBar를 UnityEngine.UI.Slider로 생성 (value로 자연스럽게 조절)
    ///   - Image.Filled 방식은 화면에 fillAmount가 반영되지 않는 문제가 있었음
    ///   - Slider.fillRect로 Fill을 지정하면 value → anchor 자동 조절로 화면 반영 보장
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

                // Lv (좌측 상단) — ⭐ 몬스터에 레벨 개념이 없으므로 LvText 생성 제거
                // (과거 하드코딩 "Lv.3"이 모든 몬스터에 붙어 이상하게 보였음)
                // var lvl = CreateLabel("LvText", slot.transform,
                //     "Lv.3", 13, new Color(0.8f, 0.3f, 0.3f, 0.8f), TextAlignmentOptions.TopLeft);
                // SetAnchors(lvl, 0.05f, 0.7f, 0.5f, 0.9f);
                // lvl.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

                // 속성 아이콘 (우측 상단, 임시 ●)
                var elem = CreateLabel("ElemIcon", slot.transform,
                    "●", 11, new Color(1f, 0.5f, 0.2f, 0.7f), TextAlignmentOptions.TopRight);
                SetAnchors(elem, 0.5f, 0.7f, 0.95f, 0.9f);

                // HP 바 (하단) — ⭐ Slider 기반
                var hpSlider = CreateBar(slot.transform, "HPBar",
                    new Vector2(0.1f, 0.0f), new Vector2(0.9f, 0.1f),
                    new Color(0.3f, 0.1f, 0.1f, 0.5f), new Color(1f, 0.2f, 0.2f, 0.7f), 1f);

                // 공격 쿨타임 바 (슬롯 상단) — ⭐ Slider 기반
                var cdSlider = CreateBar(slot.transform, "CDBar",
                    new Vector2(0.1f, 0.93f), new Vector2(0.9f, 0.99f),
                    new Color(0.2f, 0.15f, 0.05f, 0.5f), new Color(1f, 0.85f, 0.3f, 0.9f), 0f);
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

                // 체력바 (하단) — ⭐ Slider 기반
                var hpSlider = CreateBar(slot.transform, "HPBar",
                    new Vector2(0.1f, 0.0f), new Vector2(0.9f, 0.12f),
                    new Color(0.1f, 0.2f, 0.3f, 0.5f), new Color(0.3f, 0.6f, 1f, 0.7f), 1f);

                // 공격 쿨타임 바 (슬롯 상단) — ⭐ Slider 기반
                var cdSlider = CreateBar(slot.transform, "CDBar",
                    new Vector2(0.1f, 0.9f), new Vector2(0.9f, 0.98f),
                    new Color(0.2f, 0.15f, 0.05f, 0.5f), new Color(1f, 0.85f, 0.3f, 0.9f), 0f);
            }

            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();

            Debug.Log("[BattleUI] ✅ Battle UI 리빌드 v3 완료! (Slider 기반)");
            EditorUtility.DisplayDialog("Battle UI",
                "Battle Area 리빌드 완료! (v3 Slider)\n\n" +
                "- HPBar/CDBar → Slider 기반 (value로 자연스럽게 조절)\n" +
                "- WaveInfo CanvasGroup (페이드 효과 준비)\n" +
                "- TopBar 영역 확보", "OK");
        }

        /// <summary>
        /// ⭐ Slider 기반 바 생성
        /// 구조: [name](Image 배경 + Slider) > Fill(Image, slider.fillRect)
        /// Slider.value → fillRect.anchorMax.x 자동 조절 → 화면 반영 보장
        /// </summary>
        static Slider CreateBar(Transform parent, string name, Vector2 aMin, Vector2 aMax,
            Color bgColor, Color fillColor, float initialValue)
        {
            // 배경 + Slider
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            Undo.RegisterCreatedObjectUndo(bar, name);
            bar.transform.SetParent(parent, false);
            SetAnchors(bar, aMin.x, aMin.y, aMax.x, aMax.y);
            var bg = bar.GetComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;

            // Fill (Slider.fillRect) — 부모에 stretch, Slider가 anchor를 value로 조절
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(fill, "Fill");
            fill.transform.SetParent(bar.transform, false);
            var fr = fill.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = Vector2.zero;
            fr.offsetMax = Vector2.zero;
            var fi = fill.GetComponent<Image>();
            fi.color = fillColor;
            fi.raycastTarget = false;

            var slider = bar.GetComponent<Slider>();
            slider.interactable = false;                 // 사용자 터치 비활성화 (표시용)
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fr;
            slider.value = initialValue;                 // HP=1, CD=0
            return slider;
        }

        static void SetAnchors(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, yMin);
            r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 현재 오픈된 씬 저장 (CLI에서 호출 가능)
        /// 실행: SpiritMerge > UI > Save Scene
        /// </summary>
        [MenuItem("SpiritMerge/UI/Save Scene")]
        public static void SaveScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[BattleUI] ✅ 씬 저장 완료");
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

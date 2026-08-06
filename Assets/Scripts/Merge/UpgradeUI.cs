using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SpiritMerge.Core.Systems;

namespace SpiritMerge.Merge
{
    /// <summary>
    /// 업그레이드 오버레이 UI — 머지+배틀 전체 화면 (Canvas 최상위)
    /// - 상단 고정: 제목 / 탭 3개(골드·루비·레벨) / Lv·SP·경험치 바 / 닫기(X)
    /// - 중간: 세로 스크롤 리스트 — 현재 탭의 노드만 줄당 1개 가로 카드로 표시
    /// - 골드 탭: 골드 지불 / 루비 탭: 루비 지불 / 레벨 탭: SP 1 소모
    /// - % 노드·3성은 선행 노드 레벨 달성 시 잠금 해제
    /// - 폰트 30% 확대 + 버튼 눌림(ColorTint) 애니메이션
    /// </summary>
    public class UpgradeUI : MonoBehaviour
    {
        private PlayerService player;
        private System.Func<int, bool> onGoldUpgrade;
        private System.Func<int, bool> onRubyUpgrade;
        private System.Action onChanged;
        private System.Action onClose;
        private readonly List<GameObject> nodeObjs = new();
        private readonly List<GameObject> sectionLabels = new();
        private TextMeshProUGUI lvText;
        private TextMeshProUGUI spText;
        private TextMeshProUGUI expText;
        private Image expBar;
        private TMP_FontAsset _font;
        private int currentTab = 0; // 0=골드, 1=루비, 2=레벨
        private Image[] tabBgs = new Image[3];

        public static UpgradeUI Create(Transform parent, PlayerService player,
            System.Func<int, bool> onGoldUpgrade, System.Func<int, bool> onRubyUpgrade,
            System.Action onChanged = null, System.Action onClose = null)
        {
            var go = new GameObject("UpgradeOverlay", typeof(RectTransform), typeof(Image), typeof(UpgradeUI));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.12f, 0.98f);
            bg.raycastTarget = true;

            var ui = go.GetComponent<UpgradeUI>();
            ui.player = player;
            ui.onGoldUpgrade = onGoldUpgrade;
            ui.onRubyUpgrade = onRubyUpgrade;
            ui.onChanged = onChanged;
            ui.onClose = onClose;
            ui.Build();
            go.SetActive(false);
            return ui;
        }

        void Build()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");

            // ── 제목 ──
            var title = CreateLabel("TitleText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -10), new Vector2(400, 28), "업그레이드", 20, TextAlignmentOptions.Center);
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;

            // ── 탭 3개 (골드 / 루비 / 레벨) ──
            tabBgs[0] = CreateTabButton("TabGold", 0.14f, "골드", 0);
            tabBgs[1] = CreateTabButton("TabRuby", 0.40f, "루비", 1);
            tabBgs[2] = CreateTabButton("TabLevel", 0.66f, "레벨", 2);

            // ── 정보 행 (Lv / SP / EXP 바) ──
            var infoPanel = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image));
            infoPanel.transform.SetParent(transform, false);
            var ipRt = infoPanel.GetComponent<RectTransform>();
            ipRt.anchorMin = new Vector2(0.08f, 0.82f);
            ipRt.anchorMax = new Vector2(0.92f, 0.88f);
            ipRt.offsetMin = Vector2.zero;
            ipRt.offsetMax = Vector2.zero;
            var ipBg = infoPanel.GetComponent<Image>();
            ipBg.color = new Color(0.09f, 0.11f, 0.22f, 1f);
            ipBg.raycastTarget = false;

            lvText = CreateLabelAreaIn("LvText", infoPanel.transform, new Vector2(0.02f, 0.1f), new Vector2(0.24f, 0.9f),
                "Lv.1", 16, TextAlignmentOptions.MidlineLeft);
            lvText.color = new Color(0.5f, 0.7f, 1f);
            lvText.fontStyle = FontStyles.Bold;

            spText = CreateLabelAreaIn("SpText", infoPanel.transform, new Vector2(0.26f, 0.1f), new Vector2(0.46f, 0.9f),
                "SP: 0", 16, TextAlignmentOptions.MidlineLeft);
            spText.color = new Color(1f, 0.84f, 0f);
            spText.fontStyle = FontStyles.Bold;

            expText = CreateLabelAreaIn("ExpText", infoPanel.transform, new Vector2(0.48f, 0.55f), new Vector2(0.98f, 0.95f),
                "0 / 100", 12, TextAlignmentOptions.MidlineRight);
            expText.color = new Color(0.8f, 0.85f, 1f);

            var expBarGo = new GameObject("ExpBar", typeof(RectTransform), typeof(Image));
            expBarGo.transform.SetParent(infoPanel.transform, false);
            var ebr = expBarGo.GetComponent<RectTransform>();
            ebr.anchorMin = new Vector2(0.48f, 0.08f);
            ebr.anchorMax = new Vector2(0.98f, 0.48f);
            ebr.offsetMin = Vector2.zero;
            ebr.offsetMax = Vector2.zero;
            var ebg = expBarGo.GetComponent<Image>();
            ebg.color = new Color(0.15f, 0.2f, 0.4f, 1f);
            ebg.raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(expBarGo.transform, false);
            var frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            var fImg = fill.GetComponent<Image>();
            fImg.color = new Color(0.3f, 0.8f, 1f, 1f);
            fImg.type = Image.Type.Filled;
            fImg.fillMethod = Image.FillMethod.Horizontal;
            fImg.fillAmount = 0f;
            expBar = fImg;

            // ── 닫기 버튼 ──
            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(transform, false);
            closeGo.transform.SetAsLastSibling();
            var crt = closeGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-10, -10);
            crt.sizeDelta = new Vector2(42, 42);
            var cImg = closeGo.GetComponent<Image>();
            cImg.color = new Color(0.9f, 0.28f, 0.3f, 1f);
            var cBtn = closeGo.GetComponent<Button>();
            SetButtonColors(cBtn);
            cBtn.onClick.AddListener(() => onClose?.Invoke());
            var cTxt = CreateLabelIn("X", closeGo.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(40, 40), "X", 20, TextAlignmentOptions.Center);
            cTxt.color = Color.white;
            cTxt.fontStyle = FontStyles.Bold;

            // ── 스크롤 영역 (GNB 위쪽) ──
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(transform, false);
            var svRt = scrollGo.GetComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.06f, 0.10f);
            svRt.anchorMax = new Vector2(0.94f, 0.80f);
            svRt.offsetMin = Vector2.zero;
            svRt.offsetMax = Vector2.zero;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            cRt.sizeDelta = new Vector2(0, 0);

            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.viewport = vpRt;
            sr.content = cRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 24f;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.inertia = true;

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(6, 6, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // ── 섹션 라벨 3개 + 노드 25개 ──
            sectionLabels.Add(CreateSectionLabel(content.transform, "전투 강화 (골드)"));
            for (int i = 0; i < 10; i++) nodeObjs.Add(CreateNode(content.transform, i));
            sectionLabels.Add(CreateSectionLabel(content.transform, "루비 강화"));
            for (int i = 15; i < 25; i++) nodeObjs.Add(CreateNode(content.transform, i));
            sectionLabels.Add(CreateSectionLabel(content.transform, "소환 스킬트리 (SP)"));
            for (int i = 10; i < 15; i++) nodeObjs.Add(CreateNode(content.transform, i));

            Refresh();
        }

        Image CreateTabButton(string name, float x, string label, int tab)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x, 0.90f);
            rt.anchorMax = new Vector2(x + 0.20f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            var btn = go.GetComponent<Button>();
            SetButtonColors(btn);
            int cap = tab;
            btn.onClick.AddListener(() => { currentTab = cap; Refresh(); });
            var txt = CreateLabelAreaIn("TabLabel", go.transform, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f),
                label, 14, TextAlignmentOptions.Center);
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;
            return img;
        }

        /// <summary>버튼 눌림(ColorTint) 애니메이션 — 기본색 유지 + 호버/눌림 밝기 변화</summary>
        static void SetButtonColors(Button btn)
        {
            btn.transition = Selectable.Transition.ColorTint;
            var c = btn.colors;
            c.normalColor = Color.white;
            c.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            c.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            c.selectedColor = Color.white;
            c.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            c.colorMultiplier = 1f;
            c.fadeDuration = 0.08f;
            btn.colors = c;
        }

        GameObject CreateSectionLabel(Transform parent, string text)
        {
            var go = new GameObject("SectionLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 24);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = "─ " + text + " ─";
            tmp.fontSize = FS(14);
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.6f, 0.8f, 1f);
            tmp.raycastTarget = false;
            if (_font != null) tmp.font = _font;
            return go;
        }

        GameObject CreateNode(Transform parent, int idx)
        {
            var go = new GameObject("UpNode", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 68);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.1f, 0.13f, 0.26f, 1f);
            var btn = go.GetComponent<Button>();
            SetButtonColors(btn);
            int capture = idx;
            btn.onClick.AddListener(() => OnNodeClick(capture));

            var nameT = CreateLabelAreaIn("NodeName", go.transform, new Vector2(0.015f, 0.52f), new Vector2(0.6f, 0.95f),
                PlayerService.UpgradeNames[idx], 15, TextAlignmentOptions.MidlineLeft);
            nameT.color = Color.white;
            nameT.fontStyle = FontStyles.Bold;

            var lvT = CreateLabelAreaIn("NodeLv", go.transform, new Vector2(0.55f, 0.52f), new Vector2(0.985f, 0.95f),
                "Lv.0/50", 13, TextAlignmentOptions.MidlineRight);
            lvT.color = new Color(1f, 0.84f, 0f);

            var descT = CreateLabelAreaIn("NodeDesc", go.transform, new Vector2(0.015f, 0.05f), new Vector2(0.62f, 0.48f),
                PlayerService.UpgradeDesc[idx], 11, TextAlignmentOptions.TopLeft);
            descT.color = new Color(0.7f, 0.75f, 0.9f);
            descT.textWrappingMode = TextWrappingModes.Normal;

            var costT = CreateLabelAreaIn("NodeCost", go.transform, new Vector2(0.63f, 0.05f), new Vector2(0.985f, 0.48f),
                "", 14, TextAlignmentOptions.MidlineRight);
            costT.color = new Color(1f, 0.85f, 0.4f);
            costT.fontStyle = FontStyles.Bold;

            var data = go.AddComponent<UpgradeNodeData>();
            data.index = idx;
            data.lvText = lvT;
            data.descText = descT;
            data.costText = costT;

            return go;
        }

        void OnNodeClick(int idx)
        {
            if (player == null) return;
            if (PlayerService.IsLevelNode(idx))
            {
                if (player.UpgradeAt(idx))
                {
                    GameLogger.Info($"[Upgrade] {PlayerService.UpgradeNames[idx]} Lv.{player.GetUpgradeLevel(idx)} (SP -1)");
                    GameManager.Instance?.OnUpgraded(); // ⭐ 미션 진행 + 즉시 저장
                    Refresh();
                    onChanged?.Invoke();
                }
                else GameLogger.Warn($"[Upgrade] 실패 (SP 부족/최대/잠금): {PlayerService.UpgradeNames[idx]}");
            }
            else if (PlayerService.IsGoldNode(idx))
            {
                if (onGoldUpgrade != null && onGoldUpgrade(idx)) { Refresh(); onChanged?.Invoke(); }
                else GameLogger.Warn($"[Upgrade] 골드 업그레이드 실패: {PlayerService.UpgradeNames[idx]}");
            }
            else
            {
                if (onRubyUpgrade != null && onRubyUpgrade(idx)) { Refresh(); onChanged?.Invoke(); }
                else GameLogger.Warn($"[Upgrade] 루비 업그레이드 실패: {PlayerService.UpgradeNames[idx]}");
            }
        }

        public void Refresh()
        {
            if (player == null) return;
            lvText.text = $"Lv.{player.Level}";
            spText.text = $"SP: {player.SkillPoints}";
            expText.text = $"{player.Exp} / {player.ExpToNext}";
            expBar.fillAmount = player.ExpToNext > 0 ? (float)player.Exp / player.ExpToNext : 0f;

            // 탭 하이라이트
            Color on = new Color(0.22f, 0.35f, 0.6f, 1f);
            Color off = new Color(0.1f, 0.13f, 0.22f, 1f);
            for (int t = 0; t < 3; t++)
                if (tabBgs[t] != null) tabBgs[t].color = t == currentTab ? on : off;

            // 섹션 라벨 (탭 순서대로 3개) — 현재 탭만 표시
            for (int s = 0; s < sectionLabels.Count; s++)
                if (sectionLabels[s] != null) sectionLabels[s].SetActive(s == currentTab);

            foreach (var go in nodeObjs)
            {
                if (go == null) continue;
                var data = go.GetComponent<UpgradeNodeData>();
                if (data == null) continue;
                int idx = data.index;
                int lv = player.GetUpgradeLevel(idx);
                int max = PlayerService.MaxLevelFor(idx);
                bool unlimited = max >= 1000000; // ⭐ 고정스탯: 무한 강화
                bool show = currentTab switch
                {
                    0 => PlayerService.IsGoldNode(idx),
                    1 => PlayerService.IsRubyNode(idx),
                    _ => PlayerService.IsLevelNode(idx)
                };
                go.SetActive(show);
                if (!show) continue;

                bool locked = player.IsLocked(idx);
                bool maxed = !unlimited && lv >= max;

                data.lvText.text = unlimited ? $"Lv.{lv}" : $"Lv.{lv}/{max}";
                data.descText.text = FormatDesc(idx, lv);

                var img = go.GetComponent<Image>();
                var btn = go.GetComponent<Button>();
                if (locked)
                {
                    img.color = new Color(0.1f, 0.1f, 0.12f, 1f);
                    btn.interactable = false;
                    data.costText.text = "잠김";
                    data.costText.color = new Color(0.5f, 0.5f, 0.55f);
                    ApplyNameColor(data, new Color(0.45f, 0.45f, 0.5f));
                }
                else if (maxed)
                {
                    img.color = new Color(0.15f, 0.3f, 0.2f, 1f);
                    btn.interactable = false;
                    data.costText.text = "MAX";
                    data.costText.color = new Color(0.4f, 0.9f, 0.5f);
                    ApplyNameColor(data, Color.white);
                }
                else
                {
                    img.color = new Color(0.1f, 0.13f, 0.26f, 1f);
                    btn.interactable = true;
                    data.costText.color = new Color(1f, 0.85f, 0.4f);
                    data.costText.text = PlayerService.IsGoldNode(idx) ? $"{player.GetGoldCost(idx)}G"
                                        : PlayerService.IsRubyNode(idx) ? $"{player.GetRubyCost(idx)}R"
                                        : "1 SP";
                    ApplyNameColor(data, Color.white);
                }
            }
        }

        void ApplyNameColor(UpgradeNodeData data, Color c)
        {
            var nameT = data.transform.Find("NodeName")?.GetComponent<TextMeshProUGUI>();
            if (nameT != null) nameT.color = c;
        }

        static string FormatDesc(int idx, int lv)
        {
            // ⭐ 0레벨이면 수치 0 — 강화는 1레벨부터 적용 (실제 스탯 보너스는 PlayerService와 동일 기준)
            string fmt = PlayerService.UpgradeDesc[idx];
            object v = idx switch
            {
                0 or 15 => lv * 3,
                1 or 16 => lv * 2,
                2 or 17 => lv * 10,
                3 or 18 => lv,          // 공격속도 +1%
                4 or 19 => lv,          // +1%
                5 or 6 or 20 or 21 => lv * 2, // +2%
                7 => lv * 0.5f,         // 치명타 확률 +0.5%
                8 or 9 or 23 or 24 => lv * 5,
                22 => lv * 2,           // 치명타 데미지 +2%
                10 => Mathf.Max(10f, 30f - lv * 3f),
                11 or 12 => lv * 0.5f,
                13 or 14 => lv,
                _ => 0
            };
            return string.Format(fmt, v);
        }

        /// <summary>폰트 30% 확대</summary>
        static int FS(int size) => Mathf.RoundToInt(size * 1.3f);

        class UpgradeNodeData : MonoBehaviour
        {
            public int index;
            public TextMeshProUGUI lvText;
            public TextMeshProUGUI descText;
            public TextMeshProUGUI costText;
        }

        TextMeshProUGUI CreateLabel(string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            return StyleLabel(go, aMin, aMax, pos, size, text, fontSize, align);
        }

        TextMeshProUGUI CreateLabelIn(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return StyleText(go.GetComponent<TextMeshProUGUI>(), text, fontSize, align);
        }

        TextMeshProUGUI CreateLabelAreaIn(string name, Transform parent, Vector2 aMin, Vector2 aMax, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return StyleText(go.GetComponent<TextMeshProUGUI>(), text, fontSize, align);
        }

        TextMeshProUGUI StyleLabel(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, string text, int fontSize, TextAlignmentOptions align)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return StyleText(go.GetComponent<TextMeshProUGUI>(), text, fontSize, align);
        }

        TextMeshProUGUI StyleText(TextMeshProUGUI tmp, string text, int fontSize, TextAlignmentOptions align)
        {
            tmp.text = text;
            tmp.fontSize = FS(fontSize);
            tmp.fontSizeMax = FS(fontSize);
            tmp.fontSizeMin = 9;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            if (_font != null) tmp.font = _font;
            return tmp;
        }
    }
}

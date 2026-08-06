using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

namespace SpiritMerge.Merge
{
    /// <summary>
    /// 파티 편성 오버레이 UI — BattleArea 위에 얹혀서 표시
    /// - 파티 슬롯 2x2 (클릭 → 배치/해제)
    /// - 파티 정보 패널 (전투력, 편성 수, 속성 구성)
    /// - 시너지 패널 (현재 발동 시너지 + 호버 ToolTip)
    /// - GameManager가 BattleArea에 생성하고 GNB 탭으로 표시/숨김
    /// </summary>
    public class PartyFormationUI : MonoBehaviour
    {
        private MergeBoardManager board;
        private readonly List<GameObject> slotObjs = new();
        private TextMeshProUGUI powerText;
        private TextMeshProUGUI countText;
        private TextMeshProUGUI pauseBanner;
        private TextMeshProUGUI hintText;
        private Transform synergyItemsRoot; // 시너지 항목 (색 블록 + 이름) 루트
        private GameObject tooltipGo;          // ToolTip 패널
        private TextMeshProUGUI tooltipText;   // ToolTip 상세
        private SynergyTooltipHandler tooltipHandler;

        /// <summary>
        /// 오버레이 생성 (BattleArea의 자식으로, 최상단에)
        /// </summary>
        public static PartyFormationUI Create(Transform parent, MergeBoardManager board)
        {
            var go = new GameObject("FormationOverlay", typeof(RectTransform), typeof(Image), typeof(PartyFormationUI));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling(); // BattleArea 내 최상단에 표시

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.12f, 0.98f);
            bg.raycastTarget = true;

            var ui = go.GetComponent<PartyFormationUI>();
            ui.board = board;
            ui.Build();
            go.SetActive(false);

            // ⭐ 배경 탭 → 툴팁 숨김 (모바일)
            var bgClick = go.AddComponent<OverlayBgClick>();
            bgClick.ui = ui;
            return ui;
        }

        /// <summary>
        /// 오버레이 내부 UI 구성
        /// </summary>
        void Build()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");

            // ── 제목 (이모지 제거 — NotoSansKR 폰트에 없어서 □로 깨짐) ──
            // ⭐ Center 정렬: 이전 TextAlignmentOptions.Top은 좌측 정렬이라 왼쪽 치우침 원인이었음
            var title = CreateLabel("TitleText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -10), new Vector2(420, 32), "파티 편성", 18, TextAlignmentOptions.Center);
            title.color = Color.white;

            // ── 파티 정보 패널 (⭐ 전투력/편성 5/5 — 시너지는 하단 패널에서) ──
            var info = new GameObject("PartyInfo", typeof(RectTransform), typeof(Image));
            info.transform.SetParent(transform, false);
            var irt = info.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.15f, 0.80f);
            irt.anchorMax = new Vector2(0.85f, 0.92f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            var iBg = info.GetComponent<Image>();
            iBg.color = new Color(0.09f, 0.11f, 0.22f, 1f);
            iBg.raycastTarget = false;

            // ── 5/5 배치: [전투력(좌)] | [편성(우)] (PartyElems 속성 점들 제거) ──
            var powerLbl = CreateLabel("PowerLabel", new Vector2(0.25f, 0.8f), new Vector2(0.25f, 0.8f),
                Vector2.zero, new Vector2(160, 20), "전투력", 11, TextAlignmentOptions.Center);
            powerLbl.transform.SetParent(info.transform, false);
            powerLbl.color = new Color(0.54f, 0.58f, 0.7f);

            powerText = CreateLabel("PowerValue", new Vector2(0.25f, 0.3f), new Vector2(0.25f, 0.3f),
                Vector2.zero, new Vector2(170, 28), "0", 17, TextAlignmentOptions.Center);
            powerText.transform.SetParent(info.transform, false);
            powerText.color = new Color(1f, 0.84f, 0f);

            var countLbl = CreateLabel("CountLabel", new Vector2(0.75f, 0.8f), new Vector2(0.75f, 0.8f),
                Vector2.zero, new Vector2(120, 20), "편성", 11, TextAlignmentOptions.Center);
            countLbl.transform.SetParent(info.transform, false);
            countLbl.color = new Color(0.54f, 0.58f, 0.7f);

            countText = CreateLabel("CountValue", new Vector2(0.75f, 0.3f), new Vector2(0.75f, 0.3f),
                Vector2.zero, new Vector2(120, 28), "0/4", 17, TextAlignmentOptions.Center);
            countText.transform.SetParent(info.transform, false);
            countText.color = new Color(1f, 0.84f, 0f);

            // ── 빈 파티 배너 (이모지 제거) — 슬롯 아래 안내 영역 ⭐
            pauseBanner = CreateLabel("PauseBanner", new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.14f),
                new Vector2(0, 0), new Vector2(460, 26), "파티를 편성하세요 — 전투 일시정지 중", 12, TextAlignmentOptions.Center);
            pauseBanner.color = new Color(1f, 0.84f, 0f);
            pauseBanner.gameObject.SetActive(false);

            // ── 조작 힌트 (보드 정령 선택 상태 안내) — 슬롯 아래 안내 영역 ⭐
            hintText = CreateLabel("HintText", new Vector2(0.5f, 0.08f), new Vector2(0.5f, 0.08f),
                new Vector2(0, 0), new Vector2(460, 24), "", 11, TextAlignmentOptions.Center);
            hintText.color = new Color(0.6f, 0.8f, 1f);
            hintText.gameObject.SetActive(false);

            // ── 파티 슬롯 2x2 (⭐ PartyInfo 바로 아래로 상향 배치 — 정보 패널과 붙어서 자연스럽게) ──
            for (int i = 0; i < 4; i++)
            {
                var col = i % 2;
                var row = i / 2;
                var slot = new GameObject($"PartySlot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                slot.transform.SetParent(transform, false);
                var srt = slot.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.06f + col * 0.47f, 0.26f + (1 - row) * 0.24f);
                srt.anchorMax = new Vector2(0.5f + col * 0.47f, 0.5f + (1 - row) * 0.24f);
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = Vector2.zero;

                var sBg = slot.GetComponent<Image>();
                sBg.color = new Color(0.1f, 0.13f, 0.26f, 1f);

                var btn = slot.GetComponent<Button>();
                SetBtnPress(btn);
                int capture = i;
                btn.onClick.AddListener(() => OnSlotTap(capture));

                // 슬롯 번호
                var num = CreateLabel("SlotNum", new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(6, -4), new Vector2(40, 20), $"P{i + 1}", 10, TextAlignmentOptions.Left);
                num.transform.SetParent(slot.transform, false);
                num.color = new Color(0.54f, 0.58f, 0.7f);

                // 해제 버튼
                var rm = new GameObject("RemoveBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                rm.transform.SetParent(slot.transform, false);
                var rrt = rm.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(1, 1);
                rrt.anchorMax = new Vector2(1, 1);
                rrt.pivot = new Vector2(1, 1);
                rrt.sizeDelta = new Vector2(26, 26);
                rrt.anchoredPosition = new Vector2(-4, -4);
                var rBg = rm.GetComponent<Image>();
                rBg.color = new Color(0.9f, 0.28f, 0.3f, 1f);
                var rBtn = rm.GetComponent<Button>();
                SetBtnPress(rBtn);
                int cap2 = i;
                rBtn.onClick.AddListener(() => { board.RemoveFromParty(cap2); });
                var rTxt = CreateLabel("X", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(26, 26), "X", 13, TextAlignmentOptions.Center);
                rTxt.transform.SetParent(rm.transform, false);
                rTxt.color = Color.white;

                slotObjs.Add(slot);
            }

            // ── 시너지 패널 (⭐ FormationOverlay 하단 — 파티 슬롯 아래, 색 블록 + 이름) ──
            var synGo = new GameObject("SynergyPanel", typeof(RectTransform), typeof(Image));
            synGo.transform.SetParent(transform, false);
            var synRt = synGo.GetComponent<RectTransform>();
            synRt.anchorMin = new Vector2(0.08f, 0.02f);
            synRt.anchorMax = new Vector2(0.92f, 0.10f);
            synRt.offsetMin = Vector2.zero;
            synRt.offsetMax = Vector2.zero;
            var synImg = synGo.GetComponent<Image>();
            synImg.color = new Color(0.07f, 0.09f, 0.18f, 1f);
            synImg.raycastTarget = true; // ⭐ 호버 감지 (ToolTip)

            // 시너지 항목 루트 (색 블록 + 이름 — PartyElems처럼)
            var synContent = new GameObject("SynergyItems", typeof(RectTransform));
            synContent.transform.SetParent(synGo.transform, false);
            var scrt = synContent.GetComponent<RectTransform>();
            scrt.anchorMin = new Vector2(0.02f, 0.1f);
            scrt.anchorMax = new Vector2(0.98f, 0.9f);
            scrt.offsetMin = Vector2.zero;
            scrt.offsetMax = Vector2.zero;
            synergyItemsRoot = synContent.transform;

            tooltipHandler = synGo.AddComponent<SynergyTooltipHandler>();
            tooltipHandler.ui = this;

            // ── ToolTip (시너지 호버 시 — ⭐ Elem 왼쪽정렬 / Text 오른쪽정렬, 끝 15px 여백) ──
            tooltipGo = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
            tooltipGo.transform.SetParent(transform, false);
            tooltipGo.transform.SetAsLastSibling();
            var tRt = tooltipGo.GetComponent<RectTransform>();
            tRt.sizeDelta = new Vector2(360, 120); // ⭐ 고정 크기 (배경 항상 보임)
            var tImg = tooltipGo.GetComponent<Image>();
            tImg.color = new Color(0.03f, 0.04f, 0.09f, 0.97f);
            tImg.raycastTarget = false; // ⭐ 마우스 이벤트 안 받게 → 호버 번쩍임 방지

            // 텍스트 (패널 중앙 기준 — 위치 어긋남 방지)
            tooltipText = CreateLabelIn("TooltipText", tooltipGo.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(300, 110), "", 12, TextAlignmentOptions.TopLeft);
            tooltipText.color = new Color(0.9f, 0.93f, 1f);
            tooltipText.textWrappingMode = TextWrappingModes.Normal;
            tooltipText.raycastTarget = false;
            tooltipGo.SetActive(false);
        }

        /// <summary>시너지 ToolTip — ⭐ 속성 블록(count개) + 줄별 효과 텍스트 (안 겹치게)</summary>
        public void ShowTooltip(string text, Vector2 pos)
        {
            if (tooltipGo == null) return;
            tooltipGo.SetActive(true);

            // 기존 동적 요소 제거
            foreach (Transform c in tooltipGo.transform)
                if (c.name.StartsWith("Tip")) Destroy(c.gameObject);

            var items = GameManager.Instance != null ? GameManager.Instance.GetSynergyItems() : null;
            if (items == null || items.Count == 0)
            {
                var t = CreateLabelIn("TipText", tooltipGo.transform, new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(280, 30), "같은 속성 2기 이상 편성하면 시너지가 발동합니다.", 13, TextAlignmentOptions.Center);
                t.color = new Color(0.9f, 0.93f, 1f);
            }
            else
            {
                float y = 0.80f;
                foreach (var (elem, count, txt) in items)
                {
                    // ⭐ 속성 블록: 왼쪽 정렬 (count개 가로)
                    for (int i = 0; i < count; i++)
                    {
                        var dot = new GameObject("TipElem", typeof(RectTransform), typeof(Image));
                        dot.transform.SetParent(tooltipGo.transform, false);
                        var drt = dot.GetComponent<RectTransform>();
                        drt.anchorMin = new Vector2(0.04f + i * 0.06f, y);
                        drt.anchorMax = drt.anchorMin;
                        drt.sizeDelta = new Vector2(16, 16);
                        dot.GetComponent<Image>().color = GetElementColor(elem);
                    }
                    // ⭐ 효과 텍스트: 오른쪽 끝 고정(pivot 1) + 같은 Y줄 — 툴팁 밖 안 나감
                    var label = CreateLabelIn("TipText", tooltipGo.transform,
                        new Vector2(0.96f, y), Vector2.zero,
                        new Vector2(230, 24), txt, 13, TextAlignmentOptions.Right);
                    var lrt = label.GetComponent<RectTransform>();
                    lrt.pivot = new Vector2(1f, 0.5f);
                    lrt.anchoredPosition = Vector2.zero;
                    label.color = new Color(0.9f, 0.93f, 1f);
                    y -= 0.26f;
                }
            }

            // ⭐ 편성 슬롯(중앙) 안 가리는 위치: 시너지 패널(하단) 위쪽에 고정
            var rt = tooltipGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.12f);
            rt.anchorMax = new Vector2(0.5f, 0.12f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
        }

        public void HideTooltip()
        {
            if (tooltipGo != null) tooltipGo.SetActive(false);
        }

        /// <summary>시너지 패널 탭 — 툴팁 토글 (다시 탭/배경 탭하면 숨김)</summary>
        class SynergyTooltipHandler : MonoBehaviour, IPointerClickHandler
        {
            public PartyFormationUI ui;
            public void OnPointerClick(PointerEventData e)
            {
                if (ui.tooltipGo != null && ui.tooltipGo.activeSelf) ui.HideTooltip();
                else ui.ShowTooltip("", e.position);
            }
        }

        /// <summary>오버레이 배경 탭 — 툴팁 숨김</summary>
        class OverlayBgClick : MonoBehaviour, IPointerClickHandler
        {
            public PartyFormationUI ui;
            public void OnPointerClick(PointerEventData e) => ui?.HideTooltip();
        }

        TextMeshProUGUI CreateLabel(string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = Mathf.RoundToInt(fontSize * 1.3f); // ⭐ 폰트 30% 확대
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            // ⭐ 화면 비율 대응: 지정 영역 안에서 폰트 자동 축소/확대
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Min(9, fontSize);
            tmp.fontSizeMax = Mathf.RoundToInt(fontSize * 1.3f);
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (font != null) tmp.font = font;
            return tmp;
        }

        void OnSlotTap(int idx)
        {
            board.OnPartySlotTapped(idx);
            Refresh();
        }

        /// <summary>드래그 드롭 → 파티 슬롯에 배치 (MergeBoardManager.OnDragEnd에서 호출)</summary>
        public bool TryDropOnSlot(int boardIdx, Vector2 pointerPos)
        {
            if (board == null || boardIdx < 0) return false;
            for (int i = 0; i < 4; i++)
            {
                var slot = transform.Find($"PartySlot_{i}");
                if (slot == null) continue;
                if (IsInside(slot, pointerPos))
                {
                    board.OnPartySlotTapped(i); // 파티 슬롯 탭 (선택된 보드 정령 배치)
                    return true;
                }
            }
            return false;
        }

        bool IsInside(Transform slot, Vector2 pos)
        {
            var corners = new Vector3[4];
            slot.GetComponent<RectTransform>().GetWorldCorners(corners);
            return pos.x >= corners[0].x && pos.x <= corners[2].x
                && pos.y >= corners[0].y && pos.y <= corners[2].y;
        }

        /// <summary>
        /// 파티 상태에 맞게 오버레이 UI 갱신
        /// </summary>
        public void Refresh()
        {
            if (board == null) return;
            var slots = board.GetPartySlots();

            // 파티 슬롯 표시
            for (int i = 0; i < 4; i++)
            {
                var slot = slotObjs[i];
                int bidx = slots[i];
                var itemData = bidx >= 0 ? board.GetItemData(bidx) : null;

                // ⭐ 선택된 슬롯 하이라이트 (편성 모드에서 어느 슬롯을 고르고 있는지 표시)
                bool isSelected = board.SelectedPartySlot == i;
                var slotImg = slot.GetComponent<Image>();
                if (isSelected)
                    slotImg.color = new Color(0.32f, 0.42f, 0.75f, 1f);   // 선택: 밝은 파랑
                else
                    slotImg.color = new Color(0.1f, 0.13f, 0.26f, 1f);    // 기본

                // 기존 표시 제거
                foreach (Transform child in slot.transform)
                {
                    if (child.name == "Icon" || child.name == "Name" || child.name == "Lv" ||
                        child.name == "SelectMark")
                        Destroy(child.gameObject);
                }

                // RemoveBtn 위치 조정 (내용 위로)
                var rm = slot.transform.Find("RemoveBtn");
                if (rm != null) rm.gameObject.SetActive(itemData != null);

                // 선택 표시 (슬롯 번호 옆에 [선택] 라벨)
                if (isSelected)
                {
                    var mark = CreateLabelIn("SelectMark", slot.transform, new Vector2(0, 1),
                        new Vector2(40, -4), new Vector2(80, 20), "선택됨", 10, TextAlignmentOptions.Left);
                    mark.color = new Color(1f, 0.84f, 0f);
                }

                if (itemData != null)
                {
                    // 아이콘 — ⭐ 레벨에 맞는 스프라이트 사용 (머지보드와 동일 기준)
                    var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    icon.transform.SetParent(slot.transform, false);
                    var irt = icon.GetComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0.5f, 0.55f);
                    irt.anchorMax = new Vector2(0.5f, 0.55f);
                    irt.sizeDelta = new Vector2(44, 44);
                    irt.anchoredPosition = Vector2.zero;
                    var iImg = icon.GetComponent<Image>();
                    var lvSprite = SpiritData.ResolveLevelSprite(itemData.element, itemData.level);
                    iImg.sprite = lvSprite ?? (itemData.spiritData != null ? itemData.spiritData.sprite : null);
                    if (iImg.sprite != null) iImg.color = Color.white;
                    else iImg.color = GetElementColor(itemData.element);
                    iImg.raycastTarget = false;

                    // 이름
                    var nameT = CreateLabelIn("Name", slot.transform, new Vector2(0.5f, 0.25f),
                        new Vector2(0, 0), new Vector2(120, 20), itemData.spiritName, 11, TextAlignmentOptions.Center);
                    nameT.color = Color.white;

                    // 레벨
                    var lvT = CreateLabelIn("Lv", slot.transform, new Vector2(0.5f, 0.06f),
                        new Vector2(0, 0), new Vector2(120, 18), $"Lv.{itemData.level}", 9, TextAlignmentOptions.Center);
                    lvT.color = new Color(0.75f, 0.8f, 0.95f);
                }
                else
                {
                    // 빈 슬롯
                    var plus = CreateLabelIn("Icon", slot.transform, new Vector2(0.5f, 0.55f),
                        new Vector2(0, 0), new Vector2(80, 30), "+", 26, TextAlignmentOptions.Center);
                    plus.color = new Color(0.35f, 0.4f, 0.6f);
                    var emptyLabel = CreateLabelIn("Name", slot.transform, new Vector2(0.5f, 0.25f),
                        new Vector2(0, 0), new Vector2(120, 20), "빈 슬롯", 10, TextAlignmentOptions.Center);
                    emptyLabel.color = new Color(0.4f, 0.45f, 0.62f);
                }
            }

            // 정보 패널 갱신 — ⭐ SpiritItemData(성급=level) 기반으로 전투력 계산 (머지 성급 상승 반영)
            var partyData = board.GetPartyItems();
            int power = 0;
            for (int i = 0; i < partyData.Length; i++)
            {
                if (partyData[i] == null) continue;
                power += partyData[i].FinalATK + partyData[i].FinalHP;
            }
            powerText.text = power.ToString("N0");
            countText.text = $"{partyData.Length}/4";

            // ⭐ 시너지 표시 갱신 (색 블록 + 이름 항목 — 효과 설명은 ToolTip에서)
            foreach (Transform c in synergyItemsRoot) Destroy(c.gameObject);
            if (GameManager.Instance != null)
            {
                var items = GameManager.Instance.GetSynergyItems();
                if (items == null || items.Count == 0)
                {
                    var t = CreateLabelIn("SynergyText", synergyItemsRoot, new Vector2(0.5f, 0.5f),
                        Vector2.zero, new Vector2(300, 24), "시너지 없음", 12, TextAlignmentOptions.Center);
                    t.color = new Color(0.6f, 0.7f, 0.9f);
                }
                else
                {
                    // ⭐ 색 블록만 표시 (이름 텍스트 제거) — 중앙 정렬, 패널 밖 안 나감
                    int total = 0;
                    foreach (var (elem, count, txt) in items) total += count;
                    const float spacing = 0.07f; // 블록 간격 (패널 폭 대비)
                    float startX = 0.5f - (total - 1) * spacing * 0.5f;
                    int idx = 0;
                    foreach (var (elem, count, txt) in items)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var dot = new GameObject("SynElem", typeof(RectTransform), typeof(Image));
                            dot.transform.SetParent(synergyItemsRoot, false);
                            var drt = dot.GetComponent<RectTransform>();
                            drt.anchorMin = new Vector2(startX + idx * spacing, 0.5f);
                            drt.anchorMax = drt.anchorMin;
                            drt.sizeDelta = new Vector2(16, 16);
                            dot.GetComponent<Image>().color = GetElementColor(elem);
                            idx++;
                        }
                    }
                }
            }

            // (속성 구성 PartyElems 제거 — 시너지 항목이 대체)

            // 빈 파티 배너
            bool empty = board.IsPartyEmpty();
            pauseBanner.gameObject.SetActive(empty);

            // ⭐ 조작 힌트 — 빈 파티가 아닐 때만 (배너와 안 겹치게)
            bool spiritSelected = board.HasSelectedSpirit;
            if (!empty && spiritSelected)
            {
                hintText.text = "보드에서 정령 선택됨 — 배치할 파티 슬롯을 누르세요";
                hintText.gameObject.SetActive(true);
            }
            else if (!empty && board.SelectedPartySlot >= 0)
            {
                hintText.text = "보드에서 원하는 정령을 선택하세요";
                hintText.gameObject.SetActive(true);
            }
            else
            {
                hintText.gameObject.SetActive(false);
            }
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
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = Mathf.RoundToInt(fontSize * 1.3f); // ⭐ 폰트 30% 확대
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            // ⭐ 화면 비율 대응: 지정 영역 안에서 폰트 자동 축소/확대
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Min(8, fontSize);
            tmp.fontSizeMax = Mathf.RoundToInt(fontSize * 1.3f);
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (font != null) tmp.font = font;
            return tmp;
        }

        /// <summary>버튼 눌림(ColorTint) 애니메이션 — 기본색 유지 + 호버/눌림 밝기 변화</summary>
        static void SetBtnPress(Button btn)
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

        static Color GetElementColor(ElementType element)
        {
            return element switch
            {
                ElementType.Fire  => new Color(1f, 0.4f, 0.2f),
                ElementType.Water => new Color(0.2f, 0.6f, 1f),
                ElementType.Wind  => new Color(0.3f, 1f, 0.3f),
                ElementType.Earth => new Color(0.6f, 0.4f, 0.2f),
                ElementType.Dark  => new Color(0.5f, 0.2f, 0.7f),
                ElementType.Light => new Color(1f, 1f, 0.5f),
                _                 => Color.gray
            };
        }
    }
}

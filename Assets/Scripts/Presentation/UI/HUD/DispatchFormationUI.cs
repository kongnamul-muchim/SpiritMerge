using System;
using SpiritMerge.Core.Systems;
using SpiritMerge.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge
{
    /// <summary>
    /// 파견 배치 오버레이 v2 — 의뢰에 정령 2마리를 배치해서 파견
    /// - 파견 슬롯 2개 (1행 2열) = 보낼 정령 2마리
    /// - 배치: 클릭(보드 선택 → 슬롯 클릭) or 드래그(보드 → 슬롯 드롭)
    /// - 조건(속성/성급)은 추가 보상용 — 매칭 시 금 테두리+보너스, 아무 정령이나 가능
    /// - "파견 보내기" 버튼으로 확정 (보낸 정령 2마리 보드에서 제거)
    /// </summary>
    public class DispatchFormationUI : MonoBehaviour
    {
        private GameManager gm;
        private DispatchService dispatch;
        private MergeBoardManager board;
        private DispatchRequest offer;

        private TextMeshProUGUI infoText;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI hintText;
        private GameObject slot0, slot1;
        private int _slot1Board = -1;   // 슬롯1에 배치된 보드 정령 인덱스
        private int _slot2Board = -1;   // 슬롯2에 배치된 보드 정령 인덱스
        private Button sendBtn;
        private TMP_FontAsset _font;
        private int _lastSel = -2;

        /// <summary>파견 성공 시 호출 (RequestUI가 의뢰 목록 갱신 + 탭 복귀)</summary>
        public Action OnDispatched;

        public static DispatchFormationUI Create(Transform parent, GameManager gm, DispatchRequest offer)
        {
            var go = new GameObject("DispatchFormationOverlay", typeof(RectTransform), typeof(Image), typeof(DispatchFormationUI));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.12f, 1f); // ⭐ 배경 불투명 — 파견 UI만 선명하게 (BattleArea만 덮음, 머지 보드는 보임)
            go.GetComponent<Image>().raycastTarget = false; // ⭐ 배경 클릭 통과 — 머지 보드 조작 가능

            var ui = go.GetComponent<DispatchFormationUI>();
            ui.gm = gm;
            ui.dispatch = gm.dispatch;
            ui.board = UnityEngine.Object.FindAnyObjectByType<MergeBoardManager>();
            ui.offer = offer;
            ui.Build();
            go.SetActive(false);
            return ui;
        }

        public void SetOffer(DispatchRequest req)
        {
            offer = req;
            _slot1Board = -1;
            _slot2Board = -1;
            _lastSel = -2;
            Refresh();
        }

        void Build()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");

            // 제목
            titleText = CreateLabel("TitleText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -10), new Vector2(400, 28), "파견 — 정령 2마리 배치", 17, TextAlignmentOptions.Center);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;

            // 의뢰 정보 (조건/보상) — 슬롯 위, 제목과 겹치지 않게 아래로 + 크기 축소
            infoText = CreateLabel("InfoText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -70), new Vector2(420, 26), "", 12, TextAlignmentOptions.Center);
            infoText.color = new Color(0.75f, 0.8f, 1f);

            // 파견 슬롯 2개 (1행 2열 — BattleArea 기준, 파티 편성과 같은 영역)
            slot0 = MakeSlot(0, 0.06f, 0.30f, 0.50f, 0.50f);
            slot1 = MakeSlot(1, 0.53f, 0.30f, 0.97f, 0.50f);

            // 파견 보내기 버튼 (슬롯 아래 — BattleArea 중심 기준)
            sendBtn = CreateButton("SendBtn", "파견 보내기", 16, SendDispatch, new Vector2(0, -110), 200);
            sendBtn.interactable = false;

            // 힌트 (BattleArea 하단 고정 — 버튼 아래, MergeArea(414) 위, 안 겹치게)
            hintText = CreateLabel("HintText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 12), new Vector2(560, 24),
                "보드에서 정령을 클릭해 선택 후 슬롯을 누르거나, 드래그해서 슬롯에 놓으세요 (조건은 보너스용)", 11, TextAlignmentOptions.Center);
            hintText.color = new Color(0.6f, 0.8f, 1f);

            // 닫기(X) — 의뢰 탭 복귀
            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(transform, false);
            closeGo.transform.SetAsLastSibling();
            var crt = closeGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-8, -8);
            crt.sizeDelta = new Vector2(40, 40);
            closeGo.GetComponent<Image>().color = new Color(0.9f, 0.28f, 0.3f, 1f);
            var cb = closeGo.GetComponent<Button>();
            SetButtonColors(cb);
            cb.onClick.AddListener(() => { gameObject.SetActive(false); if (gm != null) gm.ShowRequestTab(); });
            var cx = CreateLabelIn("X", closeGo.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(38, 38), "X", 18, TextAlignmentOptions.Center);
            cx.color = Color.white;
            cx.fontStyle = FontStyles.Bold;

            Refresh();
        }

        GameObject MakeSlot(int idx, float xMin, float yMin, float xMax, float yMax)
        {
            var slot = new GameObject("DispatchSlot_" + idx, typeof(RectTransform), typeof(Image), typeof(Button));
            slot.transform.SetParent(transform, false);
            var srt = slot.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(xMin, yMin);
            srt.anchorMax = new Vector2(xMax, yMax);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            var img = slot.GetComponent<Image>();
            img.color = new Color(0.10f, 0.13f, 0.26f, 1f);
            img.raycastTarget = true;
            var btn = slot.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            return slot;
        }

        void Update()
        {
            int sel = board != null ? board.SelectedSlotIndex : -1;
            if (sel != _lastSel)
            {
                _lastSel = sel;
                Refresh();
            }
        }

        void Refresh()
        {
            if (infoText != null && offer != null)
            {
                float mult = dispatch.stageMultiplier;
                string cond = "";
                foreach (var s in offer.slots) cond += $"[{s.requiredElement} {s.minGrade}★ 이상] ";
                infoText.text = $"조건(보너스): {cond} | 보상: 골드 {Mathf.RoundToInt(offer.goldReward * mult):N0} / 루비 {Mathf.RoundToInt(offer.rubyReward * mult)} (배율 x{mult:0.00})";
            }

            RefreshSlot(slot0, _slot1Board, 0);
            RefreshSlot(slot1, _slot2Board, 1);

            if (sendBtn != null)
                sendBtn.interactable = _slot1Board >= 0 && _slot2Board >= 0;
        }

        void RefreshSlot(GameObject slot, int boardIdx, int slotNum)
        {
            if (slot == null) return;
            foreach (Transform c in slot.transform) Destroy(c.gameObject);
            var outline = slot.GetComponent<Outline>();
            if (outline != null) Destroy(outline);

            if (boardIdx >= 0)
            {
                var d = board != null ? board.GetItemData(boardIdx) : null;
                if (d == null) { if (slotNum == 0) _slot1Board = -1; else _slot2Board = -1; return; }
                int grade = d.level;
                bool match = offer != null && dispatch.MatchCount(offer, d.element, grade) > 0;
                CreateLabelAreaIn("Info", slot.transform, new Vector2(0.03f, 0.1f), new Vector2(0.78f, 0.9f),
                    $"[슬롯 {slotNum + 1}] {d.spiritName}({d.element} {grade}성){(match ? " 보너스" : "")}", 12, TextAlignmentOptions.MidlineLeft);
                CreateCardButton("X", "해제", 12, () => { if (slotNum == 0) _slot1Board = -1; else _slot2Board = -1; Refresh(); }, slot.transform);
                if (match)
                {
                    var o = slot.AddComponent<Outline>();
                    o.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
                    o.effectDistance = new Vector2(2.5f, -2.5f);
                }
            }
            else
            {
                CreateLabelAreaIn("Info", slot.transform, new Vector2(0.03f, 0.1f), new Vector2(0.92f, 0.9f),
                    $"[슬롯 {slotNum + 1}] 보드에서 정령을 선택/드래그해 배치", 12, TextAlignmentOptions.MidlineLeft);
            }

            // 슬롯 클릭 → 선택된 보드 정령 배치/해제 (토글)
            // ⭐ 정령 1개당 1개 슬롯만 — 다른 슬롯에 이미 배치된 정령이면 그 슬롯 해제 후 배치
            var btn = slot.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                int sel = board != null ? board.SelectedSlotIndex : -1;
                if (sel < 0) return;
                if (slotNum == 0)
                {
                    if (_slot1Board == sel) { _slot1Board = -1; }
                    else
                    {
                        if (_slot2Board == sel) _slot2Board = -1;
                        _slot1Board = sel;
                        board.DeselectCurrent(); // ⭐ 배치 완료 → 선택 해제
                    }
                }
                else
                {
                    if (_slot2Board == sel) { _slot2Board = -1; }
                    else
                    {
                        if (_slot1Board == sel) _slot1Board = -1;
                        _slot2Board = sel;
                        board.DeselectCurrent(); // ⭐ 배치 완료 → 선택 해제
                    }
                }
                Refresh();
            });
        }

        /// <summary>드래그 드롭 배치 — MergeBoardManager.OnDragEnd에서 호출
        /// ⭐ 정령 1개당 1개 슬롯만 (다른 슬롯에 있으면 그 슬롯 해제)</summary>
        public bool TryDropOnSlot(int boardIdx, Vector2 pointerPos)
        {
            if (IsInside(slot0, pointerPos))
            {
                if (_slot2Board == boardIdx) _slot2Board = -1;
                _slot1Board = boardIdx;
                board.DeselectCurrent(); // ⭐ 드롭 배치 완료 → 선택 해제
                Refresh();
                return true;
            }
            if (IsInside(slot1, pointerPos))
            {
                if (_slot1Board == boardIdx) _slot1Board = -1;
                _slot2Board = boardIdx;
                board.DeselectCurrent(); // ⭐ 드롭 배치 완료 → 선택 해제
                Refresh();
                return true;
            }
            return false;
        }

        bool IsInside(GameObject slot, Vector2 pos)
        {
            if (slot == null) return false;
            var corners = new Vector3[4];
            slot.GetComponent<RectTransform>().GetWorldCorners(corners);
            return pos.x >= corners[0].x && pos.x <= corners[2].x
                && pos.y >= corners[0].y && pos.y <= corners[2].y;
        }

        void SendDispatch()
        {
            if (_slot1Board < 0 || _slot2Board < 0 || offer == null || board == null) return;
            var d1 = board.GetItemData(_slot1Board);
            var d2 = board.GetItemData(_slot2Board);
            if (d1 == null || d2 == null) return;
            int g1 = d1.level, g2 = d2.level;

            if (dispatch.TryStart(offer, d1.spiritName, d1.element, g1, d2.spiritName, d2.element, g2, gm.dispatchTimeScale))
            {
                board.RemoveBoardSpirit(_slot1Board);
                board.RemoveBoardSpirit(_slot2Board);
                gm.OnDispatched();
                GameLogger.Info($"[Request] 파견 보내기: {d1.spiritName}, {d2.spiritName} → 보상 대기 (배율 x{dispatch.stageMultiplier:0.00})");
                gameObject.SetActive(false);
                OnDispatched?.Invoke();
            }
        }

        /// <summary>CLI 진단 — 화면 배치 좌표 (실제 겹침 여부 파악)</summary>
        public void LogState()
        {
            GameLogger.Info($"[Request] 파견배치 active={gameObject.activeSelf} 슬롯1={_slot1Board} 슬롯2={_slot2Board} 의뢰={(offer != null ? "O" : "X")}");
            LogRect("파견UI", gameObject);
            LogRect("제목", titleText != null ? titleText.gameObject : null);
            LogRect("의뢰정보", infoText != null ? infoText.gameObject : null);
            LogRect("슬롯1", slot0);
            LogRect("슬롯2", slot1);
            LogRect("보내기버튼", sendBtn != null ? sendBtn.gameObject : null);
            LogRect("힌트", hintText != null ? hintText.gameObject : null);
            LogRect("MergeArea", GameObject.Find("MergeArea"));
            LogRect("BattleArea", GameObject.Find("BattleArea"));
            LogRect("TopBar", GameObject.Find("TopBar"));
            LogRect("BottomMenu", GameObject.Find("BottomMenu"));
        }

        void LogRect(string name, GameObject go)
        {
            if (go == null) { GameLogger.Info($"[Layout] {name}: 없음"); return; }
            var corners = new Vector3[4];
            go.GetComponent<RectTransform>().GetWorldCorners(corners);
            GameLogger.Info($"[Layout] {name}: ({corners[0].x:0},{corners[0].y:0})~({corners[2].x:0},{corners[2].y:0})");
        }

        /// <summary>CLI 검증용 — 보낼 정령 배치 후 보내기 시뮬레이션</summary>
        public void TestDispatchClick()
        {
            if (_slot1Board < 0 && _slot2Board < 0 && board != null)
            {
                for (int i = 0; i < 16; i++)
                    if (board.GetItemData(i) != null) { if (_slot1Board < 0) _slot1Board = i; else if (_slot2Board < 0) { _slot2Board = i; break; } }
            }
            SendDispatch();
        }

        // ══════════════════════════════════════════
        // UI 헬퍼
        // ══════════════════════════════════════════

        static int FS(int size) => Mathf.RoundToInt(size * 1.3f);

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
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return StyleText(go.GetComponent<TextMeshProUGUI>(), text, fontSize, align);
        }

        TextMeshProUGUI CreateLabelAreaIn(string name, Transform parent, Vector2 aMin, Vector2 aMax, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return StyleText(go.GetComponent<TextMeshProUGUI>(), text, fontSize, align);
        }

        TextMeshProUGUI StyleLabel(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, string text, int fontSize, TextAlignmentOptions align)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
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

        Button CreateButton(string name, string label, int fs, Action onClick, Vector2 pos = default, float width = 140, Transform parent = null)
        {
            var p = parent != null ? parent : transform;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(p, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos == default ? new Vector2(0, 0) : pos;
            rt.sizeDelta = new Vector2(width, 34);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.8f, 1f);
            var b = go.GetComponent<Button>();
            SetButtonColors(b);
            b.onClick.AddListener(() => onClick?.Invoke());
            CreateLabelAreaIn("Label", go.transform, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f), label, fs, TextAlignmentOptions.Center);
            return b;
        }

        Button CreateCardButton(string name, string label, int fs, Action onClick, Transform card)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(card, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-10, 0);
            rt.sizeDelta = new Vector2(62, 30);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.45f, 0.8f, 1f);
            var b = go.GetComponent<Button>();
            SetButtonColors(b);
            b.onClick.AddListener(() => onClick?.Invoke());
            CreateLabelAreaIn("Label", go.transform, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f), label, fs, TextAlignmentOptions.Center);
            return b;
        }

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
    }
}

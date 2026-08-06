using System;
using System.Collections.Generic;
using SpiritMerge.Battle;
using SpiritMerge.Core.Systems;
using SpiritMerge.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge
{
    /// <summary>
    /// 의뢰 오버레이 v4 — 업그레이드 탭과 동일한 패턴
    /// - 상단: 제목 / 탭 3개(파견·미션·레이드) / 닫기
    /// - 파견: 의뢰 목록 → 파견 전용 모달(보드 정령 배치, 조건 매칭 금 테두리, 스테이지 비례 보상)
    /// - 미션: 일일/주간 가로 탭 + 10종 카드
    /// - 레이드: 보스 정보 + 보상 Tooltip 패널 + 실전투
    /// </summary>
    public class RequestUI : MonoBehaviour
    {
        private GameManager gm;
        private DispatchService dispatch;
        private MissionService missions;
        private RaidService raid;
        private TMP_FontAsset _font;

        private int currentTab = 0; // 0=파견, 1=미션, 2=레이드
        private bool missionWeekly;
        private Image[] tabBgs = new Image[3];

        private RectTransform dispatchContent, missionContent, raidPanel;
        private RectTransform missionTabRow;   // 일일/주간 가로 탭 행
        private RectTransform rewardPanel;     // 레이드 보상 Tooltip
        private RectTransform dispatchModal;   // 파견 전용 모달
        private RectTransform modalContent;
        private TextMeshProUGUI modalInfo;     // 파견 모달 조건/보상 (슬롯 위)
        private GameObject dispatchSlot0, dispatchSlot1; // 파견 슬롯 2개 (2행 1열)
        private TextMeshProUGUI raidStatusText;
        private string _rewardPanelType = "";
        private DispatchRequest _dispatchOffer;
        private int _selectedSlot = -1;        // 파견 모달에서 선택된 보드 정령 슬롯 (파티 편성처럼 선택→배치)

        private readonly List<DispatchRequest> offers = new List<DispatchRequest>();

        public static RequestUI Create(Transform parent, GameManager gm)
        {
            var go = new GameObject("RequestOverlay", typeof(RectTransform), typeof(Image), typeof(RequestUI));
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

            var ui = go.GetComponent<RequestUI>();
            ui.gm = gm;
            ui.dispatch = gm.dispatch;
            ui.missions = gm.missions;
            ui.raid = gm.raid;
            ui.Build();
            go.SetActive(false);
            return ui;
        }

        void Build()
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");

            // ── 제목 ──
            var title = CreateLabel("TitleText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -10), new Vector2(400, 28), "의뢰", 20, TextAlignmentOptions.Center);
            title.rectTransform.pivot = new Vector2(0.5f, 1f); // ⭐ 위쪽 가장자리 기준 (상반 잘림 방지)
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;

            // ── 탭 3개 ──
            tabBgs[0] = CreateTabButton("TabDispatch", 0.14f, "파견", 0);
            tabBgs[1] = CreateTabButton("TabMission", 0.40f, "미션", 1);
            tabBgs[2] = CreateTabButton("TabRaid", 0.66f, "레이드", 2);

            // ── 닫기 ──
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
            cBtn.onClick.AddListener(() => gm.ShowBattleTab());
            var cTxt = CreateLabelIn("X", closeGo.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(40, 40), "X", 20, TextAlignmentOptions.Center);
            cTxt.color = Color.white;
            cTxt.fontStyle = FontStyles.Bold;

            // ── 파견 스크롤 ──
            dispatchContent = CreateScrollContent("DispatchScroll");
            BuildDispatchContent();
            // ⭐ 초기 의뢰 2개 (슬롯 2개, 쿨다운 적용)
            dispatch.TryGetNewOffer();
            dispatch.TryGetNewOffer();

            // ── 미션 스크롤 + 일일/주간 탭 행 ──
            missionContent = CreateScrollContent("MissionScroll");
            missionTabRow = new GameObject("MissionTabRow", typeof(RectTransform)).GetComponent<RectTransform>();
            missionTabRow.transform.SetParent(transform, false);
            missionTabRow.anchorMin = new Vector2(0.06f, 0.82f);
            missionTabRow.anchorMax = new Vector2(0.94f, 0.88f);
            missionTabRow.offsetMin = Vector2.zero;
            missionTabRow.offsetMax = Vector2.zero;
            CreateButton("TabDaily", "일일", 14, () => { missionWeekly = false; RefreshMission(); }, missionTabRow, new Vector2(-110, 0), 150);
            CreateButton("TabWeekly", "주간", 14, () => { missionWeekly = true; RefreshMission(); }, missionTabRow, new Vector2(110, 0), 150);

            // ── 레이드 패널 ──
            raidPanel = new GameObject("RaidPanel", typeof(RectTransform)).GetComponent<RectTransform>();
            raidPanel.transform.SetParent(transform, false);
            raidPanel.anchorMin = new Vector2(0.06f, 0.12f);
            raidPanel.anchorMax = new Vector2(0.94f, 0.80f);
            raidPanel.offsetMin = Vector2.zero;
            raidPanel.offsetMax = Vector2.zero;
            BuildRaidPanel();

            Refresh();
        }

        public void Refresh()
        {
            bool d = currentTab == 0, m = currentTab == 1, r = currentTab == 2;
            if (dispatchContent != null) dispatchContent.gameObject.SetActive(d);
            if (missionContent != null) missionContent.gameObject.SetActive(m);
            if (missionTabRow != null) missionTabRow.gameObject.SetActive(m);
            if (raidPanel != null) raidPanel.gameObject.SetActive(r);
            if (rewardPanel != null) rewardPanel.gameObject.SetActive(false); // 탭 이탈 시 보상 패널 닫기

            for (int i = 0; i < tabBgs.Length; i++)
                if (tabBgs[i] != null)
                    tabBgs[i].color = i == currentTab ? new Color(0.16f, 0.30f, 0.55f, 1f) : new Color(0.08f, 0.10f, 0.20f, 1f);

            if (d) RefreshDispatch();
            else if (m) RefreshMission();
            else RefreshRaid();
        }

        public void ShowTab(int tab)
        {
            currentTab = tab;
            Refresh();
        }

        // ── 파견 ────────────────────────────────

        void BuildDispatchContent()
        {
            var header = CreateLabelIn("Header", dispatchContent, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(900, 28), "정령을 보내 머지칸을 비우고 보상 획득", 13, TextAlignmentOptions.Center);
            header.color = new Color(0.75f, 0.8f, 1f);
            CreateButton("NewOffer", "새 의뢰", 13, () =>
            {
                if (!dispatch.TryGetNewOffer())
                    GameLogger.Warn("[Request] 새 의뢰 불가 (슬롯 꽉 참 또는 쿨다운)");
                RefreshDispatch();
            });
        }

        void RefreshDispatch()
        {
            foreach (Transform c in dispatchContent)
                if (c.name.StartsWith("Offer") || c.name.StartsWith("Active") || c.name.StartsWith("Done") || c.name.StartsWith("SlotInfo")) Destroy(c.gameObject);

            // ⭐ 슬롯/쿨다운 상태
            string cd = dispatch.requestCooldownTimer > 0 ? $"새 의뢰 {dispatch.requestCooldownTimer:0}초" : "새 의뢰 가능";
            CreateLabelIn("SlotInfo", dispatchContent, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900, 24),
                $"슬롯 {dispatch.UsedSlots}/2 — {cd}", 12, TextAlignmentOptions.Center);

            // 의뢰 목록 (dispatch.offers)
            for (int i = 0; i < dispatch.offers.Count; i++)
            {
                var req = dispatch.offers[i];
                string cond = "";
                foreach (var s in req.slots) cond += $"[{s.requiredElement} {s.minGrade}★ 이상] ";
                int fi = i;
                var card = CreateCard("Offer_" + i, dispatchContent);
                CreateLabelAreaIn("Cond", card.transform, new Vector2(0.02f, 0.1f), new Vector2(0.36f, 0.9f),
                    $"의뢰 {i + 1}: {cond}", 12, TextAlignmentOptions.MidlineLeft);
                CreateLabelAreaIn("Reward", card.transform, new Vector2(0.38f, 0.1f), new Vector2(0.70f, 0.9f),
                    $"보상 골드 {req.goldReward:N0} / 루비 {req.rubyReward} ({req.durationHours}h)", 12, TextAlignmentOptions.Center);
                CreateCardButton("Btn", "파견", 13, () => OpenDispatchFormation(fi), card.transform);
            }

            // ── 파견 중 (Active — 정령 2마리) ──
            for (int i = 0; i < dispatch.Active.Count; i++)
            {
                var e = dispatch.Active[i];
                var card = CreateCard("Active_" + i, dispatchContent);
                CreateLabelAreaIn("Info", card.transform, new Vector2(0.02f, 0.1f), new Vector2(0.70f, 0.9f),
                    $"[파견 {i + 1}] {e.Spirit1Name}, {e.Spirit2Name} — {e.RemainingSeconds:0}s 남음", 12, TextAlignmentOptions.MidlineLeft);
            }

            // ── 완료 (Completed — 보상 받기) ──
            for (int i = 0; i < dispatch.Completed.Count; i++)
            {
                var e = dispatch.Completed[i];
                int ci = i;
                var card = CreateCard("Done_" + i, dispatchContent);
                CreateLabelAreaIn("Info", card.transform, new Vector2(0.02f, 0.1f), new Vector2(0.70f, 0.9f),
                    $"[완료] {e.Spirit1Name}, {e.Spirit2Name} — 보상 수령", 12, TextAlignmentOptions.MidlineLeft);
                CreateCardButton("Btn", "보상", 13, () => ClaimDispatch(ci), card.transform);
            }
        }

        // ── 파견 전용 모달 (편성형) ────────────────

        void BuildDispatchModal()
        {
            dispatchModal = new GameObject("DispatchModal", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            dispatchModal.transform.SetParent(transform, false);
            dispatchModal.transform.SetAsLastSibling();
            dispatchModal.anchorMin = new Vector2(0.02f, 0.02f);
            dispatchModal.anchorMax = new Vector2(0.98f, 0.98f);
            dispatchModal.offsetMin = Vector2.zero;
            dispatchModal.offsetMax = Vector2.zero;
            dispatchModal.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.98f);
            dispatchModal.gameObject.SetActive(false);

            var title = CreateLabelIn("Title", dispatchModal, new Vector2(0.5f, 1f),
                new Vector2(0, -10), new Vector2(400, 28), "파견 — 보드에서 정령 선택", 18, TextAlignmentOptions.Center);
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;

            // 닫기(X)
            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(dispatchModal, false);
            var crt = closeGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-8, -8);
            crt.sizeDelta = new Vector2(40, 40);
            closeGo.GetComponent<Image>().color = new Color(0.9f, 0.28f, 0.3f, 1f);
            var closeBtn = closeGo.GetComponent<Button>();
            SetButtonColors(closeBtn);
            closeBtn.onClick.AddListener(CloseDispatchModal);
            var closeTxt = CreateLabelIn("X", closeGo.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(38, 38), "X", 18, TextAlignmentOptions.Center);
            closeTxt.color = Color.white;
            closeTxt.fontStyle = FontStyles.Bold;

            // ── 조건/보상 (슬롯 위, 고정) ──
            modalInfo = CreateLabelIn("Info", dispatchModal, new Vector2(0.5f, 1f),
                new Vector2(0, -48), new Vector2(900, 24), "", 12, TextAlignmentOptions.Center);

            // ── 파견 슬롯 2개 (2행 1열 — 편성 슬롯 비율) ──
            dispatchSlot0 = MakeDispatchSlot(0, 0.56f, 0.80f);
            dispatchSlot1 = MakeDispatchSlot(1, 0.28f, 0.52f);

            // ── 보드 정령 목록 (하단 스크롤) ──
            modalContent = CreateScrollContentIn("ModalScroll", dispatchModal,
                new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.24f));
        }

        GameObject MakeDispatchSlot(int slotIdx, float yMin, float yMax)
        {
            var slot = new GameObject("Slot_" + slotIdx, typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(dispatchModal, false);
            var srt = slot.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.15f, yMin);
            srt.anchorMax = new Vector2(0.85f, yMax);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            var img = slot.GetComponent<Image>();
            img.color = new Color(0.10f, 0.13f, 0.26f, 1f);
            // ⭐ 빈 슬롯 클릭 → 선택된 보드 정령 파견 (파티 편성과 동일 인터랙션)
            var btn = slot.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => { if (_selectedSlot >= 0) ConfirmDispatch(_selectedSlot); });
            return slot;
        }

        void RefreshDispatchSlot(int slotIdx, GameObject slot)
        {
            if (slot == null) return;
            foreach (Transform c in slot.transform) Destroy(c.gameObject);

            bool filled = slotIdx < dispatch.Active.Count;
            var e = filled ? dispatch.Active[slotIdx] : null;
            if (filled)
            {
                CreateLabelAreaIn("Info", slot.transform, new Vector2(0.02f, 0.1f), new Vector2(0.80f, 0.9f),
                    $"[슬롯 {slotIdx + 1}] {e.Spirit1Name}, {e.Spirit2Name} — {e.RemainingSeconds:0}s 남음", 12, TextAlignmentOptions.MidlineLeft);
            }
            else
            {
                CreateLabelAreaIn("Info", slot.transform, new Vector2(0.02f, 0.1f), new Vector2(0.80f, 0.9f),
                    $"[슬롯 {slotIdx + 1}] 보드에서 정령 선택 후 이 슬롯을 눌러 파견", 12, TextAlignmentOptions.MidlineLeft);
            }
        }

        void OpenDispatchModal(int offerIdx)
        {
            if (offerIdx < 0 || offerIdx >= offers.Count) return;
            _dispatchOffer = offers[offerIdx];
            _selectedSlot = -1;
            ApplyStageMultiplier();
            dispatchModal.gameObject.SetActive(true);
            RefreshDispatchModal();
        }

        void CloseDispatchModal()
        {
            _dispatchOffer = null;
            _selectedSlot = -1;
            if (dispatchModal != null) dispatchModal.gameObject.SetActive(false);
        }

        void RefreshDispatchModal()
        {
            if (_dispatchOffer == null) return;

            // ── 조건/보상 (슬롯 위) ──
            if (modalInfo != null)
            {
                float mult = dispatch.stageMultiplier;
                string cond = "";
                foreach (var s in _dispatchOffer.slots) cond += $"[{s.requiredElement} {s.minGrade}★] ";
                modalInfo.text = $"조건: {cond} | 보상: 골드 {Mathf.RoundToInt(_dispatchOffer.goldReward * mult):N0} / 루비 {Mathf.RoundToInt(_dispatchOffer.rubyReward * mult)} (배율 x{mult:0.00})";
            }

            // ── 파견 슬롯 2개 갱신 ──
            RefreshDispatchSlot(0, dispatchSlot0);
            RefreshDispatchSlot(1, dispatchSlot1);

            // ── 보드 정령 카드 (하단 스크롤 — 클릭 → 선택) ──
            foreach (Transform c in modalContent) Destroy(c.gameObject);

            var board = UnityEngine.Object.FindAnyObjectByType<MergeBoardManager>();
            int total = 0, matchCount = 0;
            for (int i = 0; i < 16; i++)
            {
                var d = board != null ? board.GetItemData(i) : null;
                if (d == null) continue;
                int grade = d.spiritData != null ? (int)d.spiritData.grade : 1;
                bool match = dispatch.MatchCount(_dispatchOffer, d.element, grade) > 0;
                total++;
                if (match) matchCount++;
                int si = i;
                var card = CreateCard("Spirit_" + i, modalContent);
                if (match)
                {
                    // ⭐ 조건 일치 → 금 테두리
                    var outline = card.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
                    outline.effectDistance = new Vector2(2.5f, -2.5f);
                }
                if (_selectedSlot == si)
                    card.GetComponent<Image>().color = new Color(0.22f, 0.38f, 0.6f, 1f); // 선택 하이라이트
                CreateLabelAreaIn("Info", card.transform, new Vector2(0.02f, 0.1f), new Vector2(0.70f, 0.9f),
                    $"[{i}] {d.spiritName} ({d.element} {grade}★) {(match ? "매칭" : "불일치")}", 12, TextAlignmentOptions.MidlineLeft);
                // 클릭 → 선택
                var cardBtn = card.AddComponent<Button>();
                cardBtn.targetGraphic = card.GetComponent<Image>();
                cardBtn.onClick.AddListener(() => { _selectedSlot = si; RefreshDispatchModal(); });
            }
            GameLogger.Info($"[Request] 파견 모달: 보드정령 {total}개, 조건매칭 {matchCount}개 (금테두리)");
        }

        void ConfirmDispatch(int boardSlotIdx)
        {
            var board = UnityEngine.Object.FindAnyObjectByType<MergeBoardManager>();
            var d = board != null ? board.GetItemData(boardSlotIdx) : null;
            if (d == null || _dispatchOffer == null) return;
            int grade = d.spiritData != null ? (int)d.spiritData.grade : 1;
            if (dispatch.MatchCount(_dispatchOffer, d.element, grade) <= 0) return;

            if (dispatch.TryStart(_dispatchOffer, d.spiritName, d.element, grade, d.spiritName, d.element, grade, gm.dispatchTimeScale))
            {
                board.RemoveBoardSpirit(boardSlotIdx);
                gm.OnDispatched();
                GameLogger.Info($"[Request] 파견 시작: {d.spiritName}({d.element} {grade}★) — 보상 대기 (배율 x{dispatch.stageMultiplier:0.00})");
                offers.Remove(_dispatchOffer);
                _dispatchOffer = null;
                CloseDispatchModal();
                RefreshDispatch();
            }
        }

        void ClaimDispatch(int idx)
        {
            if (idx < 0 || idx >= dispatch.Completed.Count) return;
            var (entry, gold, ruby) = dispatch.Claim(idx);
            gm.AddGold(gold);
            gm.AddRuby(ruby);
            GameLogger.Info($"[Request] 파견 보상: {entry.Spirit1Name} 외 1마리 → 골드 +{gold}, 루비 +{ruby}");
            RefreshDispatch();
        }

        /// <summary>스테이지 비례 보상 배율 — 최대 스테이지(5-10)에서 최대(2배)</summary>
        void ApplyStageMultiplier()
        {
            float mult = 1f;
            var stage = gm.GetCurrentStage();
            if (stage != null)
            {
                int idx = (stage.region - 1) * 10 + (stage.stageNumber - 1);
                float ratio = Mathf.Clamp01(idx / 49f);
                mult = 1f + ratio; // 1배 ~ 2배
            }
            dispatch.stageMultiplier = mult;
        }

        // ── 미션 ────────────────────────────────

        void RefreshMission()
        {
            foreach (Transform c in missionContent) Destroy(c.gameObject);

            CreateLabelIn("Header", missionContent, new Vector2(0.5f, 1f), Vector2.zero,
                new Vector2(900, 26), missionWeekly ? "[주간 미션]" : "[일일 미션]", 13, TextAlignmentOptions.Center);

            var defs = missionWeekly ? missions.WeeklyDefs : missions.DailyDefs;
            var progress = missionWeekly ? missions.WeeklyProgress : missions.DailyProgress;
            var claimed = missionWeekly ? missions.WeeklyClaimed : missions.DailyClaimed;

            for (int i = 0; i < MissionService.MissionCount; i++)
            {
                int idx = i;
                var def = defs[i];
                bool done = progress[i] >= def.Target;
                string state = claimed[i] ? "[받음]" : done ? "[완료!]" : $"{Math.Min(progress[i], def.Target)}/{def.Target}";
                var card = CreateCard("Mission_" + i, missionContent);
                CreateLabelAreaIn("Desc", card.transform, new Vector2(0.02f, 0.1f), new Vector2(0.70f, 0.9f),
                    $"{def.Desc}  ({state})  보상: 골드 {def.GoldReward} / 루비 {def.RubyReward}", 12, TextAlignmentOptions.MidlineLeft);
                if (done && !claimed[i])
                    CreateCardButton("Btn", "받기", 13, () => ClaimMission(idx), card.transform);
            }
        }

        void ClaimMission(int idx)
        {
            if (missions.TryClaim(missionWeekly, idx, out int gold, out int ruby))
            {
                gm.AddGold(gold);
                gm.AddRuby(ruby);
                GameLogger.Info($"[Request] 미션 보상: 골드 +{gold}, 루비 +{ruby}");
            }
            RefreshMission();
        }

        // ── 레이드 ────────────────────────────────

        void BuildRaidPanel()
        {
            // ⭐ Status를 패널 가장 위쪽으로 — 아래 여백 확보 (보상 패널 위치용)
            raidStatusText = CreateLabelAreaIn("Status", raidPanel, new Vector2(0.02f, 0.52f), new Vector2(0.98f, 0.97f),
                "", 14, TextAlignmentOptions.Center);

            // ⭐ 보상 Tooltip 패널 — Status 안 위쪽 (raidPanel 자식)
            rewardPanel = new GameObject("RewardPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rewardPanel.transform.SetParent(raidPanel, false);
            rewardPanel.anchorMin = new Vector2(0.30f, 0.78f);
            rewardPanel.anchorMax = new Vector2(0.70f, 0.96f);
            rewardPanel.offsetMin = Vector2.zero;
            rewardPanel.offsetMax = Vector2.zero;
            rewardPanel.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.24f, 0.98f);
            rewardPanel.gameObject.SetActive(false);

            CreateButton("Start", "레이드 시작 (60초)", 16, () =>
            {
                var battle = UnityEngine.Object.FindAnyObjectByType<RaidBattle>();
                if (battle == null) { GameLogger.Error("[Request] RaidBattle 없음"); return; }
                battle.StartRaid();
            }, raidPanel, new Vector2(0, -120), 220);
            CreateButton("ClaimStage", "현재 단계 보상", 13, () => ToggleRewardPanel("stage"), raidPanel, new Vector2(-110, -200), 180);
            CreateButton("ClaimRank", "랭킹 보상", 13, () => ToggleRewardPanel("rank"), raidPanel, new Vector2(110, -200), 180);
        }

        void RefreshRaid()
        {
            if (raidStatusText == null) return;
            int next = Mathf.Min(raid.Stage + 1, RaidService.MaxStage);
            raidStatusText.text =
                $"주간 보스: {raid.WeeklyBossElement} 속성\n" +
                $"현재 페이즈: {raid.Stage} / {RaidService.MaxStage}  |  이번 주 점수: {raid.TotalDamage:N0}\n" +
                $"최고 점수: {raid.BestScore:N0}  |  보스 HP: {RaidService.GetBossHP(raid.Stage):N0}  ATK: {RaidService.GetBossATK(raid.Stage)}\n" +
                $"다음 페이즈: {next}페이즈 HP {RaidService.GetBossHP(next):N0}";
        }

        void ToggleRewardPanel(string type)
        {
            if (rewardPanel == null) return;
            if (_rewardPanelType == type && rewardPanel.gameObject.activeSelf)
            {
                rewardPanel.gameObject.SetActive(false);
                _rewardPanelType = "";
                return;
            }
            _rewardPanelType = type;
            rewardPanel.gameObject.SetActive(true);
            RefreshRewardPanel();
        }

        void RefreshRewardPanel()
        {
            foreach (Transform c in rewardPanel) Destroy(c.gameObject);

            // X 닫기
            var closeGo = new GameObject("X", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(rewardPanel, false);
            var xrt = closeGo.GetComponent<RectTransform>();
            xrt.anchorMin = new Vector2(1f, 1f);
            xrt.anchorMax = new Vector2(1f, 1f);
            xrt.pivot = new Vector2(1f, 1f);
            xrt.anchoredPosition = new Vector2(-4, -4);
            xrt.sizeDelta = new Vector2(26, 26);
            closeGo.GetComponent<Image>().color = new Color(0.9f, 0.28f, 0.3f, 1f);
            closeGo.GetComponent<Button>().onClick.AddListener(() => { rewardPanel.gameObject.SetActive(false); _rewardPanelType = ""; });
            var xt = CreateLabelIn("L", closeGo.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24), "X", 12, TextAlignmentOptions.Center);
            xt.color = Color.white;

            if (_rewardPanelType == "stage")
            {
                // ⭐ 다음 페이즈 보상만 표시 (전체 목록은 텍스트 압축)
                int next = Mathf.Min(raid.Stage + 1, RaidService.MaxStage);
                CreateLabelAreaIn("Title", rewardPanel, new Vector2(0.05f, 0.75f), new Vector2(0.80f, 0.95f),
                    $"다음 페이즈 보상 ({next}페이즈)", 13, TextAlignmentOptions.MidlineLeft);
                CreateLabelAreaIn("List", rewardPanel, new Vector2(0.05f, 0.08f), new Vector2(0.80f, 0.72f),
                    $"{next}페이즈 클리어: 골드 {next * 500} / 루비 {next * 5}\n(주간 1회, 수령 시 재지급 없음)", 12, TextAlignmentOptions.MidlineLeft);
            }
            else
            {
                CreateLabelAreaIn("Title", rewardPanel, new Vector2(0.05f, 0.75f), new Vector2(0.80f, 0.95f),
                    "랭킹 보상 (역대 최고 점수)", 13, TextAlignmentOptions.MidlineLeft);
                int tier = raid.GetRankTier();
                CreateLabelAreaIn("List", rewardPanel, new Vector2(0.05f, 0.08f), new Vector2(0.80f, 0.72f),
                    $"티어 {tier}: 루비 {raid.GetTierRubyReward(tier)}\n(최고 점수 {raid.BestScore:N0})", 12, TextAlignmentOptions.MidlineLeft);
            }
        }

        // ── CLI 진단 ────────────────────────────────

        public void LogState()
        {
            GameLogger.Info($"[Request] 오버레이 activeSelf={gameObject.activeSelf} 탭={currentTab}");
            GameLogger.Info($"[Request] 파견:{(dispatchContent != null ? dispatchContent.gameObject.activeSelf : false)} 미션:{(missionContent != null ? missionContent.gameObject.activeSelf : false)} 레이드:{(raidPanel != null ? raidPanel.gameObject.activeSelf : false)} | 의뢰={dispatch.offers.Count} 파견중={dispatch.Active.Count} 완료={dispatch.Completed.Count} 슬롯={dispatch.UsedSlots}/2 쿨다운={dispatch.requestCooldownTimer:0}");
        }

        /// <summary>CLI 검증용 — 첫 의뢰로 파견 배치 열기</summary>
        public void OpenDispatchModalForTest()
        {
            if (dispatch.offers.Count == 0) dispatch.TryGetNewOffer();
            OpenDispatchFormation(0);
        }

        /// <summary>
        /// 파견 버튼 → 의뢰 화면 닫고 머지 화면(전투 탭)에서 배치 오버레이 표시 (파티 편성과 동일)
        /// </summary>
        void OpenDispatchFormation(int offerIdx)
        {
            if (offerIdx < 0 || offerIdx >= dispatch.offers.Count) return;
            var req = dispatch.offers[offerIdx];
            ApplyStageMultiplier();
            if (gm != null && gm.dispatchFormation != null)
            {
                gameObject.SetActive(false); // ⭐ 의뢰 UI만 숨김 (GNB 탭은 의뢰 유지) — 머지 보드가 보이게
                gm.dispatchFormation.SetOffer(req);
                gm.dispatchFormation.OnDispatched = () =>
                {
                    gm.ShowRequestTab(); // 파견 후 의뢰 탭 복귀
                    RefreshDispatch();
                };
                gm.dispatchFormation.gameObject.SetActive(true);
            }
            else GameLogger.Error("[Request] DispatchFormationUI 없음");
        }

        // ══════════════════════════════════════════
        // UI 헬퍼 (업그레이드 패턴)
        // ══════════════════════════════════════════

        static int FS(int size) => Mathf.RoundToInt(size * 1.3f);

        RectTransform CreateScrollContent(string name) => CreateScrollContentIn(name, transform);

        RectTransform CreateScrollContentIn(string name, Transform parent, Vector2? aMin = null, Vector2? aMax = null)
        {
            var scrollGo = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(parent, false);
            var svRt = scrollGo.GetComponent<RectTransform>();
            svRt.anchorMin = aMin ?? new Vector2(0.06f, 0.10f);
            svRt.anchorMax = aMax ?? new Vector2(0.94f, 0.80f);
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
            vlg.padding = new RectOffset(10, 10, 4, 4);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return cRt;
        }

        GameObject CreateCard(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(0, 52);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.09f, 0.11f, 0.22f, 1f);
            img.raycastTarget = true;
            return go;
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

        Button CreateButton(string name, string label, int fs, Action onClick, Transform parent = null, Vector2 pos = default, float width = 140)
        {
            var p = parent != null ? parent : (currentTab == 0 ? dispatchContent : currentTab == 1 ? missionContent : raidPanel);
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

        /// <summary>카드 오른쪽 버튼 — ⭐ pivot을 오른쪽 모서리로 (중심이면 절반이 카드 밖으로 나가 잘림)</summary>
        Button CreateCardButton(string name, string label, int fs, Action onClick, Transform card)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(card, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);   // ⭐ 오른쪽 모서리 기준
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

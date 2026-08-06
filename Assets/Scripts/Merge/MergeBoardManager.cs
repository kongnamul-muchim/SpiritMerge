using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

namespace SpiritMerge.Merge
{
    /// <summary>
    /// 머지보드 관리자 — 소환/이동/합성
    /// MergeUIRebuilder가 만든 Slot_0~15를 사용합니다.
    /// </summary>
    public class MergeBoardManager : MonoBehaviour
    {
        [Header("설정")]
        public int summonCost = 500;
        public int maxLevel = 6;

        [Header("UI")]
        public TMPro.TextMeshProUGUI countLabel; // "합성 (0/16)" 표시

        // 16개 슬롯의 현재 아이템 (null = 빈 슬롯)
        // ⭐ 선언 시 초기화 — GameManager.Start()가 [ExecutionOrder(-100)]로
        //   MergeBoardManager.Start()보다 먼저 실행되어 TrySummon이 불릴 수 있음
        private GameObject[] slotItems = new GameObject[16];
        private int selectedSlot = -1;

        // ── 파티 편성 ──
        private int[] partySlots = { -1, -1, -1, -1 }; // 보드 슬롯 인덱스, -1 = 빈칸
        private int selectedPartySlot = -1;   // 파티 슬롯 먼저 선택 (편성 모드)
        public System.Action OnPartyChanged;  // 파티 구성 변경 시 호출

        public const int PartyMax = 4;

        void Start()
        {
            // MergeArea 전체 클릭 → 선택 해제 (빈 공간·다른 UI 영역 클릭 시)
            SetupAreaClickHandler();

            // slotItems는 선언부에서 초기화됨 (GameManager가 ExecutionOrder(-100)로
            // Start() 전에 TrySummon을 호출할 수 있어 여기서 새로 만들면 참조를 잃음)
            GameLogger.Info("[MB] MergeBoardManager 시작, 16개 슬롯 초기화");
            for (int i = 0; i < 16; i++)
            {
                var tr = transform.Find($"MergeBoard/Slot_{i}");
                if (tr == null) GameLogger.Warn($"[MB] Slot_{i} 없음!");
                else
                {
                    // 빈 슬롯의 LevelText만 초기화 (기본 텍스트 제거)
                    // ⭐ GameManager(-100)가 Start() 전에 지급한 스타터 정령의 LevelText는 지우면 안 됨!
                    if (slotItems[i] == null)
                    {
                        var lv = tr.Find("LevelText")?.GetComponent<TMPro.TextMeshProUGUI>();
                        if (lv != null) lv.text = "";
                    }

                    // 빈 슬롯 클릭 → 선택 해제 (SpiritItem이 없을 때만 동작)
                    var inner = tr.Find("Inner")?.GetComponent<Image>();
                    if (inner != null)
                    {
                        var btn = inner.gameObject.GetComponent<Button>();
                        if (btn == null) btn = inner.gameObject.AddComponent<Button>();
                        btn.transition = Selectable.Transition.None;
                        int slotIdx = i;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnSlotClicked(slotIdx));
                    }
                }
            }
            // 카운트 레이블 자동 연결 (MergeSectionHeader)
            if (countLabel == null)
            {
                var header = transform.Find("MergeSectionHeader")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (header != null) countLabel = header;
            }
            UpdateCount();

            GameLogger.Info("[MB] 16개 슬롯 준비 완료");
        }

        void Update()
        {
            // 화면 어디든 클릭 시 MergeBoard 요소가 아니면 선택 해제
            if (selectedSlot != -1)
            {
                // Input System: Mouse/터치 통합 처리
                var pointer = Pointer.current;
                if (pointer == null || !pointer.press.wasPressedThisFrame) return;

                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es == null) { DeselectCurrent(); return; }

                var pointerData = new UnityEngine.EventSystems.PointerEventData(es);
                pointerData.position = pointer.position.ReadValue();
                var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                es.RaycastAll(pointerData, results);

                bool isMergeElement = false;
                foreach (var r in results)
                {
                    // SpiritItem / Slot Inner / SummonBtn / MergeBoardManager 영역 / 파티 오버레이 / 파견 배치
                    if (r.gameObject.GetComponentInParent<MergeBoardManager>() != null ||
                        r.gameObject.name == "SummonBtn" ||
                        r.gameObject.GetComponentInParent<PartyFormationUI>() != null ||
                        r.gameObject.GetComponentInParent<DispatchFormationUI>() != null)
                    {
                        isMergeElement = true;
                        break;
                    }
                }

                if (!isMergeElement)
                {
                    DeselectCurrent();
                    GameLogger.Info("[MB] 화면 클릭 → 선택 해제 (MergeBoard 외부)");
                }
            }
        }

        /// <summary>
        /// MergeArea 전체에 투명 클릭 영역 추가 → 아무 곳이나 눌러도 선택 해제
        /// </summary>
        public int TrySummon(SpiritData data)
        {
            GameLogger.Info($"[MB] 소환 시도: {data.name}");
            for (int i = 0; i < 16; i++)
            {
                if (slotItems[i] != null) continue;
                var slot = transform.Find($"MergeBoard/Slot_{i}");
                if (slot == null) continue;

                // ⭐ 소환 시 성급 반영 (1~5성 그대로) — level이 곧 성급, 스프라이트/표시 일치
                int grade = (int)data.grade;
                slotItems[i] = CreateItem(slot, data, grade, i);
                GameLogger.Info($"[MB] 소환 성공: {data.name} {grade}성 → Slot_{i}");
                UpdateCount();
                return i;
            }
            GameLogger.Warn("[MB] 소환 실패: 빈 슬롯 없음");
            return -1;
        }

        /// <summary>
        /// ⭐ 저장 로드용 — 지정 슬롯 + 지정 성급(level)으로 소환 (로드 복원)
        /// </summary>
        public int TrySummonAt(SpiritData data, int slotIdx, int level)
        {
            if (data == null || slotIdx < 0 || slotIdx >= 16) return -1;
            if (slotItems[slotIdx] != null) return -1;
            var slot = transform.Find($"MergeBoard/Slot_{slotIdx}");
            if (slot == null) return -1;

            int grade = Mathf.Max(1, level); // 성급 = level
            slotItems[slotIdx] = CreateItem(slot, data, grade, slotIdx);
            GameLogger.Info($"[MB] 로드 소환: {data.name} Lv.{grade} → Slot_{slotIdx}");
            UpdateCount();
            return slotIdx;
        }

        GameObject CreateItem(Transform slot, SpiritData data, int level, int slotIdx)
        {
            GameObject go = null;
            try
            {
                go = new GameObject("SpiritItem", typeof(RectTransform), typeof(Image), typeof(Button), typeof(SpiritItemData));
                go.transform.SetParent(slot, false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                // SpiritItemData 설정 (⭐ 이게 없으면 합성/색상복귀 둘 다 안 됨)
                var itemData = go.GetComponent<SpiritItemData>();
                itemData.spiritName = data.spiritName;
                itemData.element = data.element;
                itemData.level = level;
                itemData.slotIndex = slotIdx;
                itemData.spiritData = data; // 원본 데이터 참조 (전투 배치용)

                var img = go.GetComponent<Image>();
                // 레벨에 맞는 스프라이트 조회 (Lv.1→1성, Lv.2→2성, ...)
                var lvSprite = GetSpiritSprite(data.element, level);
                img.sprite = lvSprite ?? data.sprite;
                img.color = Color.white;

                // ⭐ 도감 등록 (소환/시작 시)
                GameManager.Instance?.UnlockSpirit(data);

                // 레벨 텍스트 (Slot 내 LevelText 찾기)
                var lvLabel = slot.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
                if (lvLabel != null)
                {
                    lvLabel.text = $"{GetElementKorean(data.element)} Lv.{level}";
                    lvLabel.color = Color.white;
                    lvLabel.fontSize = 14;
                }
                else
                {
                    // LevelText가 없으면 새로 생성
                    var lvGo = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    lvGo.transform.SetParent(go.transform, false);
                    var lr = lvGo.GetComponent<RectTransform>();
                    lr.anchorMin = new Vector2(0, 0);
                    lr.anchorMax = new Vector2(1, 0.3f);
                    lr.offsetMin = Vector2.zero;
                    lr.offsetMax = Vector2.zero;
                    lvLabel = lvGo.GetComponent<TextMeshProUGUI>();
                    lvLabel.text = $"{GetElementKorean(data.element)} Lv.{level}";
                    lvLabel.fontSize = 14;
                    lvLabel.alignment = TextAlignmentOptions.Bottom;
                    lvLabel.color = Color.white;
                    var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
                    if (font != null) lvLabel.font = font;
                }

                // 파티 배지 (P 표시 — 오른쪽 위)
                // ⭐ TMP와 Image를 같은 오브젝트에 두지 않고 분리:
                //   PartyBadge(Image 배경) → BadgeText(TMP) 자식 구조
                CreatePartyBadge(go);

                // 속성 표시 (테두리 색) — ⭐ 스프라이트가 있으면 원본 이미지색(흰색) 유지
                img.color = SpiritItemColor(img, data.element);

                // 최대 레벨 테두리 (Lv.6 → 노란색 Outline)
                UpdateMaxLevelEffect(go, level);

                // 버튼 클릭 (ColorTint OFF — 수동 색상 제어)
                // ⭐ 클릭 시 현재 슬롯 인덱스 사용 (itemData.slotIndex는 이동 시 갱신 — 드래그 이동 후에도 클릭 정상)
                var btn = go.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OnItemClicked(itemData.slotIndex));

                // ⭐ 드래그 앤 드롭 이동 (고스트 아이콘 + 드롭 슬롯 판정)
                var drag = go.AddComponent<SpiritDragHandler>();
                drag.Setup(this, slotIdx, GetComponentInParent<Canvas>());

                return go;
            }
            catch (System.Exception e)
            {
                // 부분 생성된 오브젝트가 남지 않도록 정리 후 실패 반환
                GameLogger.Error($"[MB] CreateItem 예외 (Slot_{slotIdx}, {data.name}): {e}");
                if (go != null) Destroy(go);
                return null;
            }
        }

        /// <summary>
        /// 파티 배지 생성 — Image 배경(PartyBadge) + TMP 텍스트(BadgeText) 분리 구조
        /// </summary>
        void CreatePartyBadge(GameObject parent)
        {
            var badgeBg = new GameObject("PartyBadge", typeof(RectTransform), typeof(Image));
            badgeBg.transform.SetParent(parent.transform, false);
            var badgeRt = badgeBg.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1, 1);
            badgeRt.anchorMax = new Vector2(1, 1);
            badgeRt.pivot = new Vector2(1, 1);
            badgeRt.sizeDelta = new Vector2(18, 18);
            badgeRt.anchoredPosition = new Vector2(-2, -2);
            var bgImg = badgeBg.GetComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.8f);
            bgImg.raycastTarget = false;

            var txtGo = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(badgeBg.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            var badge = txtGo.GetComponent<TextMeshProUGUI>();
            badge.text = "P";
            badge.fontSize = 14;
            badge.fontStyle = FontStyles.Bold;
            badge.alignment = TextAlignmentOptions.Center;
            badge.color = new Color(1, 0.84f, 0);
            badge.raycastTarget = false;
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (font != null) badge.font = font;

            badgeBg.SetActive(false); // UpdatePartyBadges에서 토글
        }

        void OnItemClicked(int idx)
        {
            if (idx < 0 || idx >= 16) return;

            // 🛡️ 편성 모드에서 파티 슬롯이 먼저 선택됨 → 이 정령을 파티에 배치
            if (selectedPartySlot >= 0)
            {
                AssignToParty(selectedPartySlot, idx);
                selectedPartySlot = -1;
                return;
            }

            if (selectedSlot == -1)
            {
                // 첫 선택
                selectedSlot = idx;
                HighlightSlot(idx, true);
                GameLogger.Info($"[MB] 슬롯 선택: Slot_{idx}");
            }
            else if (selectedSlot == idx)
            {
                DeselectCurrent(); // 같은 슬롯 → 선택 해제
            }
            else if (slotItems[idx] == null)
            {
                // 빈 슬롯 → 이동
                GameLogger.Info($"[MB] 이동 시도: Slot_{selectedSlot} → Slot_{idx}");
                MoveItem(selectedSlot, idx);
            }
            else
            {
                // 같은 element + 같은 레벨 → 합성
                var fromData = slotItems[selectedSlot].GetComponent<SpiritItemData>();
                var toData = slotItems[idx].GetComponent<SpiritItemData>();
                bool canMerge = fromData != null && toData != null &&
                    fromData.element == toData.element &&
                    fromData.level == toData.level &&
                    fromData.level < maxLevel;
                if (!canMerge)
                {
                    GameLogger.Info($"[MB] 합성 불가: from={(fromData?.element.ToString()??"null")} Lv.{(fromData?.level??-1)}  to={(toData?.element.ToString()??"null")} Lv.{(toData?.level??-1)}  maxLv={maxLevel}");
                }
                if (canMerge)
                {
                    GameLogger.Info($"[MB] 합성 시도: Slot_{selectedSlot}(Lv.{fromData.level}) + Slot_{idx}(Lv.{toData.level}) → Lv.{fromData.level + 1}");
                    MergeItems(selectedSlot, idx);
                }
                else
                {
                    // 다른 정령 → 선택 전환
                    GameLogger.Info($"[MB] 선택 전환: Slot_{selectedSlot} → Slot_{idx}");
                    DeselectCurrent();
                    selectedSlot = idx;
                    HighlightSlot(idx, true);
                }
            }
        }

        // ── 드래그 앤 드롭 이동 ──

        /// <summary>드래그 시작 — 선택 상태 설정 (고스트는 SpiritDragHandler가 처리)</summary>
        public void OnDragBegin(int fromIdx)
        {
            if (selectedSlot == -1)
            {
                selectedSlot = fromIdx;
                HighlightSlot(fromIdx, true);
            }
            else if (selectedSlot != fromIdx)
            {
                DeselectCurrent();
                selectedSlot = fromIdx;
                HighlightSlot(fromIdx, true);
            }
        }

        /// <summary>드롭 — 포인터 위치의 슬롯으로 이동/합성 (파견/파티 슬롯 드롭 우선)</summary>
        public void OnDragEnd(int fromIdx, Vector2 pointerPos)
        {
            // ⭐ 드래그 → 파견 배치 슬롯 드롭
            var df = FindAnyObjectByType<DispatchFormationUI>();
            if (df != null && df.gameObject.activeSelf && df.TryDropOnSlot(fromIdx, pointerPos))
            {
                GameLogger.Info($"[MB] 파견 슬롯 드롭: Slot_{fromIdx}");
                DeselectCurrent();
                return;
            }
            // ⭐ 드래그 → 파티 편성 슬롯 드롭
            var pf = FindAnyObjectByType<PartyFormationUI>();
            if (pf != null && pf.gameObject.activeSelf && pf.TryDropOnSlot(fromIdx, pointerPos))
            {
                GameLogger.Info($"[MB] 파티 슬롯 드롭: Slot_{fromIdx}");
                DeselectCurrent();
                return;
            }

            int toIdx = FindSlotAt(pointerPos);
            if (toIdx < 0 || toIdx == fromIdx) { DeselectCurrent(); return; }

            // 빈 슬롯 → 이동
            if (slotItems[toIdx] == null)
            {
                GameLogger.Info($"[MB] 드래그 이동: Slot_{fromIdx} → Slot_{toIdx}");
                MoveItem(fromIdx, toIdx);
                return;
            }

            // 정령 있음 → 합성 가능 여부
            var fromData = slotItems[fromIdx]?.GetComponent<SpiritItemData>();
            var toData = slotItems[toIdx].GetComponent<SpiritItemData>();
            bool canMerge = fromData != null && toData != null &&
                fromData.element == toData.element &&
                fromData.level == toData.level &&
                fromData.level < maxLevel;
            if (canMerge)
            {
                GameLogger.Info($"[MB] 드래그 합성: Slot_{fromIdx}(Lv.{fromData.level}) + Slot_{toIdx}(Lv.{toData.level})");
                MergeItems(fromIdx, toIdx);
                return;
            }

            // 합성 불가 → 선택 해제 (원래 자리 유지)
            GameLogger.Info($"[MB] 드래그 합성 불가 — 이동 취소");
            DeselectCurrent();
        }

        /// <summary>포인터 위치가 겹치는 보드 슬롯 인덱스 찾기 (-1 = 없음)</summary>
        int FindSlotAt(Vector2 pointer)
        {
            for (int i = 0; i < 16; i++)
            {
                var slot = transform.Find($"MergeBoard/Slot_{i}");
                if (slot == null) continue;
                var rt = slot.GetComponent<RectTransform>();
                if (rt == null) continue;
                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                // ⭐ 월드→화면 좌표 변환 (CanvasScaler/캔버스 모드와 무관하게 드롭 판정 정확)
                Vector2 sp0 = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
                Vector2 sp2 = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
                Rect rect = Rect.MinMaxRect(sp0.x, sp0.y, sp2.x, sp2.y);
                if (rect.Contains(pointer)) return i;
            }
            return -1;
        }

        /// <summary>
        /// MergeArea 전체에 투명 클릭 영역 추가 → 아무 곳이나 눌러도 선택 해제
        /// </summary>
        void SetupAreaClickHandler()
        {
            // MergeArea에 투명 Image 추가 (raycast용)
            var areaImg = GetComponent<Image>();
            if (areaImg == null)
            {
                areaImg = gameObject.AddComponent<Image>();
                areaImg.color = new Color(0, 0, 0, 0); // 완전 투명
                areaImg.raycastTarget = true;
            }

            // Button 추가 → 클릭 시 선택 해제
            var areaBtn = GetComponent<Button>();
            if (areaBtn == null)
            {
                areaBtn = gameObject.AddComponent<Button>();
                areaBtn.transition = Selectable.Transition.None;
                areaBtn.onClick.AddListener(DeselectCurrent);
            }
        }

        /// <summary>
        /// 현재 선택 해제 (재사용)
        /// </summary>
        public void DeselectCurrent()
        {
            if (selectedSlot == -1) return;
            HighlightSlot(selectedSlot, false);
            GameLogger.Info($"[MB] 선택 해제: Slot_{selectedSlot}");
            selectedSlot = -1;
        }

        /// <summary>
        /// 빈 슬롯 클릭 → 선택 해제 (SpiritItem이 없는 슬롯의 Inner 클릭)
        /// SpiritItem이 있는 슬롯은 OnItemClicked가 우선 처리함
        /// </summary>
        void OnSlotClicked(int idx)        {
            if (idx < 0 || idx >= 16) return;
            if (slotItems[idx] != null) return; // SpiritItem이 있는 슬롯은 OnItemClicked가 처리

            // ⭐ 선택된 정령이 있으면 빈 슬롯 클릭 → 이동 (클릭 이동)
            if (selectedSlot >= 0)
            {
                GameLogger.Info($"[MB] 이동 시도: Slot_{selectedSlot} → Slot_{idx} (빈 슬롯 클릭)");
                MoveItem(selectedSlot, idx);
                return;
            }
            DeselectCurrent();
        }

        void MoveItem(int from, int to)
        {
            if (slotItems[from] == null) { GameLogger.Warn($"[MB] MoveItem 실패: Slot_{from} 비어있음"); return; }
            var spirit = slotItems[from]; // 이동 전 참조 저장
            slotItems[to] = spirit;
            slotItems[from] = null;

            // from 슬롯 LevelText 정리
            var fromSlotTr = transform.Find($"MergeBoard/Slot_{from}");
            if (fromSlotTr != null)
            {
                var fromLv = fromSlotTr.Find("LevelText")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (fromLv != null) fromLv.text = "";
            }

            var slot = transform.Find($"MergeBoard/Slot_{to}");
            if (slot != null) spirit.transform.SetParent(slot, false);
            // 인덱스 갱신
            var data = spirit.GetComponent<SpiritItemData>();
            if (data != null) data.slotIndex = to;

            // ⭐ to 슬롯 LevelText 갱신 (이동 후에도 이름/Lv 표시 — 소환과 동일한 폰트11/흰색)
            if (slot != null && data != null)
            {
                var toLv = slot.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
                if (toLv != null)
                {
                    toLv.text = $"{GetElementKorean(data.element)} Lv.{data.level}";
                    toLv.color = Color.white;   // ⭐ 소환(CreateItem)과 동일하게 흰색
                    toLv.fontSize = 14;         // ⭐ 소환(CreateItem)과 동일하게 11폰트
                }
            }

            // ⭐ 드래그 핸들러 슬롯 인덱스 갱신 (이동한 정령으로 다시 드래그/합성 가능)
            var drag = spirit.GetComponent<SpiritDragHandler>();
            if (drag != null) drag.UpdateSlotIndex(to);

            // 🛡️ 파티 참조 갱신 (이동한 슬롯 반영)
            for (int p = 0; p < PartyMax; p++)
                if (partySlots[p] == from) partySlots[p] = to;

            // 🎨 이동한 정령 색상 복원 (slotItems[from]이 null이므로 HighlightSlot 불가)
            var img = spirit.GetComponent<Image>();
            if (img != null && data != null)
                img.color = SpiritItemColor(img, data.element);

            string name = data?.spiritName ?? "?";
            HighlightSlot(from, false); // slotItems[from]==null → 조용히 실패
            selectedSlot = -1;
            UpdateCount();
            GameLogger.Info($"[MB] 이동 완료: Slot_{from} → Slot_{to} ({name})");
        }

        void MergeItems(int from, int to)
        {
            if (slotItems[from] == null || slotItems[to] == null)
            { GameLogger.Warn($"[MB] MergeItems 실패: from={slotItems[from]!=null} to={slotItems[to]!=null}"); return; }

            var fromData = slotItems[from].GetComponent<SpiritItemData>();
            var toData = slotItems[to].GetComponent<SpiritItemData>();
            if (fromData == null || toData == null)
            { GameLogger.Error($"[MB] MergeItems 실패: SpiritItemData 없음"); return; }

            int newLevel = fromData.level + 1;
            GameLogger.Info($"[MB] ✨ 합성! Slot_{from}(Lv.{fromData.level}) + Slot_{to}(Lv.{toData.level}) → Lv.{newLevel}");

            // 🛡️ 파티에 속한 정령이 합성에 관여하는지 (소멸 전 미리 확인)
            bool affectsParty = GetPartySlotOfBoard(from) >= 0 || GetPartySlotOfBoard(to) >= 0;

            // from 제거
            Destroy(slotItems[from]);
            slotItems[from] = null;

            // 🛡️ 파티에 있던 정령이 합성으로 사라지면 → 파티에서 자동 해제
            for (int p = 0; p < PartyMax; p++)
            {
                if (partySlots[p] == from)
                {
                    partySlots[p] = -1;
                    GameLogger.Info($"[MB] 파티 P{p + 1}의 정령이 합성으로 소멸 → 자동 해제");
                }
            }
            UpdatePartyBadges();

            // from 슬롯 LevelText 정리
            var fromSlotTr = transform.Find($"MergeBoard/Slot_{from}");
            if (fromSlotTr != null)
            {
                var fromLv = fromSlotTr.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
                if (fromLv != null) fromLv.text = "";
            }

            // to 레벨업
            toData.level = newLevel;
            slotItems[to].transform.localScale = Vector3.one * (0.8f + newLevel * 0.1f);

            // 🖼️ 새 레벨에 맞는 스프라이트로 변경
            var newSprite = GetSpiritSprite(toData.element, newLevel);
            if (newSprite != null)
            {
                var mergeImg = slotItems[to].GetComponent<Image>();
                if (mergeImg != null) mergeImg.sprite = newSprite;
            }

            // 🌟 최대 레벨 도달 시 노란 테두리
            UpdateMaxLevelEffect(slotItems[to], newLevel);

            // to 슬롯 LevelText 업데이트
            var toSlotTr = transform.Find($"MergeBoard/Slot_{to}");
            if (toSlotTr != null)
            {
                var toLv = toSlotTr.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
                if (toLv != null)
                    toLv.text = $"{GetElementKorean(toData.element)} Lv.{newLevel}";
            }

            HighlightSlot(from, false);
            selectedSlot = -1;
            UpdateCount();
            GameLogger.Info($"[MB] ✨ 합성 완료! Slot_{to} → Lv.{newLevel}");
            // ⭐ 합성 결과 정령 도감 등록 (등급 상승)
            GameManager.Instance?.UnlockSpiritByGrade(toData.element, newLevel);
            // ⭐ 머지 보너스: 합성 성공 시 추가 골드/경험치 (업그레이드)
            GameManager.Instance?.AddMergeReward();
            // ⭐ 미션 진행도 (머지)
            GameManager.Instance?.OnSpiritMerged();
            // 파티에 영향이 있는 머지만 전투 재시작 트리거 (구성/전투력 변경 반영)
            if (affectsParty) OnPartyChanged?.Invoke();
        }

        // ══════════════════════════════════════════
        // 파티 편성 (BattleArea 오버레이 연동)
        // ══════════════════════════════════════════

        /// <summary>
        /// 편성 모드 전환 (GameManager가 GNB 탭에 따라 호출)
        /// </summary>
        public void SetFormationMode(bool on)
        {
            if (!on)
            {
                selectedPartySlot = -1;
                DeselectCurrent();
            }
        }

        /// <summary>
        /// 파티 슬롯이 먼저 선택됨 (편성 모드) — -1이면 미선택
        /// UI에서 어떤 슬롯이 선택 중인지 표시하기 위한 읽기 전용 프로퍼티
        /// </summary>
        public int SelectedPartySlot => selectedPartySlot;

        /// <summary>
        /// 보드 정령이 선택되어 있는지 (편성 UI 힌트 표시용)
        /// </summary>
        public bool HasSelectedSpirit => selectedSlot >= 0 && slotItems != null && slotItems[selectedSlot] != null;

        /// <summary>현재 선택된 보드 정령 슬롯 인덱스 (파견 배치/파티 편성 연동용)</summary>
        public int SelectedSlotIndex => selectedSlot;

        /// <summary>CLI 검증용 — 보드 정령 선택 시뮬레이션 (실제 클릭과 동일 경로)</summary>
        public void TestSelect(int boardSlotIdx) => OnItemClicked(boardSlotIdx);

        /// <summary>
        /// 파티 슬롯 탭 (오버레이 UI에서 호출) — 양방향 배치 지원
        /// ② 정령 먼저 선택됨 → 이 슬롯에 배치
        /// ① 아니면 슬롯 선택 (토글)
        /// </summary>
        public void OnPartySlotTapped(int partyIdx)
        {
            if (partyIdx < 0 || partyIdx >= PartyMax) return;

            // ② 보드 정령이 먼저 선택됨 → 이 파티 슬롯에 배치
            if (selectedSlot >= 0)
            {
                AssignToParty(partyIdx, selectedSlot);
                DeselectCurrent();
                return;
            }

            // ① 파티 슬롯 먼저 선택 (토글)
            if (selectedPartySlot == partyIdx) selectedPartySlot = -1;
            else selectedPartySlot = partyIdx;
            GameLogger.Info($"[MB] 파티 슬롯 선택: P{partyIdx + 1} ({(selectedPartySlot == partyIdx ? "선택됨" : "해제")})");
        }

        /// <summary>
        /// 정령을 파티 슬롯에 배치 (⭐ 로드 복원에서도 사용 — public)
        /// </summary>
        public void AssignToParty(int partyIdx, int boardSlotIdx)
        {
            if (partyIdx < 0 || partyIdx >= PartyMax) return;
            if (boardSlotIdx < 0 || boardSlotIdx >= 16 || slotItems[boardSlotIdx] == null) return;

            var data = slotItems[boardSlotIdx].GetComponent<SpiritItemData>();
            if (data == null) return;

            // ⭐ 빛/어둠은 함께 편성할 수 없음 (서로 반대 속성 — 시너지 와일드카드 중복 방지)
            if (data.element == ElementType.Light || data.element == ElementType.Dark)
            {
                for (int p = 0; p < PartyMax; p++)
                {
                    if (partySlots[p] < 0 || p == partyIdx) continue;
                    var d = slotItems[partySlots[p]]?.GetComponent<SpiritItemData>();
                    if (d == null) continue;
                    if ((data.element == ElementType.Light && d.element == ElementType.Dark) ||
                        (data.element == ElementType.Dark && d.element == ElementType.Light))
                    {
                        GameLogger.Warn("[MB] 빛과 어둠 정령은 함께 편성할 수 없습니다!");
                        return;
                    }
                }
            }

            // 이미 다른 슬롯에 배치된 정령인지
            int existing = GetPartySlotOfBoard(boardSlotIdx);
            if (existing >= 0 && existing != partyIdx)
            {
                GameLogger.Warn($"[MB] {data.spiritName}은(는) 이미 P{existing + 1}에 배치되어 있음");
                return;
            }

            // 해당 파티 슬롯에 다른 정령이 있으면 교체
            if (partySlots[partyIdx] >= 0 && partySlots[partyIdx] != boardSlotIdx)
            {
                GameLogger.Info($"[MB] P{partyIdx + 1}의 기존 정령 교체");
            }

            partySlots[partyIdx] = boardSlotIdx;
            UpdatePartyBadges();
            UpdateCount();
            GameLogger.Info($"[MB] 🛡️ P{partyIdx + 1}에 {data.spiritName} 배치!");
            OnPartyChanged?.Invoke();
        }

        /// <summary>
        /// 보드 슬롯의 아이템 데이터 반환 (없으면 null)
        /// </summary>
        /// <summary>
        /// 클릭 이동 시뮬레이션 (CLI 검증용): from 선택 → to(빈 슬롯) 클릭 이동
        /// </summary>
        public void TestClickMove(int from, int to)
        {
            OnItemClicked(from);
            OnSlotClicked(to);
        }

        /// <summary>
        /// 클릭 합성 시뮬레이션 (CLI 검증용): from 선택 → to(정령 있는 슬롯) 클릭 → 합성
        /// ⭐ 정령이 있는 슬롯은 OnSlotClicked가 아니라 OnItemClicked가 처리 (합성/선택 전환)
        /// </summary>
        public void TestClickMerge(int from, int to)
        {
            OnItemClicked(from);
            OnItemClicked(to);
        }

        public SpiritItemData GetItemData(int boardSlotIdx)
        {
            if (boardSlotIdx < 0 || boardSlotIdx >= 16 || slotItems == null) return null;
            if (slotItems[boardSlotIdx] == null) return null;
            return slotItems[boardSlotIdx].GetComponent<SpiritItemData>();
        }

        /// <summary>
        /// 첫 빈 파티 슬롯에 정령 자동 배치 (시작 정령용)
        /// </summary>
        public void AutoAssignFirstToParty(int boardSlotIdx)
        {
            for (int p = 0; p < PartyMax; p++)
            {
                if (partySlots[p] < 0)
                {
                    AssignToParty(p, boardSlotIdx);
                    return;
                }
            }
            GameLogger.Warn("[MB] AutoAssignFirstToParty 실패: 파티 슬롯이 모두 차 있음");
        }

        /// <summary>
        /// 파티 슬롯에서 정령 해제
        /// </summary>
        public void RemoveFromParty(int partyIdx)
        {
            if (partyIdx < 0 || partyIdx >= PartyMax) return;
            if (partySlots[partyIdx] < 0) return;

            partySlots[partyIdx] = -1;

            UpdatePartyBadges();
            UpdateCount();
            OnPartyChanged?.Invoke();
            GameLogger.Info($"[MB] 파티 P{partyIdx + 1} 해제");
        }

        /// <summary>
        /// 보드 슬롯에서 정령 제거 (파견 소멸 등) — 파티에 배치돼 있으면 자동 해제
        /// </summary>
        public bool RemoveBoardSpirit(int boardSlotIdx)
        {
            if (boardSlotIdx < 0 || boardSlotIdx >= 16) return false;
            if (slotItems[boardSlotIdx] == null) return false;

            // 파티에 배치되어 있으면 자동 해제
            for (int p = 0; p < PartyMax; p++)
                if (partySlots[p] == boardSlotIdx) partySlots[p] = -1;

            Destroy(slotItems[boardSlotIdx].gameObject);
            slotItems[boardSlotIdx] = null;

            var slotTr = transform.Find($"MergeBoard/Slot_{boardSlotIdx}");
            if (slotTr != null)
            {
                var lv = slotTr.Find("LevelText")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (lv != null) lv.text = "";
            }

            UpdateCount();
            UpdatePartyBadges();
            OnPartyChanged?.Invoke();
            GameLogger.Info($"[MB] 정령 제거: Slot_{boardSlotIdx} (파견 소멸)");
            return true;
        }

        /// <summary>
        /// 보드 슬롯이 몇 번째 파티 슬롯에 있는지 (-1 = 없음)
        /// </summary>
        public int GetPartySlotOfBoard(int boardSlotIdx)
        {
            for (int i = 0; i < PartyMax; i++)
                if (partySlots[i] == boardSlotIdx) return i;
            return -1;
        }

        public bool IsPartyEmpty()
        {
            for (int i = 0; i < PartyMax; i++)
                if (partySlots[i] >= 0) return false;
            return true;
        }

        /// <summary>
        /// 파티 슬롯의 보드 인덱스 배열 (복사본)
        /// </summary>
        public int[] GetPartySlots()
        {
            return (int[])partySlots.Clone();
        }

        /// <summary>
        /// 파티에 편성된 정령들의 SpiritData 목록 (전투 배치용)
        /// </summary>
        public SpiritData[] GetPartySpiritData()
        {
            var list = new System.Collections.Generic.List<SpiritData>();
            for (int i = 0; i < PartyMax; i++)
            {
                int bidx = partySlots[i];
                if (bidx >= 0 && bidx < 16 && slotItems[bidx] != null)
                {
                    var d = slotItems[bidx].GetComponent<SpiritItemData>();
                    if (d != null && d.spiritData != null)
                        list.Add(d.spiritData);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 파티에 편성된 정령들의 SpiritItemData 목록 (레벨 포함 — 전투 배치용)
        /// ⭐ 레벨별 스프라이트 해석에 level이 필요해서 추가
        /// </summary>
        public SpiritItemData[] GetPartyItems()
        {
            var list = new System.Collections.Generic.List<SpiritItemData>();
            for (int i = 0; i < PartyMax; i++)
            {
                int bidx = partySlots[i];
                if (bidx >= 0 && bidx < 16 && slotItems[bidx] != null)
                {
                    var d = slotItems[bidx].GetComponent<SpiritItemData>();
                    if (d != null && d.spiritData != null)
                        list.Add(d);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 편성 오버레이 UI 갱신 (GameManager가 생성한 슬롯들)
        /// </summary>
        public void UpdatePartyUI()
        {
            if (OnPartyChanged != null) OnPartyChanged.Invoke(); // UI 갱신 트리거
        }

        /// <summary>
        /// 보드 아이템의 P 배지 표시 갱신
        /// </summary>
        void UpdatePartyBadges()
        {
            for (int i = 0; i < 16; i++)
            {
                if (slotItems[i] == null) continue;
                var badge = slotItems[i].transform.Find("PartyBadge");
                if (badge != null)
                    badge.gameObject.SetActive(GetPartySlotOfBoard(i) >= 0);
            }
        }

        void HighlightSlot(int idx, bool on)
        {
            if (idx < 0 || idx >= 16 || slotItems[idx] == null) return;
            var img = slotItems[idx].GetComponent<Image>();
            if (img != null) img.color = on ? Color.yellow : SpiritItemColor(img,
                slotItems[idx].GetComponent<SpiritItemData>()?.element ?? ElementType.Fire);
        }

        /// <summary>
        /// ⭐ 정령 이미지 색 — 스프라이트가 있으면 원본색(흰색), 없으면 속성 색 폴백
        /// (과거 속성 색으로 덮어 써서 스프라이트 원본이 안 보였음)
        /// </summary>
        Color SpiritItemColor(Image img, ElementType element)
            => img != null && img.sprite != null ? Color.white : GetElementColor(element);

        Color GetElementColor(ElementType element)
        {
            return element switch
            {
                ElementType.Fire  => new Color(1, 0.4f, 0.2f),
                ElementType.Water => new Color(0.2f, 0.6f, 1),
                ElementType.Wind  => new Color(0.3f, 1, 0.3f),
                ElementType.Earth => new Color(0.6f, 0.4f, 0.2f),
                ElementType.Dark  => new Color(0.5f, 0.2f, 0.7f),
                ElementType.Light => new Color(1, 1, 0.5f),
                _                 => Color.gray
            };
        }

        string GetElementKorean(ElementType element)
        {
            return element switch
            {
                ElementType.Fire  => "불",
                ElementType.Water => "물",
                ElementType.Wind  => "번개",
                ElementType.Earth => "자연",
                ElementType.Dark  => "어둠",
                ElementType.Light => "빛",
                _                 => "?"
            };
        }

        /// <summary>
        /// 속성 + 레벨에 맞는 스프라이트 반환 (SpiritData.ResolveLevelSprite 공용 사용)
        /// Lv.1→1성, Lv.2→2성, ..., Lv.5/6→5성
        /// </summary>
        Sprite GetSpiritSprite(ElementType element, int level)
        {
            return SpiritData.ResolveLevelSprite(element, level);
        }

        /// <summary>
        /// 최대 레벨(Lv.6) 도달 시 노란 테두리(Outline) 표시
        /// </summary>
        void UpdateMaxLevelEffect(GameObject go, int level)
        {
            var outline = go.GetComponent<Outline>();
            if (level >= maxLevel)
            {
                if (outline == null) outline = go.AddComponent<Outline>();
                outline.effectColor = Color.yellow;
                outline.effectDistance = new Vector2(2, 2);
            }
            else
            {
                if (outline != null) Destroy(outline);
            }
        }

        /// <summary>
        /// 현재 보드에 배치된 정령들의 SpiritData 목록 반환 (전투 배치용)
        /// </summary>
        public SpiritData[] GetActiveSpiritData()
        {
            if (slotItems == null) return new SpiritData[0]; // Start() 전에 호출될 경우
            var list = new System.Collections.Generic.List<SpiritData>();
            for (int i = 0; i < 16; i++)
            {
                if (slotItems[i] != null)
                {
                    var data = slotItems[i].GetComponent<SpiritItemData>();
                    if (data != null && data.spiritData != null)
                        list.Add(data.spiritData);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 머지보드 정령 카운트 업데이트 → "합성 (3/16)"
        /// </summary>
        void UpdateCount()
        {
            if (countLabel == null) return;
            int count = 0;
            for (int i = 0; i < 16; i++)
            {
                if (slotItems[i] != null) count++;
            }
            countLabel.text = $"합성 ({count}/16)";
        }
    }

    /// <summary>
    /// 머지 아이템 데이터 (SpiritItem 오브젝트에 붙음)
    /// </summary>
    public class SpiritItemData : MonoBehaviour
    {
        public string spiritName;
        public int level = 1;
        public int slotIndex = -1;
        public ElementType element;
        public SpiritData spiritData; // 원본 SpiritData 참조 (전투 배치용)

        /// <summary>⭐ 성급(=level) 기반 최종 공격력 — 머지 성급 상승 시 전투력 반영</summary>
        public int FinalATK => spiritData != null ? spiritData.FinalATKAt(level) : 0;

        /// <summary>⭐ 성급(=level) 기반 최종 체력 — 머지 성급 상승 시 전투력 반영</summary>
        public int FinalHP => spiritData != null ? spiritData.FinalHPAt(level) : 0;
    }
}

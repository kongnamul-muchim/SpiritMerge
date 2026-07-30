using UnityEngine;
using UnityEngine.UI;
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
        private GameObject[] slotItems;
        private int selectedSlot = -1;

        void Start()
        {
            // MergeArea 전체 클릭 → 선택 해제 (빈 공간·다른 UI 영역 클릭 시)
            SetupAreaClickHandler();

            slotItems = new GameObject[16];
            GameLogger.Info("[MB] MergeBoardManager 시작, 16개 슬롯 초기화");
            for (int i = 0; i < 16; i++)
            {
                var tr = transform.Find($"MergeBoard/Slot_{i}");
                if (tr == null) GameLogger.Warn($"[MB] Slot_{i} 없음!");
                else
                {
                    // 빈 슬롯의 LevelText 초기화 (기본 텍스트 제거)
                    var lv = tr.Find("LevelText")?.GetComponent<TMPro.TextMeshProUGUI>();
                    if (lv != null) lv.text = "";

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
            if (selectedSlot != -1 && Input.GetMouseButtonDown(0))
            {
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es == null) { DeselectCurrent(); return; }

                var pointerData = new UnityEngine.EventSystems.PointerEventData(es);
                pointerData.position = Input.mousePosition;
                var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                es.RaycastAll(pointerData, results);

                bool isMergeElement = false;
                foreach (var r in results)
                {
                    // SpiritItem / Slot Inner / SummonBtn / MergeBoardManager 영역
                    if (r.gameObject.GetComponentInParent<MergeBoardManager>() != null ||
                        r.gameObject.name == "SummonBtn")
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
        public bool TrySummon(SpiritData data)
        {
            GameLogger.Info($"[MB] 소환 시도: {data.name}");
            for (int i = 0; i < 16; i++)
            {
                if (slotItems[i] != null) continue;
                var slot = transform.Find($"MergeBoard/Slot_{i}");
                if (slot == null) continue;

                slotItems[i] = CreateItem(slot, data, 1, i);
                GameLogger.Info($"[MB] 소환 성공: {data.name} Lv.1 → Slot_{i}");
                UpdateCount();
                return true;
            }
            GameLogger.Warn("[MB] 소환 실패: 빈 슬롯 없음");
            return false;
        }

        GameObject CreateItem(Transform slot, SpiritData data, int level, int slotIdx)
        {
            var go = new GameObject("SpiritItem", typeof(RectTransform), typeof(Image), typeof(Button), typeof(SpiritItemData));
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
            if (data.sprite != null) img.sprite = data.sprite;
            img.color = Color.white;

            // 레벨 텍스트 (Slot 내 LevelText 찾기)
            var lvLabel = slot.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            if (lvLabel != null)
            {
                lvLabel.text = $"{GetElementKorean(data.element)} Lv.{level}";
                lvLabel.color = Color.white;
                lvLabel.fontSize = 11;
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
                lvLabel.fontSize = 11;
                lvLabel.alignment = TextAlignmentOptions.Bottom;
                lvLabel.color = Color.white;
                var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
                if (font != null) lvLabel.font = font;
            }

            // 속성 표시 (테두리 색)
            img.color = GetElementColor(data.element);

            // 버튼 클릭 (ColorTint OFF — 수동 색상 제어)
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            int captureIdx = slotIdx;
            btn.onClick.AddListener(() => OnItemClicked(captureIdx));

            return go;
        }

        void OnItemClicked(int idx)
        {
            if (idx < 0 || idx >= 16) return;

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
        void OnSlotClicked(int idx)
        {
            if (idx < 0 || idx >= 16) return;
            if (slotItems[idx] != null) return; // SpiritItem이 있는 슬롯은 OnItemClicked가 처리
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

            // 🎨 이동한 정령 색상 복원 (slotItems[from]이 null이므로 HighlightSlot 불가)
            var img = spirit.GetComponent<Image>();
            if (img != null && data != null)
                img.color = GetElementColor(data.element);

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

            // from 제거
            Destroy(slotItems[from]);
            slotItems[from] = null;

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
        }

        void HighlightSlot(int idx, bool on)
        {
            if (idx < 0 || idx >= 16 || slotItems[idx] == null) return;
            var img = slotItems[idx].GetComponent<Image>();
            if (img != null) img.color = on ? Color.yellow : GetElementColor(
                slotItems[idx].GetComponent<SpiritItemData>()?.element ?? ElementType.Fire);
        }

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
        /// 현재 보드에 배치된 정령들의 SpiritData 목록 반환 (전투 배치용)
        /// </summary>
        public SpiritData[] GetActiveSpiritData()
        {
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
    }
}

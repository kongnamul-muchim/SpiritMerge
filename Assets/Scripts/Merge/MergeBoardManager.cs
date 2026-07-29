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

        // 16개 슬롯의 현재 아이템 (null = 빈 슬롯)
        private GameObject[] slotItems;
        private int selectedSlot = -1;

        void Start()
        {
            slotItems = new GameObject[16];
            GameLogger.Info("[MB] MergeBoardManager 시작, 16개 슬롯 초기화");
            for (int i = 0; i < 16; i++)
            {
                var tr = transform.Find($"MergeBoard/Slot_{i}");
                if (tr == null) GameLogger.Warn($"[MB] Slot_{i} 없음!");
            }
            GameLogger.Info("[MB] 16개 슬롯 준비 완료");
        }

        /// <summary>
        /// 소환: GameManager가 호출
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
                return true;
            }
            GameLogger.Warn("[MB] 소환 실패: 빈 슬롯 없음");
            return false;
        }

        GameObject CreateItem(Transform slot, SpiritData data, int level, int slotIdx)
        {
            var go = new GameObject("SpiritItem", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(slot, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

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
                // 같은 슬롯 → 선택 해제
                HighlightSlot(idx, false);
                GameLogger.Info($"[MB] 슬롯 선택 해제: Slot_{idx}");
                selectedSlot = -1;
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
                    HighlightSlot(selectedSlot, false);
                    selectedSlot = idx;
                    HighlightSlot(idx, true);
                }
            }
        }

        void MoveItem(int from, int to)
        {
            if (slotItems[from] == null) { GameLogger.Warn($"[MB] MoveItem 실패: Slot_{from} 비어있음"); return; }
            var spirit = slotItems[from]; // 이동 전 참조 저장
            slotItems[to] = spirit;
            slotItems[from] = null;

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

            // to 레벨업
            toData.level = newLevel;
            slotItems[to].transform.localScale = Vector3.one * (0.8f + newLevel * 0.1f);

            // LevelText 업데이트 (Slot의 자식)
            var slotTr = transform.Find($"MergeBoard/Slot_{to}");
            if (slotTr != null)
            {
                var lvLabel = slotTr.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
                if (lvLabel != null)
                    lvLabel.text = $"{GetElementKorean(toData.element)} Lv.{newLevel}";
            }

            HighlightSlot(from, false);
            selectedSlot = -1;
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
    }
}

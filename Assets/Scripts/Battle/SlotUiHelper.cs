using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Battle
{
    /// <summary>
    /// 전투 슬롯(EnemySlot/SpiritSlot)의 공용 UI 요소를 찾는 헬퍼
    /// - displayImage : 슬롯 자체 Image (스프라이트 표시)
    /// - hpSlider     : HPBar의 Slider (value로 HP 비율 조절)
    /// - cdSlider     : CDBar의 Slider (value로 공격 쿨타임 게이지 조절)
    /// - lvText       : LvText (EnemySlot에만 존재, 없으면 null)
    ///
    /// ⭐ v3: HP/CD바가 Image.Filled → Slider로 변경됨 (fillAmount 대신 value 사용)
    /// </summary>
    public static class SlotUiHelper
    {
        public static Image FindDisplayImage(Transform slot)
        {
            return slot != null ? slot.GetComponent<Image>() : null;
        }

        public static Slider FindHpSlider(Transform slot)
        {
            if (slot == null) return null;
            var hpBar = slot.Find("HPBar");
            if (hpBar == null) return null;
            return hpBar.GetComponent<Slider>();
        }

        public static Slider FindCdSlider(Transform slot)
        {
            if (slot == null) return null;
            var cdBar = slot.Find("CDBar");
            if (cdBar == null) return null;
            return cdBar.GetComponent<Slider>();
        }

        public static TextMeshProUGUI FindLvText(Transform slot)
        {
            return slot != null ? slot.Find("LvText")?.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}

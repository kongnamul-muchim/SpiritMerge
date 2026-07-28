using UnityEngine;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 미션 UI — 일일 + 반복 미션
    /// </summary>
    public class MissionUI : MonoBehaviour
    {
        [SerializeField] private Transform dailyMissionRoot;
        [SerializeField] private Transform repeatMissionRoot;
        [SerializeField] private GameObject missionItemPrefab;
    }
}

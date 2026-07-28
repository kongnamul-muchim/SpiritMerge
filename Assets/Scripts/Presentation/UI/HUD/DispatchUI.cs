using UnityEngine;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 의뢰(파견) UI — 조건 매칭 시스템
    /// </summary>
    public class DispatchUI : MonoBehaviour
    {
        [SerializeField] private Transform requestListRoot;
        [SerializeField] private Transform slotPanel;
        [SerializeField] private GameObject requestItemPrefab;
    }
}

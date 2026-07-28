using SpiritMerge.Core.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 의뢰 UI — 조건 매칭 파견 (GDD §13)
    /// </summary>
    public class DispatchUI : MonoBehaviour
    {
        [SerializeField] private Transform requestRoot;
        [SerializeField] private GameObject requestPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private TMP_Text infoText;

        private DispatchService _dispatch = new();
        private DispatchRequest _current;

        private void Start()
        {
            NewRequest();
            refreshButton?.onClick.AddListener(NewRequest);
        }

        public void NewRequest()
        {
            _current = _dispatch.GenerateRequest();
            Refresh();
        }

        private void Refresh()
        {
            foreach (Transform c in requestRoot) Destroy(c.gameObject);
            if (_current == null) return;

            var go = Instantiate(requestPrefab, requestRoot);
            var conditions = "";
            foreach (var slot in _current.slots)
                conditions += $"[{slot.requiredElement} {slot.minGrade}★] ";
            go.GetComponentInChildren<TMP_Text>().text =
                $"보상: 골드 {_current.goldReward} / 루비 {_current.rubyReward}\n조건: {conditions}";
            infoText.text = $"파견 시간: {_current.durationHours}시간";
        }
    }
}

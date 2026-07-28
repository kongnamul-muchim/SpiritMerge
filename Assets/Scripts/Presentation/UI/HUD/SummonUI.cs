using System.Collections.Generic;
using SpiritMerge.Core.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SpiritMerge.Presentation.UI.HUD
{
    /// <summary>
    /// 소환(뽑기) 시스템 UI (GDD §7)
    /// </summary>
    public class SummonUI : MonoBehaviour
    {
        [Header("버튼")]
        [SerializeField] private Button normalSummonBtn;     // 골드 500
        [SerializeField] private Button normalSummon10Btn;   // 10연차
        [SerializeField] private Button premiumSummonBtn;    // 루비 200
        [SerializeField] private Button premiumSummon10Btn;
        [SerializeField] private Button pickupSummonBtn;     // 정령석 150

        [Header("결과")]
        [SerializeField] private Transform resultGrid;
        [SerializeField] private GameObject resultSlotPrefab;
        [SerializeField] private TMP_Text resultInfo;

        [Inject] private ISpiritService _spiritService;
        [Inject] private ICurrencyService _currency;

        private void Start()
        {
            normalSummonBtn?.onClick.AddListener(() => Summon(SummonType.Normal, 1));
            normalSummon10Btn?.onClick.AddListener(() => Summon(SummonType.Normal, 10));
            premiumSummonBtn?.onClick.AddListener(() => Summon(SummonType.Premium, 1));
            premiumSummon10Btn?.onClick.AddListener(() => Summon(SummonType.Premium, 10));
            pickupSummonBtn?.onClick.AddListener(() => Summon(SummonType.Pickup, 1));
        }

        public void Summon(SummonType type, int count)
        {
            // 비용 확인
            if (type == SummonType.Normal && !_currency.SpendGold(500 * count)) return;
            if (type == SummonType.Premium && !_currency.SpendRuby(200 * count)) return;
            if (type == SummonType.Pickup && !_currency.SpendSpiritStone(150 * count)) return;

            // 기존 결과 제거
            foreach (Transform child in resultGrid) Destroy(child.gameObject);

            // 소환 실행
            var results = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var spirit = DoSummon(type);
                if (spirit != null)
                {
                    var go = Instantiate(resultSlotPrefab, resultGrid);
                    go.GetComponentInChildren<TMP_Text>().text = $"{spirit.dataId} {spirit.grade}★";
                    results.Add($"{spirit.dataId}({spirit.grade}★)");
                }
            }

            resultInfo.text = $"결과: {string.Join(", ", results)}";
        }

        private OwnedSpirit DoSummon(SummonType type)
        {
            float roll = Random.value;
            int grade;
            string element;

            switch (type)
            {
                case SummonType.Normal:
                    grade = roll < 0.8f ? 1 : 2;
                    break;
                case SummonType.Premium:
                    grade = roll < 0.6f ? 2 : (roll < 0.9f ? 3 : 4);
                    break;
                case SummonType.Pickup:
                    grade = roll < 0.6f ? 3 : (roll < 0.9f ? 4 : 5);
                    break;
                default:
                    grade = 1;
                    break;
            }

            string[] elements = { "Fire", "Water", "Wind", "Earth", "Dark", "Light" };
            element = elements[Random.Range(0, elements.Length)];
            string dataId = $"{element}_{grade}";

            return _spiritService.AddSpirit(dataId, grade);
        }
    }

    public enum SummonType { Normal, Premium, Pickup }
}

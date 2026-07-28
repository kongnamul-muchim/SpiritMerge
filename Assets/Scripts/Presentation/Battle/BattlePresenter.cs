using System.Collections.Generic;
using SpiritMerge;
using SpiritMerge.Core.Interfaces;
using SpiritMerge.Core.Systems;
using UnityEngine;
using VContainer;

namespace SpiritMerge.Presentation.Battle
{
    /// <summary>
    /// 전투 Presenter — BattleService + SpiritUnit + EnemyUnit + UI 연결
    /// SRP: 전투 중재자 역할 (Service와 View 사이)
    /// </summary>
    public class BattlePresenter : MonoBehaviour
    {
        [Header("스폰 위치")]
        [SerializeField] private Transform spiritSpawnRoot;
        [SerializeField] private Transform enemySpawnRoot;
        [SerializeField] private GameObject spiritUnitPrefab;
        [SerializeField] private GameObject enemyUnitPrefab;

        [Header("전투 UI")]
        [SerializeField] private UnityEngine.UI.Slider progressBar;
        [SerializeField] private UnityEngine.UI.Button autoToggleButton;

        [Inject] private IBattleService _battle;
        [Inject] private IPlayerService _player;

        private readonly List<SpiritUnit> _activeSpirits = new();
        private readonly List<EnemyUnit> _activeEnemies = new();
        private bool _isAutoMode = true;

        private void Start()
        {
            Debug.Log("[BattlePresenter] Ready");
        }

        /// <summary>
        /// 전투 시작 (외부 호출)
        /// </summary>
        public void BeginBattle(int region, int stage)
        {
            _battle.StartBattle(region, stage);

            // 정령 소환
            SpawnPartySpirits();

            // 첫 웨이브 시작
            SpawnWave();
        }

        private void SpawnPartySpirits()
        {
            // 기존 정령 제거
            foreach (var s in _activeSpirits) Destroy(s.gameObject);
            _activeSpirits.Clear();

            // 4마리 정령 생성 (파티 데이터 대신 임시)
            for (int i = 0; i < 4; i++)
            {
                var go = Instantiate(spiritUnitPrefab, spiritSpawnRoot);
                go.name = $"Spirit_{i}";
                var unit = go.GetComponent<SpiritUnit>();
                _activeSpirits.Add(unit);
            }

            Debug.Log($"[Battle] Spawned {_activeSpirits.Count} spirits");
        }

        private void SpawnWave()
        {
            // 기존 적 제거
            foreach (var e in _activeEnemies) Destroy(e.gameObject);
            _activeEnemies.Clear();

            var spawns = _battle.GetCurrentWaveSpawns();
            foreach (var info in spawns)
            {
                var go = Instantiate(enemyUnitPrefab, enemySpawnRoot);
                go.name = $"Enemy_{info.element}_{(_activeEnemies.Count)}";
                var unit = go.GetComponent<EnemyUnit>();
                unit.Initialize(info, 15, 80, 5);
                _activeEnemies.Add(unit);
            }

            Debug.Log($"[Battle] Wave {_battle.CurrentWave + 1}: {_activeEnemies.Count} enemies");
            UpdateUI();
        }

        /// <summary>
        /// 적이 죽었을 때 호출
        /// </summary>
        public void OnEnemyKilled(EnemyUnit enemy)
        {
            _activeEnemies.Remove(enemy);
            Destroy(enemy.gameObject);

            if (_activeEnemies.Count == 0)
            {
                // 웨이브 클리어
                _battle.NextWave();

                if (_battle.State == BattleState.Victory)
                {
                    Debug.Log("[Battle] 🏆 All waves cleared!");
                }
                else
                {
                    SpawnWave();
                }
            }

            UpdateUI();
        }

        /// <summary>
        /// 정령이 죽었을 때 호출
        /// </summary>
        public void OnSpiritKilled(SpiritUnit spirit)
        {
            _activeSpirits.Remove(spirit);
            Destroy(spirit.gameObject);

            if (_activeSpirits.Count == 0)
            {
                _battle.OnSpiritDefeated();
                Debug.Log("[Battle] 💀 All spirits defeated!");
            }

            UpdateUI();
        }

        /// <summary>
        /// 자동/수동 전환
        /// </summary>
        public void ToggleAutoMode()
        {
            _isAutoMode = !_isAutoMode;
        }

        private void UpdateUI()
        {
            if (progressBar != null)
                progressBar.value = _battle.GetBattleProgress();
        }

        private void Update()
        {
            if (_battle.State != BattleState.Battling) return;

            // 실시간 전투 로직 (자동 공격)
            if (_isAutoMode)
            {
                foreach (var spirit in _activeSpirits)
                {
                    if (!spirit.IsAlive || _activeEnemies.Count == 0) continue;

                    var target = _activeEnemies[0];
                    if (target == null || !target.IsAlive) continue;

                    int dmg = BattleService.CalcDamageToEnemy(
                        spirit.GetComponent<SpiritUnit>().GetHashCode() % 50 + 10,
                        1.0f, spirit.Element, target.Element, target.DEF, 0.05f, 1.5f);

                    target.TakeDamage(dmg);

                    if (!target.IsAlive)
                        OnEnemyKilled(target);
                }
            }

            UpdateUI();
        }
    }
}

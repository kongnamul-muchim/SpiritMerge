using UnityEditor;
using UnityEngine;
using SpiritMerge.Battle;
using SpiritMerge.Merge;

namespace SpiritMerge.Editor
{
    public static class ApplySceneComponents
    {
        [MenuItem("SpiritMerge/Setup/Apply All Components")]
        public static void Apply()
        {
            int added = 0;

            var canvas = GameObject.Find("MainCanvas");
            if (canvas == null) { EditorUtility.DisplayDialog("Error", "MainCanvas를 찾을 수 없습니다!", "OK"); return; }
            Debug.Log($"[Apply] MainCanvas 찾음: {canvas.name}");

            // ── GameManager ──
            if (canvas.GetComponent<GameManager>() == null)
            {
                canvas.AddComponent<GameManager>();
                Undo.RegisterCreatedObjectUndo(canvas.GetComponent<GameManager>(), "GM");
                added++;
                Debug.Log("[Apply] ✅ GameManager 추가");
            }

            // ── MergeBoardManager ──
            var mergeArea = GameObject.Find("MergeArea");
            if (mergeArea != null && mergeArea.GetComponent<MergeBoardManager>() == null)
            {
                mergeArea.AddComponent<MergeBoardManager>();
                Undo.RegisterCreatedObjectUndo(mergeArea.GetComponent<MergeBoardManager>(), "MBM");
                added++;
                Debug.Log("[Apply] ✅ MergeBoardManager 추가");
            }

            // ── Battle 시스템 ──
            var battleArea = GameObject.Find("BattleArea");
            if (battleArea != null)
            {
                var bm = battleArea.GetComponent<BattleManager>();
                if (bm == null)
                {
                    bm = battleArea.AddComponent<BattleManager>();
                    Undo.RegisterCreatedObjectUndo(bm, "BM");
                    added++;
                    Debug.Log("[Apply] ✅ BattleManager 추가");
                }

                if (bm.spiritSpawnRoot == null)
                {
                    var ssr = new GameObject("SpiritSpawnRoot");
                    ssr.transform.SetParent(battleArea.transform, false);
                    bm.spiritSpawnRoot = ssr.transform;
                    EditorUtility.SetDirty(bm);
                }
                if (bm.enemySpawnRoot == null)
                {
                    var esr = new GameObject("EnemySpawnRoot");
                    esr.transform.SetParent(battleArea.transform, false);
                    bm.enemySpawnRoot = esr.transform;
                    EditorUtility.SetDirty(bm);
                }

                if (battleArea.GetComponent<MonsterSpawner>() == null)
                {
                    battleArea.AddComponent<MonsterSpawner>();
                    Undo.RegisterCreatedObjectUndo(battleArea.GetComponent<MonsterSpawner>(), "MS");
                    added++;
                    Debug.Log("[Apply] ✅ MonsterSpawner 추가");
                }

                if (battleArea.GetComponent<WaveController>() == null)
                {
                    battleArea.AddComponent<WaveController>();
                    Undo.RegisterCreatedObjectUndo(battleArea.GetComponent<WaveController>(), "WC");
                    added++;
                    Debug.Log("[Apply] ✅ WaveController 추가");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Apply] ✅ {added}개 추가 완료!");
            EditorUtility.DisplayDialog("Apply All Components",
                $"✅ {added}개 컴포넌트 추가 완료!\n\n" +
                "MainCanvas → GameManager\n" +
                "MergeArea → MergeBoardManager\n" +
                "BattleArea → BattleManager, MonsterSpawner, WaveController\n\n" +
                "Save Scene → Play", "OK");
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// MainCanvas에 GameManager 추가
    /// 실행: SpiritMerge > Setup > Add GameManager
    /// </summary>
    public static class AddGameManagerToScene
    {
        [MenuItem("SpiritMerge/Setup/Add GameManager")]
        public static void Add()
        {
            var mainCanvas = GameObject.Find("MainCanvas");
            if (mainCanvas == null)
            {
                Debug.LogError("MainCanvas 없음!");
                return;
            }

            var gm = mainCanvas.GetComponent<GameManager>();
            if (gm != null)
            {
                Debug.Log("[AddGM] 이미 GameManager 있음");
                return;
            }

            gm = mainCanvas.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gm, "Add GameManager");
            Debug.Log("[AddGM] ✅ GameManager 추가 완료!");
            EditorUtility.DisplayDialog("GameManager", "✅ GameManager 추가 완료!\nPlay를 누르면 게임이 시작됩니다.", "OK");
        }
    }
}

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// 씬에 EventSystem + InputSystemUIInputModule 추가
    /// 실행: SpiritMerge > Setup > Add EventSystem
    /// </summary>
    public static class AddEventSystemToScene
    {
        [MenuItem("SpiritMerge/Setup/Add EventSystem")]
        public static void Add()
        {
            var existing = Object.FindObjectOfType<EventSystem>();
            if (existing != null)
            {
                Debug.Log("[EventSystem] 이미 존재함: " + existing.name);
                EditorUtility.DisplayDialog("EventSystem", "이미 EventSystem이 씬에 있습니다.", "OK");
                return;
            }

            var es = new GameObject("EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Add EventSystem");
            Debug.Log("[EventSystem] ✅ EventSystem + InputSystemUIInputModule 추가 완료!");
            EditorUtility.DisplayDialog("EventSystem", "✅ EventSystem 추가 완료!\n이제 버튼 클릭이 동작합니다.", "OK");
        }
    }
}

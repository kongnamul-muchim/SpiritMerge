using UnityEditor;
using UnityEngine;
using TMPro;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// NotoSansKR-VariableFont_wght SDF를 주 폰트로 설정
    /// (한글 + 영어 + 숫자 모두 지원하므로 별도 fallback 불필요)
    /// 실행: SpiritMerge > Setup > Register Korean Font Fallback
    /// </summary>
    public static class KoreanFontFallback
    {
        [MenuItem("SpiritMerge/Setup/Register Korean Font Fallback")]
        public static void Register()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            if (font == null)
            {
                Debug.LogError("[폰트] NotoSansKR-VariableFont_wght SDF 없음!");
                return;
            }

            Debug.Log($"[폰트] ✅ NotoSansKR-VariableFont_wght SDF 준비 완료!");
            EditorUtility.DisplayDialog("Korean Font",
                "NotoSansKR-VariableFont_wght SDF가\\n정상 로드되었습니다.\\n\\n이 폰트는 한글/영어/숫자를\\n모두 지원합니다.", "OK");
        }
    }
}

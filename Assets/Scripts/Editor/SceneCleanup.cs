using UnityEditor;
using UnityEngine;
using TMPro;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// Scene 클린업 + BattleArea/GNB 리사이징
    /// 실행: SpiritMerge > Setup > Cleanup Scene
    /// </summary>
    public static class SceneCleanup
    {
        [MenuItem("SpiritMerge/Setup/Cleanup Scene")]
        public static void Cleanup()
        {
            Undo.SetCurrentGroupName("Scene Cleanup");
            int group = Undo.GetCurrentGroup();

            int removed = 0;

            // ── 0. 전체화면 배경 추가 (MainCanvas 첫 번째 자식) ──
            var mainCanvas = GameObject.Find("MainCanvas");
            if (mainCanvas != null)
            {
                var bg = GameObject.Find("ScreenBackground");
                if (bg == null)
                {
                    bg = new GameObject("ScreenBackground", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    Undo.RegisterCreatedObjectUndo(bg, "ScreenBackground");
                    bg.transform.SetParent(mainCanvas.transform, false);
                    bg.transform.SetAsFirstSibling(); // 제일 뒤로
                    var br = bg.GetComponent<RectTransform>();
                    br.anchorMin = Vector2.zero;
                    br.anchorMax = Vector2.one;
                    br.offsetMin = Vector2.zero;
                    br.offsetMax = Vector2.zero;
                    var bimg = bg.GetComponent<UnityEngine.UI.Image>();
                    bimg.color = new Color(0.05f, 0.06f, 0.12f); // #0C0F1F
                    bimg.raycastTarget = false;
                    Debug.Log("[Cleanup] ✅ ScreenBackground 추가 (전체화면 #0C0F1F)");
                }
            }

            // ── 1. 구형 MergeArea 요소 제거 ──
            string[] oldMergeItems = {
                "SpiritListRoot", "MergeBoardRoot", "MergeBtn",
                "CrossMergeBtn", "SortBtn", "InfoText",
                "MergeResultPopup", "MergeSummonBtn"
            };
            foreach (var name in oldMergeItems)
            {
                var obj = GameObject.Find(name);
                if (obj != null)
                {
                    Undo.DestroyObjectImmediate(obj);
                    removed++;
                    Debug.Log($"[Cleanup] 제거: {name}");
                }
            }

            // ── 2. GBN의 7개 탭 제거 ──
            var gnb = GameObject.Find("BottomMenu");
            if (gnb != null)
            {
                var tabs = gnb.GetComponentsInChildren<RectTransform>(true);
                foreach (var tab in tabs)
                {
                    if (tab != null && tab.gameObject != gnb && tab.name.StartsWith("Tab_"))
                    {
                        Undo.DestroyObjectImmediate(tab.gameObject);
                        removed++;
                    }
                }
            }

            // ── 2.5 구형 GoldText/RubyText 제거 (anchorMin==anchorMax) ──
            string[] oldTopBarItems = { "GoldText", "RubyText" };
            foreach (var name in oldTopBarItems)
            {
                var obj = GameObject.Find(name);
                if (obj != null)
                {
                    // anchorMin == anchorMax면 구형 (크기 0)
                    var rt = obj.GetComponent<RectTransform>();
                    if (rt != null && rt.anchorMin == rt.anchorMax)
                    {
                        Undo.DestroyObjectImmediate(obj);
                        removed++;
                        Debug.Log($"[Cleanup] 제거: {name} (anchorMin==anchorMax)");
                    }
                }
            }

            // ── 3. 중복 MainCanvas 제거 (진짜 하나만 남기고 모두 제거) ──
            var allCanvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            GameObject realCanvas = null;
            int canvasCount = 0;

            // 진짜 MainCanvas 찾기 (TopBar 또는 BattleArea를 자식으로 가진 것)
            foreach (var cv in allCanvases)
            {
                if (cv.name != "MainCanvas") continue;
                canvasCount++;
                if (cv.transform.Find("TopBar") != null || cv.transform.Find("BattleArea") != null)
                {
                    realCanvas = cv.gameObject;
                }
            }

            // 진짜 외에는 모두 제거
            foreach (var cv in allCanvases)
            {
                if (cv.name != "MainCanvas") continue;
                if (realCanvas != null && cv.gameObject != realCanvas)
                {
                    Undo.DestroyObjectImmediate(cv.gameObject);
                    removed++;
                    Debug.Log("[Cleanup] 제거: 중복 MainCanvas");
                }
            }

            if (canvasCount > 1)
            {
                string statusMsg = realCanvas != null ? "1개 유지" : "모두 제거?";
                Debug.Log($"[Cleanup] ⚠️ MainCanvas {canvasCount}개 → {statusMsg}");
            }

            // MainCanvas가 아예 없으면 경고
            var finalCanvases = GameObject.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            int finalCount = 0;
            foreach (var cv in finalCanvases)
                if (cv.name == "MainCanvas") finalCount++;
            if (finalCount == 0)
                Debug.LogError("[Cleanup] ❌ MainCanvas가 없습니다! SceneSetup을 실행하세요.");
            else if (finalCount == 1)
                Debug.Log($"[Cleanup] ✅ MainCanvas 1개 — 정상");

            // ── 4. TopBar 리사이징 ──
            var topBar = GameObject.Find("TopBar");
            if (topBar != null)
            {
                var tr = topBar.GetComponent<RectTransform>();
                Undo.RecordObject(tr, "Resize TopBar");
                tr.anchorMin = new Vector2(0, 0.93f);
                tr.anchorMax = new Vector2(1, 1);
                tr.offsetMin = Vector2.zero;
                tr.offsetMax = Vector2.zero;
                Debug.Log("[Cleanup] TopBar → top 7%");
            }

            // ── 4. BattleArea 재조정 (TopBar 아래) ──
            var battleArea = GameObject.Find("BattleArea");
            if (battleArea != null)
            {
                var br = battleArea.GetComponent<RectTransform>();
                Undo.RecordObject(br, "Resize BattleArea");
                br.anchorMin = new Vector2(0, 0.55f);
                br.anchorMax = new Vector2(1, 0.92f);
                br.offsetMin = new Vector2(0, 0);
                br.offsetMax = new Vector2(0, 0);
                Debug.Log("[Cleanup] BattleArea → 55%~92% (37%)");
            }

            // ── 5. MergeArea 재조정 (BattleArea 아래 + GNB 위) ──
            var mergeArea = GameObject.Find("MergeArea");
            if (mergeArea != null)
            {
                var mr = mergeArea.GetComponent<RectTransform>();
                Undo.RecordObject(mr, "Resize MergeArea");
                mr.anchorMin = new Vector2(0, 0);
                mr.anchorMax = new Vector2(1, 0.54f);
                mr.offsetMin = new Vector2(0, 60);
                mr.offsetMax = new Vector2(0, 0);
                mr.pivot = new Vector2(0.5f, 0);
                Debug.Log("[Cleanup] MergeArea → bottom 54% + GNB 60px");
            }

            // ── 5. 폰트 Fallback: NotoSansKR에 LiberationSans를 Fallback 등록 ──
            var notoFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");
            var libFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (notoFont != null && libFont != null)
            {
                if (notoFont.fallbackFontAssetTable == null)
                    notoFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                if (!notoFont.fallbackFontAssetTable.Contains(libFont))
                {
                    notoFont.fallbackFontAssetTable.Add(libFont);
                    EditorUtility.SetDirty(notoFont);
                    Debug.Log("[Cleanup] ✅ NotoSansKR → LiberationSans Fallback 등록 (영문/숫자 지원)");
                }
            }

            Undo.CollapseUndoOperations(group);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log($"[Cleanup] ✅ {removed}개 불필요 오브젝트 제거 완료!");
            EditorUtility.DisplayDialog("Scene Cleanup",
                $"✅ {removed}개 불필요 오브젝트 제거\n" +
                "BattleArea → top 45%\n" +
                "MergeArea → bottom 55%\n\n" +
                "이제 SpiritMerge > UI > Rebuild GNB도 실행하세요!", "OK");
        }
    }
}

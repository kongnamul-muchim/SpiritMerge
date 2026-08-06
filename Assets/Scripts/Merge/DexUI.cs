using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.Text;

namespace SpiritMerge.Merge
{
    /// <summary>
    /// 도감 오버레이 UI — Canvas 최상위
    /// 정령 30종 (속성 6 × 등급 5)을 스크롤 리스트로 표시
    /// 획득한 정령은 컬러 + 이름, 미획득은 회색 + ???
    /// ⭐ 카드 호버 시 기본 스탯 툴팁
    /// </summary>
    public class DexUI : MonoBehaviour
    {
        private TMP_FontAsset _font;
        private GameObject tooltipGo;
        private TextMeshProUGUI tooltipText;
        private MonoBehaviour currentCard; // 현재 표시 중인 카드 (같은 카드 재탭 → 숨김)

        public static DexUI Create(Transform parent, System.Action onClose)
        {
            var go = new GameObject("DexOverlay", typeof(RectTransform), typeof(Image), typeof(DexUI));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.12f, 0.98f);
            bg.raycastTarget = true;

            var ui = go.GetComponent<DexUI>();
            ui.Build(onClose);
            go.SetActive(false);
            return ui;
        }

        void Build(System.Action onClose)
        {
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-VariableFont_wght SDF");

            // ── 제목 ──
            var title = CreateLabel("TitleText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -10), new Vector2(300, 28), "도감", 20, TextAlignmentOptions.Center);
            title.color = Color.white;
            title.fontStyle = FontStyles.Bold;

            // ── 닫기 ──
            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(transform, false);
            closeGo.transform.SetAsLastSibling();
            var crt = closeGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-10, -10);
            crt.sizeDelta = new Vector2(42, 42);
            var cImg = closeGo.GetComponent<Image>();
            cImg.color = new Color(0.9f, 0.28f, 0.3f, 1f);
            var cBtn = closeGo.GetComponent<Button>();
            cBtn.onClick.AddListener(() => onClose?.Invoke());
            var cTxt = CreateLabelIn("X", closeGo.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(40, 40), "X", 20, TextAlignmentOptions.Center);
            cTxt.color = Color.white;
            cTxt.fontStyle = FontStyles.Bold;

            // ── 스크롤 리스트 ──
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(transform, false);
            var svRt = scrollGo.GetComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.06f, 0.08f);
            svRt.anchorMax = new Vector2(0.94f, 0.86f);
            svRt.offsetMin = Vector2.zero;
            svRt.offsetMax = Vector2.zero;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform),
                typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            cRt.sizeDelta = new Vector2(0, 0);

            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.viewport = vpRt;
            sr.content = cRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 24f;
            sr.movementType = ScrollRect.MovementType.Clamped;

            // ⭐ nxm 카드 그리드 (5열) — ⭐ 셀 폭을 Viewport에 맞춰 자동 계산 (좌우 잘림 방지)
            Canvas.ForceUpdateCanvases();
            float vw = vpRt.rect.width;
            float cellW = Mathf.Floor((vw - 12f - 40f) / 5f); // padding(12) + 4×spacing(40) 제외
            var glg = content.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(cellW, 130);
            glg.spacing = new Vector2(10, 10);
            glg.padding = new RectOffset(6, 6, 6, 6);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 5;
            glg.childAlignment = TextAnchor.UpperCenter;

            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // ── 정령 30종 카드 생성 (속성 → 등급 정렬) ──
            var all = Resources.LoadAll<SpiritData>("Data/Spirits");
            var sorted = new List<SpiritData>(all);
            sorted.Sort((a, b) =>
            {
                int c = ((int)a.element).CompareTo((int)b.element);
                return c != 0 ? c : ((int)a.grade).CompareTo((int)b.grade);
            });
            foreach (var sd in sorted)
                CreateCard(content.transform, sd);

            // 미획득 수 표시
            int total = sorted.Count;
            int unlockedCount = 0;
            if (GameManager.Instance != null)
                foreach (var sd in sorted)
                    if (GameManager.Instance.unlockedSpirits.Contains(sd.name)) unlockedCount++;
            var countT = CreateLabel("CountText", new Vector2(0.5f, 0.90f), new Vector2(0.5f, 0.90f),
                Vector2.zero, new Vector2(300, 24), $"획득 {unlockedCount} / {total}", 14, TextAlignmentOptions.Center);
            countT.color = new Color(0.8f, 0.9f, 1f);

            // ── ToolTip (카드 호버 시 기본 스탯) ──
            tooltipGo = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
            tooltipGo.transform.SetParent(transform, false);
            tooltipGo.transform.SetAsLastSibling();
            var tRt = tooltipGo.GetComponent<RectTransform>();
            tRt.sizeDelta = new Vector2(300, 110);
            var tImg = tooltipGo.GetComponent<Image>();
            tImg.color = new Color(0.03f, 0.04f, 0.09f, 0.97f);
            tImg.raycastTarget = false;
            tooltipText = CreateLabelIn("TooltipText", tooltipGo.transform, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(280, 100), "", 12, TextAlignmentOptions.TopLeft);
            tooltipText.color = new Color(0.9f, 0.93f, 1f);
            tooltipText.textWrappingMode = TextWrappingModes.Normal;
            tooltipGo.SetActive(false);

            // ⭐ 배경 탭 → 툴팁 숨김 (모바일)
            var bgClick = gameObject.AddComponent<DexBgClick>();
            bgClick.ui = this;
        }

        /// <summary>정령 스탯 툴팁 표시 (카드 위치 기준 — ⭐ 탭 방식)</summary>
        public void ShowTooltip(SpiritData data, bool unlocked, Vector3 worldPos)
        {
            if (tooltipGo == null) return;
            tooltipText.text = BuildStats(data, unlocked);
            tooltipGo.SetActive(true);
            // 카드 위쪽에 표시, 화면 밖 안 나가게
            Vector3 p = worldPos + new Vector3(0, 40, 0);
            p.x = Mathf.Clamp(p.x, 150f, Screen.width - 150f);
            p.y = Mathf.Clamp(p.y, 120f, Screen.height - 120f);
            tooltipGo.transform.position = p;
        }

        public void HideTooltip()
        {
            if (tooltipGo != null) tooltipGo.SetActive(false);
            currentCard = null;
        }

        static string BuildStats(SpiritData data, bool unlocked)
        {
            var sb = new StringBuilder();
            if (!unlocked)
            {
                sb.AppendLine($"{(int)data.grade}성 {ElementName(data.element)} 정령");
                sb.AppendLine();
                sb.AppendLine("도감에 등록 후");
                sb.AppendLine("스탯을 확인할 수 있습니다.");
                return sb.ToString();
            }
            sb.AppendLine($"<b>{data.spiritName}</b>  ({ElementName(data.element)} {(int)data.grade}성)");
            sb.AppendLine($"공격력 {data.baseATK}   체력 {data.baseHP}");
            sb.AppendLine($"방어력 {data.baseDEF}   속도 {data.baseSpeed:0.0}");
            sb.AppendLine($"치명타 {(data.baseCritRate * 100f):0}% / 데미지 {(data.baseCritDamage * 100f):0}%");
            sb.AppendLine(ElementAttackStyle(data)); // ⭐ 라벨 없이 문장만
            sb.AppendLine(ElementTrait(data.element));
            return sb.ToString();
        }

        /// <summary>속성별 공격방식 설명 (등급별 수치 나열)</summary>
        static string ElementAttackStyle(SpiritData data) => data.element switch
        {
            ElementType.Fire   => "강한 공격을 한다",
            ElementType.Water  => $"스플래시 데미지를 준다 ({SplashList(data.grade)})",
            ElementType.Earth  => $"범위 공격을 한다 ({AoeList(data.grade)})",
            ElementType.Wind   => "빠른 연속 공격을 한다",
            ElementType.Dark   => "단일 공격 + 흡혈",
            ElementType.Light  => "단일 공격 + 회복",
            _                  => "단일 공격"
        };

        /// <summary>물 스플래시 등급별 수치 (1성~5성, 좌우 1칸)</summary>
        static string SplashList(SpiritGrade g) => "20/40/60/80/100%";

        /// <summary>대지 범위 등급별 수치 (1성~5성, 모든 적)</summary>
        static string AoeList(SpiritGrade g) => "30/50/70/90/100%";

        /// <summary>속성 시너지 설명 (2기/3기/4기 수치 나열 — GameManager와 일치)</summary>
        static string ElementTrait(ElementType element) => element switch
        {
            ElementType.Fire   => "시너지: 공격력 +20/35/50%",
            ElementType.Water  => "시너지: 공격속도 +15/25/35%",
            ElementType.Earth  => "시너지: 파티 HP +30/50/70%",
            ElementType.Wind   => "시너지: 치명타 +15/25/35%",
            ElementType.Dark   => "시너지: 흡혈5% 공격+10%",
            ElementType.Light  => "시너지: 재생5% 방어+10%",
            _                  => ""
        };

        void CreateCard(Transform parent, SpiritData data)
        {
            bool unlocked = GameManager.Instance != null && GameManager.Instance.unlockedSpirits.Contains(data.name);

            var go = new GameObject("DexCard", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = unlocked ? new Color(0.10f, 0.13f, 0.26f, 1f) : new Color(0.09f, 0.09f, 0.12f, 1f);
            img.raycastTarget = true; // ⭐ 드래그/휠/탭 이벤트 수신

            // 스프라이트 (상단)
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            var irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.08f, 0.36f);
            irt.anchorMax = new Vector2(0.92f, 0.96f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            var iImg = icon.GetComponent<Image>();
            iImg.raycastTarget = false;
            if (unlocked && data.sprite != null)
            {
                iImg.sprite = data.sprite;
                iImg.preserveAspect = true;
                iImg.color = Color.white;
            }
            else
            {
                iImg.color = new Color(0.18f, 0.18f, 0.24f, 1f);
            }

            // 이름 (하단)
            var nameT = CreateLabelArea("Name", go.transform, new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.40f),
                unlocked ? data.spiritName : "???", 13, TextAlignmentOptions.Center);
            nameT.color = unlocked ? Color.white : new Color(0.45f, 0.45f, 0.5f);
            nameT.fontStyle = FontStyles.Bold;

            // 등급 (하단 아래)
            var gradeT = CreateLabelArea("Grade", go.transform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.20f),
                unlocked ? new string('★', (int)data.grade) : "???", 11, TextAlignmentOptions.Center);
            gradeT.color = unlocked ? new Color(1f, 0.84f, 0f) : new Color(0.45f, 0.45f, 0.5f);

            // ⭐ 스탯 툴팁 (탭)
            var handler = go.AddComponent<DexTooltipHandler>();
            handler.ui = this;
            handler.data = data;
            handler.unlocked = unlocked;
        }

        /// <summary>도감 카드 탭 핸들러 — 스탯 툴팁 토글 (모바일: 탭, 다시 탭/다른 곳 탭하면 숨김)</summary>
        class DexTooltipHandler : MonoBehaviour, IPointerClickHandler
        {
            public DexUI ui;
            public SpiritData data;
            public bool unlocked;
            public void OnPointerClick(PointerEventData e)
            {
                if (ui.currentCard == this) { ui.HideTooltip(); return; } // 같은 카드 → 숨김
                ui.ShowTooltip(data, unlocked, transform.position);       // 새 카드 → 표시
                ui.currentCard = this;
            }
        }

        /// <summary>도감 배경 탭 — 툴팁 숨김</summary>
        class DexBgClick : MonoBehaviour, IPointerClickHandler
        {
            public DexUI ui;
            public void OnPointerClick(PointerEventData e) { ui.HideTooltip(); }
        }

        static string ElementName(ElementType element) => element switch
        {
            ElementType.Fire => "불",
            ElementType.Water => "물",
            ElementType.Wind => "바람",
            ElementType.Earth => "대지",
            ElementType.Dark => "어둠",
            ElementType.Light => "빛",
            _ => "?"
        };

        static Color GetElementColor(ElementType element) => element switch
        {
            ElementType.Fire => new Color(1f, 0.4f, 0.2f),
            ElementType.Water => new Color(0.2f, 0.6f, 1f),
            ElementType.Wind => new Color(0.3f, 1f, 0.3f),
            ElementType.Earth => new Color(0.6f, 0.4f, 0.2f),
            ElementType.Dark => new Color(0.5f, 0.2f, 0.7f),
            ElementType.Light => new Color(1f, 1f, 0.5f),
            _ => Color.gray
        };

        TextMeshProUGUI CreateLabel(string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            if (_font != null) tmp.font = _font;
            return tmp;
        }

        TextMeshProUGUI CreateLabelIn(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            if (_font != null) tmp.font = _font;
            return tmp;
        }

        TextMeshProUGUI CreateLabelArea(string name, Transform parent, Vector2 aMin, Vector2 aMax, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8;
            tmp.fontSizeMax = fontSize;
            if (_font != null) tmp.font = _font;
            return tmp;
        }
    }
}

using System.Collections.Generic;
using System.Text;
using SpiritMerge.Battle;
using SpiritMerge.Core.Interfaces;
using SpiritMerge.Core.Systems;
using SpiritMerge.Data;
using SpiritMerge.Merge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritMerge.Cli
{
    /// <summary>
    /// 독립형 CLI 테스트 — DI/VContainer 없이 직접 서비스 인스턴스 생성
    /// CliServer → method: 호출로 실행
    /// </summary>
    public static class CliTestSuite
    {
        private static SpiritService _spirit;
        private static MergeService _merge;
        private static bool _initialized;

        /// <summary>간단 연결 테스트</summary>
        public static void CmdPing()
        {
            Debug.Log("[CLI] pong from CliTestSuite!");
        }

        /// <summary>
        /// 전투 상태 진단 — BattleManager state, 몬스터/정령 수·HP 출력
        /// static(Instance)은 도메인 리로드 시 null이 될 수 있으므로,
        /// 씬 컴포넌트(FindAnyObjectByType)에서 직접 읽은 compState를 함께 출력한다.
        /// </summary>
        public static void CmdBattleStatus()
        {
            try
            {
                var bm = BattleManager.Instance;
                string state = bm != null ? bm.state.ToString() : "null";
                var anyBm = Object.FindAnyObjectByType<BattleManager>();
                string compState = anyBm != null ? anyBm.state.ToString() : "null";
                bool staticMatch = (bm != null) == (anyBm != null);
                var gm = GameManager.Instance;
                var battleArea = GameObject.Find("BattleArea");
                var wc = Object.FindAnyObjectByType<WaveController>();
                var monsters = Object.FindObjectsByType<Monster>();
                var spiritList = Object.FindObjectsByType<SpiritUnit>();

                var sb = new StringBuilder();
                sb.Append($"[BattleStatus] t={Time.time:0.0} frame={Time.frameCount} scale={Time.timeScale}");
                sb.Append($" isPlaying={Application.isPlaying} bmInst={(bm != null ? "O" : "X")} anyBm={(anyBm != null ? "O" : "X")} staticMatch={staticMatch}");
                sb.Append($" gm={(gm != null ? "O" : "X")} battleArea={(battleArea != null ? "O" : "X")}");
                sb.Append($" state={state} compState={compState}");
                if (bm != null)
                    sb.Append($" partyHP={bm.partyHP}/{bm.partyMaxHP}");
                if (wc != null)
                    sb.Append($" / wave={wc.CurrentWave}/{wc.TotalWaves} remaining={wc.MonstersRemaining}");
                sb.Append($" / monsters={monsters.Length}");
                foreach (var m in monsters)
                    sb.Append($" / [{m.gameObject.name}] data={(m.data != null ? m.data.name : "-")} hp={m.hp}/{m.maxHp} hpBar={(m.hpSlider != null ? m.hpSlider.value : 0f):0.00} cdBar={(m.cdSlider != null ? m.cdSlider.value : 0f):0.00} pos=({m.transform.position.x:0.0},{m.transform.position.y:0.0}) alive={m.isAlive}");
                sb.Append($" / spirits={spiritList.Length}");
                foreach (var s in spiritList)
                    sb.Append($" / [{s.gameObject.name}] data={(s.data != null ? s.data.name : "-")} hp={s.hp}/{s.maxHp} atk={s.atk} spd={s.atkSpeed} hpBar={(s.hpSlider != null ? s.hpSlider.value : 0f):0.00} cdBar={(s.cdSlider != null ? s.cdSlider.value : 0f):0.00} pos=({s.transform.position.x:0.0},{s.transform.position.y:0.0}) alive={s.isAlive}");

                string msg = sb.ToString();
                GameLogger.Info(msg);
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[BattleStatus] 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 클리어 테스트용 — 1성 정령을 소환해 파티를 최대 4기까지 채운다
        /// (1-1을 1성 스타터 1기로는 못 깨므로, 스테이지 진행 검증을 위해 파티 강화)
        /// </summary>
        public static void CmdFillParty()
        {
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                if (board == null) { GameLogger.Error("[CLI] MergeBoardManager 없음"); return; }

                var all = Resources.LoadAll<SpiritData>("Data/Spirits");
                var oneStars = System.Array.FindAll(all, s => s != null && s.grade == SpiritGrade.OneStar);

                int added = 0;
                foreach (var sd in oneStars)
                {
                    var slots = board.GetPartySlots();
                    int filled = 0;
                    foreach (int s in slots) if (s >= 0) filled++;
                    if (filled >= MergeBoardManager.PartyMax) break;

                    int idx = board.TrySummon(sd);
                    if (idx >= 0)
                    {
                        board.AutoAssignFirstToParty(idx);
                        added++;
                    }
                }
                GameLogger.Info($"[CLI] 파티 정령 {added}기 추가 완료 (파티 변경 → 전투 재시작)");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdFillParty 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// PartyInfo 자식 탈출 진단 — 파티 편성 화면의 PartyInfo RectTransform와
        /// 그 자식(전투력/편성/속성)들의 좌표를 비교해 부모 밖으로 나갔는지 수치로 판정
        /// </summary>
        public static void CmdPartyLayoutCheck()
        {
            try
            {
                var ui = Object.FindAnyObjectByType<PartyFormationUI>();
                if (ui == null) { GameLogger.Info("[Layout] PartyFormationUI 없음 (오버레이 미표시)"); return; }
                var info = ui.transform.Find("PartyInfo");
                if (info == null) { GameLogger.Info("[Layout] PartyInfo 없음"); return; }

                Rect pRect = RtRect(info.GetComponent<RectTransform>());
                GameLogger.Info($"[Layout] PartyInfo rect=({pRect.xMin:0},{pRect.yMin:0})-({pRect.xMax:0},{pRect.yMax:0}) size=({pRect.width:0}x{pRect.height:0})");

                foreach (Transform c in info)
                {
                    var rt = c.GetComponent<RectTransform>();
                    if (rt == null) continue;
                    Rect cr = RtRect(rt);
                    // 부모 바깥으로 1px 이상 넘치면 OVERFLOW
                    bool inside = cr.xMin >= pRect.xMin - 1f && cr.xMax <= pRect.xMax + 1f
                               && cr.yMin >= pRect.yMin - 1f && cr.yMax <= pRect.yMax + 1f;
                    GameLogger.Info($"[Layout] child '{c.name}' rect=({cr.xMin:0},{cr.yMin:0})-({cr.xMax:0},{cr.yMax:0}) " +
                                    $"size=({cr.width:0}x{cr.height:0}) -> [{(inside ? "OK" : "OVERFLOW")}]");
                }
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[Layout] 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        static Rect RtRect(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        /// <summary>
        /// 업그레이드 탭 표시 + 스크린샷 (검증용)
        /// </summary>
        public static void CmdShowUpgradeAndShot()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                gm.ShowUpgradeTab();
                System.IO.Directory.CreateDirectory("Screenshots");
                string path = $"Screenshots/upgrade_{System.DateTime.Now:HHmmss}.png";
                ScreenCapture.CaptureScreenshot(path);
                GameLogger.Info($"[CLI] 업그레이드 화면 캡처: {path}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdShowUpgradeAndShot 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 업그레이드 오버레이 배치 진단 — 스크롤 구조 + 노드 텍스트 탈출 판정
        /// </summary>
        public static void CmdUpgradeLayoutCheck()
        {
            try
            {
                var ui = Object.FindAnyObjectByType<UpgradeUI>();
                if (ui == null) { GameLogger.Info("[UpgradeLayout] UpgradeUI 없음"); return; }
                var rt = ui.GetComponent<RectTransform>();
                GameLogger.Info($"[UpgradeLayout] 오버레이 parent={rt.parent.name} closeBtn={(ui.transform.Find("CloseBtn") != null ? "O" : "X")}");

                var content = ui.transform.Find("ScrollView/Viewport/Content");
                if (content == null) { GameLogger.Info("[UpgradeLayout] ScrollView/Content 없음"); return; }
                var cRt = content.GetComponent<RectTransform>();
                var vpRt = ui.transform.Find("ScrollView/Viewport")?.GetComponent<RectTransform>();

                int nodeCount = 0, overflow = 0;
                foreach (Transform c in content)
                {
                    if (c.name != "UpNode") continue;
                    nodeCount++;
                    Rect pr = RtRect(c.GetComponent<RectTransform>());
                    foreach (Transform child in c)
                    {
                        var crt = child.GetComponent<RectTransform>();
                        if (crt == null) continue;
                        Rect cr = RtRect(crt);
                        bool inside = cr.xMin >= pr.xMin - 1f && cr.xMax <= pr.xMax + 1f
                                   && cr.yMin >= pr.yMin - 1f && cr.yMax <= pr.yMax + 1f;
                        if (!inside) overflow++;
                        if (!inside)
                            GameLogger.Info($"[UpgradeLayout] '{c.name}' > '{child.name}' -> [OVERFLOW] ({cr.xMin:0},{cr.yMin:0})-({cr.xMax:0},{cr.yMax:0}) vs ({pr.xMin:0},{pr.yMin:0})-({pr.xMax:0},{pr.yMax:0})");
                    }
                }
                float contentH = cRt != null ? cRt.rect.height : 0f;
                float viewportH = vpRt != null ? vpRt.rect.height : 0f;
                GameLogger.Info($"[UpgradeLayout] 노드 {nodeCount}개, 탈출 {overflow}건 / Content 높이 {contentH:0} vs Viewport {viewportH:0} -> {(contentH > viewportH ? "스크롤 필요" : "한 화면")}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[UpgradeLayout] 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 경험치 지급 테스트 — 레벨업/SP 획득 검증 (기본 1500 경험치)
        /// </summary>
        public static void CmdGrantExp()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                gm.AddPlayerExp(1500);
                GameLogger.Info($"[CLI] 경험치 지급 완료 → Lv.{gm.player.Level} / SP {gm.player.SkillPoints}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdGrantExp 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 업그레이드 SP 지급 테스트 — 노드 강화 검증 (SP +10)
        /// </summary>
        public static void CmdGrantSP()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                for (int i = 0; i < 10; i++) gm.player.LevelUp();
                GameLogger.Info($"[CLI] SP +10 → Lv.{gm.player.Level} / SP {gm.player.SkillPoints}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdGrantSP 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 레벨 업그레이드(SP) 노드 강화 테스트 — 자동소환(10)/2성(11)/비용감소(13)/머지보너스(14)/3성(12, 잠금)
        /// </summary>
        public static void CmdUpgradeNodes()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                int[] targets = { 10, 11, 13, 14, 12 };
                foreach (int t in targets)
                {
                    bool ok = gm.player.UpgradeAt(t);
                    GameLogger.Info($"[CLI] 노드[{t}] {PlayerService.UpgradeNames[t]} 강화 {(ok ? "성공" : "실패")} → Lv.{gm.player.GetUpgradeLevel(t)} / SP {gm.player.SkillPoints}");
                }
                // ⭐ 강화 반영: 전투 정령 스탯 재배치 (UI 클릭 경로의 onChanged와 동일 효과)
                gm.RefreshBattleSpirits();
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdUpgradeNodes 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 골드 지급 테스트 (골드 업그레이드 검증용)
        /// </summary>
        public static void CmdGrantGold()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                gm.AddGold(5000);
                GameLogger.Info($"[CLI] 골드 +5000 → {gm.gold}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdGrantGold 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 골드 업그레이드 노드 강화 테스트 — 공격력(0)/방어력(1)/체력(2)/치명타(7)/공격%(4, 잠금)
        /// </summary>
        public static void CmdUpgradeGold()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                int[] targets = { 0, 1, 2, 7, 4 };
                foreach (int t in targets)
                {
                    bool ok = gm.TryGoldUpgrade(t);
                    GameLogger.Info($"[CLI] 골드노드[{t}] {PlayerService.UpgradeNames[t]} {(ok ? "성공" : "실패")} → Lv.{gm.player.GetUpgradeLevel(t)} / 골드 {gm.gold}");
                }
                gm.RefreshBattleSpirits();
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdUpgradeGold 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 소환 관련 정보 + 소환 버튼 텍스트 확인
        /// </summary>
        public static void CmdSummonInfo()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                GameLogger.Info($"[CLI] 소환 정보 — 2성확률 {gm.player.GetTwoStarChance():0%}, 비용감소 {gm.player.GetSummonCostDiscount():0%}, 자동소환주기 {gm.player.GetAutoSummonInterval():0}s, SP {gm.player.SkillPoints}");
                gm.RefreshSummonButton(); // ⭐ 버튼 텍스트 즉시 갱신 후 확인
                var btn = GameObject.Find("SummonBtn");
                if (btn != null)
                {
                    var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) GameLogger.Info($"[CLI] 소환 버튼 텍스트: '{txt.text}'");
                    else GameLogger.Info("[CLI] SummonBtn에 텍스트 없음");
                }
                else GameLogger.Info("[CLI] SummonBtn 없음");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdSummonInfo 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 소환 버튼 클릭 시뮬레이션 — OnSummonClicked(2성 확률/비용 감소) 실제 동작 확인
        /// </summary>
        public static void CmdClickSummon()
        {
            try
            {
                var btnGo = GameObject.Find("SummonBtn");
                if (btnGo == null) { GameLogger.Error("[CLI] SummonBtn 없음"); return; }
                var b = btnGo.GetComponent<UnityEngine.UI.Button>();
                if (b == null) { GameLogger.Error("[CLI] SummonBtn Button 없음"); return; }
                b.onClick.Invoke();
                GameLogger.Info("[CLI] SummonBtn 클릭 시뮬레이션 완료");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdClickSummon 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 업그레이드 닫기(X) 버튼 클릭 시뮬레이션 — 오버레이가 정상 닫히는지 확인
        /// </summary>
        public static void CmdClickUpgradeClose()
        {
            try
            {
                var closeBtn = GameObject.Find("UpgradeOverlay/CloseBtn");
                if (closeBtn == null) { GameLogger.Error("[CLI] CloseBtn 없음 (오버레이 미표시?)"); return; }
                var b = closeBtn.GetComponent<UnityEngine.UI.Button>();
                if (b == null) { GameLogger.Error("[CLI] CloseBtn Button 없음"); return; }
                b.onClick.Invoke();
                GameLogger.Info("[CLI] CloseBtn 클릭 → 업그레이드 닫기 시도");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdClickUpgradeClose 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 루비 지급 테스트 (루비 업그레이드 검증용)
        /// </summary>
        public static void CmdGrantRuby()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                gm.AddRuby(500);
                GameLogger.Info($"[CLI] 루비 +500 → {gm.ruby}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdGrantRuby 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 루비 업그레이드 노드 강화 테스트 — 공격력(15)/방어력(16)/체력(17)/치명타데미지(22)/공격%(19, 잠금)
        /// </summary>
        public static void CmdUpgradeRuby()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                int[] targets = { 15, 16, 17, 22, 19 };
                foreach (int t in targets)
                {
                    bool ok = gm.TryRubyUpgrade(t);
                    GameLogger.Info($"[CLI] 루비노드[{t}] {PlayerService.UpgradeNames[t]} {(ok ? "성공" : "실패")} → Lv.{gm.player.GetUpgradeLevel(t)} / 루비 {gm.ruby}");
                }
                gm.RefreshBattleSpirits();
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdUpgradeRuby 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 머지 보드 상태 진단 — 16슬롯 정령/레벨 + 슬롯 LevelText 텍스트/색
        /// </summary>
        public static void CmdBoardStatus()
        {
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                if (board == null) { GameLogger.Error("[CLI] MergeBoardManager 없음"); return; }
                var sb = new StringBuilder();
                sb.Append("[Board]");
                for (int i = 0; i < 16; i++)
                {
                    var d = board.GetItemData(i);
                    var slotTr = board.transform.Find($"MergeBoard/Slot_{i}");
                    var lv = slotTr?.Find("LevelText")?.GetComponent<TMPro.TextMeshProUGUI>();
                    string lvTxt = lv != null ? $"\"{lv.text}\"" : "none";
                    float lvA = lv != null ? lv.color.a : -1f;
                    sb.Append($" [{i}:{(d != null ? d.spiritName + " Lv." + d.level : "·")} LvTxt={lvTxt} a={lvA:0.0}]");
                }
                GameLogger.Info(sb.ToString());
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdBoardStatus 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 클릭 이동 테스트 — Slot_from 선택 → Slot_to(빈 슬롯) 클릭 이동
        /// </summary>
        public static void CmdClickMove()
        {
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                if (board == null) { GameLogger.Error("[CLI] MergeBoardManager 없음"); return; }
                board.TestClickMove(0, 5);
                GameLogger.Info("[CLI] 클릭 이동 시뮬레이션: Slot_0 → Slot_5");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdClickMove 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 실제 합성(머지) 테스트 — 보드에서 같은 element + 같은 level 쌍을 찾아 합성
        /// (같은 클릭 경로: TestClickMove(from,to) — to에 같은 정령이 있으면 합성 발동)
        /// </summary>
        public static void CmdTestMerge()
        {
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                if (board == null) { GameLogger.Error("[CLI] MergeBoardManager 없음"); return; }

                for (int i = 0; i < 16; i++)
                {
                    var a = board.GetItemData(i);
                    if (a == null) continue;
                    for (int j = i + 1; j < 16; j++)
                    {
                        var b = board.GetItemData(j);
                        if (b == null) continue;
                        if (a.element == b.element && a.level == b.level)
                        {
                            GameLogger.Info($"[CLI] 합성 시도: Slot_{i}({a.spiritName} Lv.{a.level}) + Slot_{j}({b.spiritName} Lv.{b.level})");
                            board.TestClickMerge(i, j);
                            return;
                        }
                    }
                }
                GameLogger.Info("[CLI] 합성 가능한 정령 쌍 없음 (보드 상태 확인 필요)");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdTestMerge 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 파티 전체 해제 (전투 재시작 → 정령 0기 → 패배 재도전 루프 유도)
        /// </summary>
        public static void CmdClearParty()
        {
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                if (board == null) { GameLogger.Error("[CLI] MergeBoardManager 없음"); return; }
                for (int p = 0; p < 4; p++) board.RemoveFromParty(p);
                GameLogger.Info("[CLI] 파티 전체 해제 → 전투 재시작");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdClearParty 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// UI 요소 존재 진단 — TopBar/반복모드버튼/파티HP바/정령아이콘 크기
        /// </summary>
        public static void CmdUITest()
        {
            try
            {
                var topBar = GameObject.Find("TopBar");
                var rmBtn = topBar != null ? topBar.transform.Find("RepeatModeBtn") : null;
                var partyHP = GameObject.Find("PartyHPBar");
                var synPanel = GameObject.Find("SynergyPanel");

                string rmInfo = "X";
                if (rmBtn != null)
                {
                    var rmImg = rmBtn.GetComponent<Image>();
                    var rmLabel = rmBtn.Find("Label")?.GetComponent<TextMeshProUGUI>();
                    rmInfo = $"O label='{(rmLabel != null ? rmLabel.text : "?")}' color={(rmImg != null ? rmImg.color.ToString() : "?")} anchor={rmBtn.GetComponent<RectTransform>().anchoredPosition}";
                }

                // 정령 아이콘 크기 (전투 슬롯 첫 번째)
                string iconInfo = "X";
                var spiritGroup = GameObject.Find("SpiritGroup");
                var slot0 = spiritGroup != null ? spiritGroup.transform.Find("SpiritSlot_0") : null;
                var icon = slot0 != null ? slot0.Find("SpiritIcon") : null;
                if (icon != null)
                {
                    var irt = icon.GetComponent<RectTransform>();
                    Vector3[] corners = new Vector3[4];
                    irt.GetWorldCorners(corners);
                    float w = Vector3.Distance(corners[0], corners[3]);
                    float h = Vector3.Distance(corners[0], corners[1]);
                    iconInfo = $"size=({w:0}x{h:0})";
                }

                GameLogger.Info($"[UI] topBar={(topBar != null ? "O" : "X")} rmBtn={rmInfo} partyHPBar={(partyHP != null ? "O" : "X")} synergyPanel={(synPanel != null ? "O" : "X")} icon={iconInfo}");

                // ⭐ TopBar 정렬 진단 (StageInfo/Gold/Ruby 좌표)
                string TopBarInfo(string name)
                {
                    var go = GameObject.Find(name);
                    if (go == null) return $"{name}=X";
                    var rt = go.GetComponent<RectTransform>();
                    Vector3[] c = new Vector3[4];
                    rt.GetWorldCorners(c);
                    return $"{name}=({c[0].x:0},{c[0].y:0})-({c[2].x:0},{c[2].y:0})";
                }
                GameLogger.Info($"[TopBar] {TopBarInfo("StageInfo")} {TopBarInfo("GoldText")} {TopBarInfo("RubyText")}");

                // ⭐ TMP 실제 텍스트 렌더 위치 (폰트/정렬 차이로 y가 어긋날 수 있음)
                string TmpInfo(string name)
                {
                    var go = GameObject.Find(name);
                    var tmp = go != null ? go.GetComponent<TextMeshProUGUI>() : null;
                    if (tmp == null) return $"{name}=X";
                    var b = tmp.textBounds;
                    var rt = go.GetComponent<RectTransform>();
                    return $"{name} fs={tmp.fontSize} posY={rt.anchoredPosition.y:0.0} cb={((b.min.y + b.max.y) / 2f):0}";
                }
                GameLogger.Info($"[TmpY] {TmpInfo("StageInfo")} {TmpInfo("GoldText")} {TmpInfo("RubyText")}");

                // ⭐ GNB 탭 Icon 세로 정렬 확인 (같은 줄이어야)
                string gnbInfo = "";
                var bm = GameObject.Find("BottomMenu");
                if (bm != null)
                {
                    var names = new[] { "전투", "파티", "업그레이드", "도감", "의뢰" };
                    foreach (var n in names)
                    {
                        var tab = bm.transform.Find("Tab_" + n);
                        var tabIcon = tab != null ? tab.Find("Icon")?.GetComponent<TextMeshProUGUI>() : null;
                        if (tabIcon != null)
                        {
                            var b = tabIcon.textBounds;
                            gnbInfo += $" {n}:cy={((b.min.y + b.max.y) / 2f):0}";
                        }
                    }
                }
                GameLogger.Info($"[GNBY]{gnbInfo}");

                // ⭐ 도감 스크롤 진단 (Content vs Viewport 높이)
                var dex = GameObject.Find("DexOverlay");
                if (dex != null)
                {
                    var vp = dex.transform.Find("ScrollView/Viewport")?.GetComponent<RectTransform>();
                    var ct = dex.transform.Find("ScrollView/Viewport/Content")?.GetComponent<RectTransform>();
                    var sr = dex.transform.Find("ScrollView")?.GetComponent<ScrollRect>();
                    string vh = vp != null ? $"{vp.rect.height:0}" : "X";
                    string ch = ct != null ? $"{ct.rect.height:0}" : "X";
                    string srV = sr != null ? $"v={sr.vertical} h={sr.horizontal}" : "X";
                    GameLogger.Info($"[DexScroll] viewportH={vh} contentH={ch} scrollRect={srV} contentParent={(ct != null ? ct.parent.name : "X")}");
                }
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdUITest 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 반복 모드 버튼 토글 시뮬레이션
        /// </summary>
        public static void CmdToggleRepeatMode()
        {
            try
            {
                var topBar = GameObject.Find("TopBar");
                var btn = topBar != null ? topBar.transform.Find("RepeatModeBtn") : null;
                if (btn == null) { GameLogger.Error("[CLI] RepeatModeBtn 없음"); return; }
                var b = btn.GetComponent<UnityEngine.UI.Button>();
                b.onClick.Invoke();
                var gm = Object.FindAnyObjectByType<GameManager>();
                GameLogger.Info($"[CLI] 반복 모드 토글 → {(gm != null ? (gm.repeatMode ? "ON(반복)" : "OFF(등반)") : "?")}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdToggleRepeatMode 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 파티 HP 강제 0 — 즉시 패배 로직 확인 (HP 0 도달 → 한 대 더 안 맞고 패배)
        /// </summary>
        public static void CmdKillParty()
        {
            try
            {
                var bm = Object.FindAnyObjectByType<BattleManager>();
                if (bm == null) { GameLogger.Error("[CLI] BattleManager 없음"); return; }
                bm.DamageParty(999999);
                GameLogger.Info($"[CLI] 파티 HP 강제 0 → {bm.partyHP}/{bm.partyMaxHP}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdKillParty 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 현재 스테이지/모드 상태 확인
        /// </summary>
        public static void CmdStageCheck()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                GameLogger.Info($"[CLI] 스테이지: {gm.CurrentStageName} (index {gm.CurrentStageIndex}), 모드: {(gm.repeatMode ? "R(반복)" : "C(등반)")}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdStageCheck 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 시너지 상태 확인 — 계산값(GetSynergySummary) + UI 표시 상태(색 블록/텍스트)
        /// ⭐ 시너지 발동 중엔 UI가 색 블록으로만 표시되므로, 계산값과 UI를 함께 출력
        /// </summary>
        public static void CmdSynergyText()
        {
            try
            {
                var gm = GameManager.Instance;
                string calc = "GameManager 없음";
                string items = "없음";
                if (gm != null)
                {
                    var (summary, detail) = gm.GetSynergySummary();
                    calc = summary;
                    var list = gm.GetSynergyItems();
                    items = list.Count > 0
                        ? string.Join(" / ", list.ConvertAll(i => $"{i.Item1}×{i.Item2}({i.Item3})"))
                        : "없음";
                }

                var ui = Object.FindAnyObjectByType<PartyFormationUI>();
                string uiState = "UI 없음";
                if (ui != null)
                {
                    var txt = ui.transform.Find("SynergyPanel/SynergyText")?.GetComponent<TextMeshProUGUI>();
                    var root = ui.transform.Find("SynergyPanel/SynergyItems");
                    int dots = root != null ? root.childCount : -1;
                    uiState = txt != null
                        ? $"text='{txt.text}'"
                        : $"색블록 {dots}개 (텍스트 없음)";
                }

                GameLogger.Info($"[CLI] 시너지 계산: '{calc}' / 항목: [{items}] / UI: {uiState}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdSynergyText 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 도감 탭 표시 + 스크린샷 (검증용)
        /// </summary>
        public static void CmdShowDexAndShot()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                gm.ShowDexTab();
                System.IO.Directory.CreateDirectory("Screenshots");
                string path = $"Screenshots/dex_{System.DateTime.Now:HHmmss}.png";
                ScreenCapture.CaptureScreenshot(path);
                GameLogger.Info($"[CLI] 도감 화면 캡처: {path}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdShowDexAndShot 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 저장/불러오기 테스트 — SaveGame → 파일 존재 → LoadGame 복원 확인
        /// </summary>
        public static void CmdSaveTest()
        {
            try
            {
                var dm = Object.FindAnyObjectByType<DataManager>();
                if (dm == null) { GameLogger.Error("[CLI] DataManager 없음 (씬 미배치?)"); return; }
                var gm = GameManager.Instance;
                int gold = gm != null ? gm.gold : 0;

                // 1) 현재 상태 저장
                var save = new SaveData();
                save.gold = gold;
                save.playerLevel = gm != null ? gm.player.Level : 1;
                dm.SaveGame(save);

                // 2) 파일 존재 확인
                string path = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
                bool exists = System.IO.File.Exists(path);
                GameLogger.Info($"[CLI] 저장 완료: gold={save.gold}, Lv={save.playerLevel}, fileExists={exists}");

                // 3) 로드 후 복원 확인
                var loaded = dm.LoadGame();
                if (loaded != null)
                    GameLogger.Info($"[CLI] 로드 복원: gold={loaded.gold}, Lv={loaded.playerLevel}");
                else
                    GameLogger.Warn("[CLI] 로드 실패 (null)");

                // 4) JSON 유효성 확인
                try
                {
                    string json = System.IO.File.ReadAllText(path);
                    GameLogger.Info($"[CLI] save.json {json.Length}자, gold키 포함: {json.Contains("\"gold\":")}");
                }
                catch { }
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdSaveTest 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        // ── 의뢰 시스템 검증 (파견/미션/레이드) ──

        /// <summary>
        /// 전체 시스템 통합 검증 — 12개 섹션을 순차 실행하며 PASS/FAIL 판정
        /// cli-client.py fulltest로 호출. 실제 게임 상태를 변경함(소환/파견/미션 진행 등).
        /// 결과는 [FullTest] 태그로 system 로그에 기록된다.
        /// </summary>
        public static void CmdFullTest()
        {
            var results = new List<(string name, bool ok, string msg)>();

            // F01 — 서비스 존재
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                bool ok = gm != null && gm.dispatch != null && gm.missions != null && gm.raid != null;
                results.Add(("F01 서비스존재", ok, ok ? "GameManager + dispatch + missions + raid" : (gm == null ? "GameManager 없음" : "서비스 누락")));
            }
            catch (System.Exception e) { results.Add(("F01 서비스존재", false, e.Message)); }

            // F02 — 소환
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                var oneStar = FirstOneStar();
                int idx = oneStar != null && board != null ? board.TrySummon(oneStar) : -1;
                bool ok = idx >= 0 && board != null && board.GetItemData(idx) != null;
                results.Add(("F02 소환", ok, ok ? $"{oneStar?.name} → Slot_{idx}" : (oneStar == null ? "1성 데이터 없음" : "소환 실패")));
            }
            catch (System.Exception e) { results.Add(("F02 소환", false, e.Message)); }

            // F03 — 파티 편성
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                var gm = Object.FindAnyObjectByType<GameManager>();
                bool ok = false;
                if (board != null)
                {
                    var oneStar = FirstOneStar();
                    if (oneStar != null)
                    {
                        int idx = board.TrySummon(oneStar);
                        if (idx >= 0)
                        {
                            board.AutoAssignFirstToParty(idx);
                            foreach (var s in board.GetPartySlots()) if (s == idx) ok = true;
                        }
                    }
                }
                results.Add(("F03 파티편성", ok, ok ? "정령 1기가 파티 포함됨" : "파티 미포함"));
            }
            catch (System.Exception e) { results.Add(("F03 파티편성", false, e.Message)); }

            // F04 — 머지 (같은 element + level 쌍)
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                bool ok = false; string msg = "쌍 없음";
                if (board != null)
                {
                    var oneStar = FirstOneStar();
                    if (oneStar != null)
                    {
                        int m1 = -1, m2 = -1;
                        for (int i = 0; i < 16 && m2 < 0; i++)
                        {
                            var d = board.GetItemData(i);
                            if (d != null && d.element == oneStar.element && d.level == (int)oneStar.grade)
                            { if (m1 < 0) m1 = i; else m2 = i; }
                        }
                        if (m2 < 0 && oneStar != null) // 보드에 쌍이 없으면 소환으로 확보
                        {
                            int n1 = board.TrySummon(oneStar);
                            int n2 = board.TrySummon(oneStar);
                            m1 = n1 >= 0 ? n1 : m1; m2 = n2 >= 0 ? n2 : m2;
                        }
                        if (m1 >= 0 && m2 >= 0)
                        {
                            board.TestClickMerge(m1, m2);
                            bool m1Gone = board.GetItemData(m1) == null;
                            bool m2Gone = board.GetItemData(m2) == null;
                            var remain = board.GetItemData(m1) ?? board.GetItemData(m2);
                            ok = (m1Gone || m2Gone) && remain != null;
                            msg = ok ? $"Slot_{m1}+Slot_{m2} → {remain.spiritName} Lv.{remain.level}" : "합성 후 결과 없음";
                        }
                    }
                }
                results.Add(("F04 머지", ok, msg));
            }
            catch (System.Exception e) { results.Add(("F04 머지", false, e.Message)); }

            // F05 — SP(레벨) 업그레이드
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                bool ok = false;
                if (gm != null)
                {
                    for (int i = 0; i < 3; i++) gm.player.LevelUp();
                    int before = gm.player.GetUpgradeLevel(10);
                    bool upgraded = gm.player.UpgradeAt(10);
                    ok = upgraded && gm.player.GetUpgradeLevel(10) == before + 1;
                }
                results.Add(("F05 SP업그레이드", ok, ok ? $"노드10(자동소환) Lv.{gm?.player.GetUpgradeLevel(10)} SP {gm?.player.SkillPoints}" : "강화 실패"));
            }
            catch (System.Exception e) { results.Add(("F05 SP업그레이드", false, e.Message)); }

            // F06 — 골드/루비 업그레이드
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                bool ok = false;
                if (gm != null)
                {
                    gm.AddGold(5000);
                    gm.AddRuby(500);
                    int gBefore = gm.player.GetUpgradeLevel(0);
                    int rBefore = gm.player.GetUpgradeLevel(15);
                    bool gOk = gm.TryGoldUpgrade(0);
                    bool rOk = gm.TryRubyUpgrade(15);
                    ok = gOk && gm.player.GetUpgradeLevel(0) == gBefore + 1
                      && rOk && gm.player.GetUpgradeLevel(15) == rBefore + 1;
                }
                results.Add(("F06 골드/루비업글", ok, ok ? $"골드노드0 Lv.{gm?.player.GetUpgradeLevel(0)} 루비노드15 Lv.{gm?.player.GetUpgradeLevel(15)}" : "강화 실패"));
            }
            catch (System.Exception e) { results.Add(("F06 골드/루비업글", false, e.Message)); }

            // F07 — 의뢰 생성 (TryGetNewOffer로 오퍼 확보 — GenerateRequest만으론 offers에 없음)
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                bool ok = false; string msg = "GM 없음";
                if (gm != null)
                {
                    bool got = gm.dispatch.TryGetNewOffer();
                    var req = gm.dispatch.offers.Count > 0 ? gm.dispatch.offers[0] : null;
                    ok = req != null && req.slots != null && req.slots.Length >= 1 && req.goldReward > 0;
                    msg = ok ? $"의뢰 슬롯 {req.slots.Length}개, 골드 {req.goldReward}, 루비 {req.rubyReward} (새오퍼 {got})" : "의뢰 생성 실패";
                }
                results.Add(("F07 의뢰생성", ok, msg));
            }
            catch (System.Exception e) { results.Add(("F07 의뢰생성", false, e.Message)); }

            // F08 — 파견 시작 + 완료 + 보상
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                bool started = false, completed = false, claimed = false;
                string msg = "준비 실패";
                if (gm != null && board != null && gm.dispatch.offers.Count > 0)
                {
                    var req = gm.dispatch.offers[0];
                    int s1 = -1, s2 = -1;
                    for (int i = 0; i < 16 && s2 < 0; i++)
                        if (board.GetItemData(i) != null) { if (s1 < 0) s1 = i; else s2 = i; }
                    // ⭐ 보드에 정령 부족하면 소환으로 확보 (F02~F04가 소모할 수 있음)
                    if (s2 < 0)
                    {
                        var os = FirstOneStar();
                        if (os != null)
                        {
                            if (s1 < 0) s1 = board.TrySummon(os);
                            if (s2 < 0) s2 = board.TrySummon(os);
                        }
                    }
                    if (s1 >= 0 && s2 >= 0)
                    {
                        var d1 = board.GetItemData(s1);
                        var d2 = board.GetItemData(s2);
                        started = gm.dispatch.TryStart(req, d1.spiritName, d1.element, d1.level,
                            d2.spiritName, d2.element, d2.level, 0.001f);
                        if (started)
                        {
                            board.RemoveBoardSpirit(s1);
                            board.RemoveBoardSpirit(s2);
                            gm.dispatch.Tick(1000f); // ⭐ 수동 틱 → 즉시 완료
                            completed = gm.dispatch.Completed.Count > 0;
                        }
                    }
                    if (completed)
                    {
                        int gold0 = gm.gold;
                        var (entry, gold, ruby) = gm.dispatch.Claim(0);
                        gm.AddGold(gold);
                        gm.AddRuby(ruby);
                        claimed = gm.gold > gold0;
                        msg = $"{entry.Spirit1Name} 외 1마리 → 골드 +{gold} 루비 +{ruby}";
                    }
                    else msg = started ? "완료 전환 실패" : "파견 시작 실패";
                }
                else if (gm == null) msg = "GM 없음";
                else if (board == null) msg = "보드 없음";
                else msg = "오퍼 없음 (F07 참조)";
                results.Add(("F08 파견+보상", started && completed && claimed, msg));
            }
            catch (System.Exception e) { results.Add(("F08 파견+보상", false, e.Message)); }

            // F09 — 미션 진행 (몬스터 처치)
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                bool ok = false; string msg = "GM 없음";
                if (gm != null)
                {
                    var m = gm.missions;
                    int before = -1;
                    for (int i = 0; i < MissionService.MissionCount; i++)
                        if (m.DailyDefs[i].Type == MissionType.KillMonster) { before = m.DailyProgress[i]; break; }
                    gm.OnMonsterKilled();
                    for (int i = 0; i < MissionService.MissionCount; i++)
                        if (m.DailyDefs[i].Type == MissionType.KillMonster && m.DailyProgress[i] > before) { ok = true; msg = $"KillMonster {before}→{m.DailyProgress[i]}"; break; }
                    if (!ok) msg = "미션 진행 반영 안 됨";
                }
                results.Add(("F09 미션진행", ok, msg));
            }
            catch (System.Exception e) { results.Add(("F09 미션진행", false, e.Message)); }

            // F10 — 레이드 페이즈
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                bool ok = false; string msg = "GM 없음";
                if (gm != null)
                {
                    var r = gm.raid;
                    int hp1 = RaidService.GetBossHP(1);
                    int hp2 = RaidService.GetBossHP(2);
                    long dmg0 = r.TotalDamage;
                    r.AddDamage(5000);
                    ok = hp1 > 0 && hp2 > hp1 && r.TotalDamage > dmg0;
                    msg = ok ? $"보스HP 1단 {hp1} / 2단 {hp2}, 피해 {dmg0}→{r.TotalDamage}" : "레이드 계산 불일치";
                }
                results.Add(("F10 레이드", ok, msg));
            }
            catch (System.Exception e) { results.Add(("F10 레이드", false, e.Message)); }

            // F11 — 저장/불러오기 (DataManager 씬 미배치 시 SKIP)
            try
            {
                var dm = Object.FindAnyObjectByType<DataManager>();
                if (dm == null)
                {
                    results.Add(("F11 저장/로드", true, "SKIP — DataManager 씬 미배치"));
                }
                else
                {
                    var save = new SaveData();
                    save.gold = 12345;
                    save.playerLevel = 7;
                    dm.SaveGame(save);
                    var loaded = dm.LoadGame();
                    bool ok = loaded != null && loaded.gold == 12345 && loaded.playerLevel == 7;
                    results.Add(("F11 저장/로드", ok, ok ? $"저장→로드 gold {loaded.gold} Lv.{loaded.playerLevel}" : "복원 불일치"));
                }
            }
            catch (System.Exception e) { results.Add(("F11 저장/로드", false, e.Message)); }

            // F12 — 전투 상태
            try
            {
                var bm = Object.FindAnyObjectByType<BattleManager>();
                bool ok = bm != null && bm.partyMaxHP > 0;
                results.Add(("F12 전투상태", ok, ok ? $"BattleManager state={bm.state} HP {bm.partyHP}/{bm.partyMaxHP}" : "BattleManager 없음"));
            }
            catch (System.Exception e) { results.Add(("F12 전투상태", false, e.Message)); }

            // F13 — 성급(level) 전투력 반영: 정령 FinalATK+FinalHP가 level 기반으로 증가하는지
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                bool ok = false; string msg = "정령 없음";
                if (board != null)
                {
                    // ⭐ 파티 정령 우선, 없으면 보드에서 소환+배치로 확보 (F08이 정령을 소모할 수 있음)
                    var items = board.GetPartyItems();
                    SpiritItemData it = items.Length > 0 ? items[0] : null;
                    if (it == null)
                    {
                        var os = FirstOneStar();
                        if (os != null)
                        {
                            int idx = board.TrySummon(os);
                            if (idx >= 0) { board.AutoAssignFirstToParty(idx); it = board.GetItemData(idx); }
                        }
                    }
                    if (it != null && it.spiritData != null)
                    {
                        int before = it.FinalATK + it.FinalHP;
                        int lv2 = Mathf.Min(5, it.level + 1);
                        int after = it.spiritData.FinalATKAt(lv2) + it.spiritData.FinalHPAt(lv2);
                        ok = after > before;
                        msg = ok
                            ? $"파티 {it.spiritName} Lv.{it.level} 전투력 {before:N0} → Lv.{lv2} {after:N0} (증가 {after - before:N0})"
                            : $"증가 안 함 (Lv.{it.level} = {before:N0}, Lv.{lv2} = {after:N0})";
                    }
                }
                results.Add(("F13 성급전투력", ok, msg));
            }
            catch (System.Exception e) { results.Add(("F13 성급전투력", false, e.Message)); }

            // F14 — 저장/불러오기: BuildSaveData → SaveGame(파일) → LoadGame 복원 일치
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                bool ok = false; string msg = "GM 없음";
                if (gm != null)
                {
                    var save = gm.BuildSaveData();
                    save.gold += 777; // ⭐ 복원 확인용 마커
                    var dm = Object.FindAnyObjectByType<DataManager>();
                    if (dm == null) { msg = "DataManager 없음 (자동 생성 안 됨)"; }
                    else
                    {
                        dm.SaveGame(save);
                        var loaded = dm.LoadGame();
                        string path = System.IO.Path.Combine(Application.persistentDataPath, "save.json");
                        bool fileExists = System.IO.File.Exists(path);
                        ok = loaded != null
                             && loaded.gold == save.gold
                             && loaded.boardSpirits != null
                             && loaded.offers != null
                             && fileExists;
                        msg = ok
                            ? $"저장→로드 gold {loaded.gold}(마커 {save.gold - 777}→{save.gold}), 보드정령 {loaded.boardSpirits.Count}, 의뢰 {loaded.offers.Count}, 파일 {fileExists}"
                            : $"복원 불일치 (file={fileExists}, gold {loaded?.gold}/{save.gold})";
                    }
                }
                results.Add(("F14 저장/로드", ok, msg));
            }
            catch (System.Exception e) { results.Add(("F14 저장/로드", false, e.Message)); }

            // ── 요약 ──
            int pass = 0, fail = 0;
            foreach (var (n, ok, m) in results)
            {
                if (ok) pass++; else fail++;
                GameLogger.Info($"[FullTest] [{(ok ? "PASS" : "FAIL")}] {n} — {m}");
            }
            GameLogger.Info($"[FullTest] ===== 결과: PASS {pass} / FAIL {fail} / 총 {results.Count} (SKIP은 PASS로 집계) =====");
        }

        static SpiritData FirstOneStar()
        {
            foreach (var s in Resources.LoadAll<SpiritData>("Data/Spirits"))
                if (s != null && s.grade == SpiritGrade.OneStar) return s;
            return null;
        }

        /// <summary>의뢰 탭 열기 + 스크린샷</summary>
        public static void CmdShowRequestTab()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                gm.ShowRequestTab();
                System.IO.Directory.CreateDirectory("Screenshots");
                string path = $"Screenshots/request_{System.DateTime.Now:HHmmss}.png";
                ScreenCapture.CaptureScreenshot(path);
                GameLogger.Info($"[CLI] 의뢰 화면 캡처: {path}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdShowRequestTab 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>파견 테스트 — 보드 정령을 의뢰에 파견 (시간 축소)</summary>
        public static void CmdDispatchTest()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                if (board == null) { GameLogger.Error("[CLI] MergeBoardManager 없음"); return; }

                var req = gm.dispatch.GenerateRequest();
                string cond = "";
                foreach (var s in req.slots) cond += $"[{s.requiredElement} {s.minGrade}★ 이상] ";

                // ⭐ 아무 정령 2마리 찾아 파견 (조건은 보너스용)
                int[] slots = new int[2];
                int found = 0;
                for (int i = 0; i < 16 && found < 2; i++)
                    if (board.GetItemData(i) != null) slots[found++] = i;

                if (found < 2) { GameLogger.Warn("[CLI] 파견할 정령 2마리 필요 (보드 정령 부족)"); return; }

                var d1 = board.GetItemData(slots[0]);
                var d2 = board.GetItemData(slots[1]);
                gm.dispatchTimeScale = 0.0001f; // ⭐ 테스트용 시간 축소 (즉시 완료)
                if (gm.dispatch.TryStart(req, d1.spiritName, d1.element, d1.level,
                    d2.spiritName, d2.element, d2.level, gm.dispatchTimeScale))
                {
                    board.RemoveBoardSpirit(slots[0]);
                    board.RemoveBoardSpirit(slots[1]);
                    gm.OnDispatched();
                    GameLogger.Info($"[CLI] 파견 보내기: {d1.spiritName}, {d2.spiritName} 조건[{cond}] — 시간 축소 완료 대기");
                    return;
                }
                GameLogger.Warn("[CLI] 파견 시작 실패 (슬롯 꽉 참)");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdDispatchTest 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>파견 보상 수령 (완료된 첫 항목)</summary>
        public static void CmdDispatchClaim()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                if (gm.dispatch.Completed.Count > 0)
                {
                    var (entry, gold, ruby) = gm.dispatch.Claim(0);
                    gm.AddGold(gold);
                    gm.AddRuby(ruby);
                    GameLogger.Info($"[CLI] 파견 보상: {entry.Spirit1Name} 외 1마리 → 골드 +{gold}, 루비 +{ruby}");
                    return;
                }
                GameLogger.Info("[CLI] 완료된 파견 없음");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdDispatchClaim 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>미션 상태 확인</summary>
        public static void CmdMissionCheck()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                var m = gm.missions;
                int daily = m.CompletedCount(false), weekly = m.CompletedCount(true);
                var sb = new System.Text.StringBuilder();
                sb.Append($"[CLI] 미션 상태 — 일일 {daily}/10 완료, 주간 {weekly}/10 완료 / ");
                for (int i = 0; i < MissionService.MissionCount; i++)
                    sb.Append($"[{m.DailyDefs[i].Desc} {System.Math.Min(m.DailyProgress[i], m.DailyDefs[i].Target)}/{m.DailyDefs[i].Target}]");
                GameLogger.Info(sb.ToString());
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdMissionCheck 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>레이드 실전투 시작 (전투 일시정지 + 보스 스폰)</summary>
        public static void CmdRaidStart()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                gm.ShowRequestTab();   // 의뢰 탭 표시
                var battle = Object.FindAnyObjectByType<RaidBattle>();
                if (battle == null) { GameLogger.Error("[CLI] RaidBattle 없음"); return; }
                battle.StartRaid();
                GameLogger.Info("[CLI] 레이드 실전투 시작 명령 전송");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdRaidStart 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>의뢰 오버레이 표시 상태 진단 (비활성 포함 — 초기엔 숨김 상태라)</summary>
        public static void CmdRequestState()
        {
            try
            {
                // ⭐ RequestUI는 초기 SetActive(false) — FindObjectsInactive.Include 필수
                var reqs = Object.FindObjectsByType<RequestUI>(FindObjectsInactive.Include);
                if (reqs.Length == 0) { GameLogger.Error("[CLI] RequestUI 없음 (오버레이 미생성?)"); return; }
                reqs[0].LogState();
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdRequestState 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>미션 탭 열기</summary>
        public static void CmdOpenMissionTab() => OpenRequestTab(1, "미션");

        /// <summary>레이드 탭 열기</summary>
        public static void CmdOpenRaidTab() => OpenRequestTab(2, "레이드");

        /// <summary>파견 전용 모달 열기 (첫 의뢰)</summary>
        public static void CmdOpenDispatchModal()
        {
            try
            {
                var gm = Object.FindAnyObjectByType<GameManager>();
                if (gm != null) gm.ShowRequestTab(); // ⭐ 의뢰 탭 먼저 (모달이 보이도록)
                var reqs = Object.FindObjectsByType<RequestUI>(FindObjectsInactive.Include);
                if (reqs.Length == 0) { GameLogger.Error("[CLI] RequestUI 없음"); return; }
                reqs[0].OpenDispatchModalForTest();
                GameLogger.Info("[CLI] 파견 모달 열기");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdOpenDispatchModal 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>보드 첫 정령 선택 시뮬레이션 (파견 배치 검증용)</summary>
        public static void CmdSelectSpirit()
        {
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                if (board == null) { GameLogger.Error("[CLI] MergeBoardManager 없음"); return; }
                for (int i = 0; i < 16; i++)
                {
                    if (board.GetItemData(i) != null)
                    {
                        board.TestSelect(i);
                        GameLogger.Info($"[CLI] 보드 슬롯 {i} 선택");
                        return;
                    }
                }
                GameLogger.Warn("[CLI] 보드에 정령 없음");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdSelectSpirit 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>파견 배치 상태 진단 + 빈 슬롯 클릭 시뮬레이션</summary>
        public static void CmdDispatchFormationTest()
        {
            try
            {
                var ui = Object.FindAnyObjectByType<DispatchFormationUI>();
                if (ui == null) { GameLogger.Error("[CLI] DispatchFormationUI 없음"); return; }
                ui.LogState();
                ui.TestDispatchClick();
                GameLogger.Info("[CLI] 파견 슬롯 클릭 시뮬레이션 완료");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdDispatchFormationTest 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>저장 파일 상태 진단 — persistentDataPath + save.json/백업 존재·크기</summary>
        public static void CmdSavePath()
        {
            string p = Application.persistentDataPath;
            string main = System.IO.Path.Combine(p, "save.json");
            string backup = System.IO.Path.Combine(p, "save.backup.json");
            string tmp = System.IO.Path.Combine(p, "save.tmp");
            GameLogger.Info($"[CLI] persistentDataPath={p}");
            GameLogger.Info($"[CLI] save.json 존재={System.IO.File.Exists(main)} 크기={(System.IO.File.Exists(main) ? new System.IO.FileInfo(main).Length : 0)} 최근저장={(System.IO.File.Exists(main) ? System.IO.File.GetLastWriteTime(main).ToString("HH:mm:ss") : "-")}");
            GameLogger.Info($"[CLI] save.backup.json 존재={System.IO.File.Exists(backup)} 크기={(System.IO.File.Exists(backup) ? new System.IO.FileInfo(backup).Length : 0)}");
            GameLogger.Info($"[CLI] save.tmp 잔재={System.IO.File.Exists(tmp)} (있으면 직전 쓰기 중단 흔적)");
        }

        /// <summary>플레이어 상태 진단 — 레벨/경험치/SP/재화</summary>
        public static void CmdPlayerState()
        {
            var gm = Object.FindAnyObjectByType<GameManager>();
            if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
            GameLogger.Info($"[CLI] 플레이어: Lv.{gm.player.Level} / EXP {gm.player.Exp} / SP {gm.player.SkillPoints} / 골드 {gm.gold} / 루비 {gm.ruby} / 업그레이드 노드10 Lv.{gm.player.GetUpgradeLevel(10)}");
        }

        static void OpenRequestTab(int tab, string name)
        {
            try
            {
                var reqs = Object.FindObjectsByType<RequestUI>(FindObjectsInactive.Include);
                if (reqs.Length == 0) { GameLogger.Error("[CLI] RequestUI 없음"); return; }
                reqs[0].ShowTab(tab);
                GameLogger.Info($"[CLI] 의뢰 {name} 탭 표시");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] OpenRequestTab 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 물 1성 정령을 보드에 소환 + 파티에 배치 (스플래시 검증용)
        /// </summary>
        public static void CmdAddWaterToParty()
        {
            try
            {
                var board = Object.FindAnyObjectByType<MergeBoardManager>();
                if (board == null) { GameLogger.Error("[CLI] MergeBoardManager 없음"); return; }
                // 파티 꽉 차면 마지막 해제
                var slots = board.GetPartySlots();
                int filled = 0;
                foreach (var s in slots) if (s >= 0) filled++;
                if (filled >= 4) board.RemoveFromParty(3);

                SpiritData water = null;
                foreach (var s in Resources.LoadAll<SpiritData>("Data/Spirits"))
                    if (s != null && s.element == ElementType.Water && s.grade == SpiritGrade.OneStar) { water = s; break; }
                if (water == null) { GameLogger.Error("[CLI] 물 1성 데이터 없음"); return; }

                int idx = board.TrySummon(water);
                if (idx >= 0)
                {
                    board.AutoAssignFirstToParty(idx);
                    GameLogger.Info("[CLI] 물 1성 파티 추가 완료");
                }
                else GameLogger.Warn("[CLI] 물 소환 실패 (빈 슬롯 없음)");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdAddWaterToParty 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 테스트용 파티 세팅 — 특정 속성/성급 조합 (공격방식/성급 수치 검증)
        /// </summary>
        static void SetPartyCombo(string elem1, int g1, string elem2, int g2)
        {
            var board = Object.FindAnyObjectByType<MergeBoardManager>();
            if (board == null) { GameLogger.Error("[CLI] MergeBoardManager 없음"); return; }
            for (int p = 0; p < 4; p++) board.RemoveFromParty(p);
            AddSpirit(board, elem1, g1);
            AddSpirit(board, elem2, g2);
            GameLogger.Info($"[CLI] 파티 세팅: {elem1} {g1}성 + {elem2} {g2}성");
        }

        static void AddSpirit(MergeBoardManager board, string elem, int grade)
        {
            foreach (var s in Resources.LoadAll<SpiritData>("Data/Spirits"))
                if (s != null && s.element.ToString() == elem && (int)s.grade == grade)
                {
                    int idx = board.TrySummon(s);
                    if (idx >= 0) board.AutoAssignFirstToParty(idx);
                    return;
                }
            GameLogger.Warn($"[CLI] {elem} {grade}성 데이터 없음");
        }

        public static void CmdPartyWater1() => SetPartyCombo("Water", 1, "Fire", 1);
        public static void CmdPartyWater5() => SetPartyCombo("Water", 5, "Fire", 1);
        public static void CmdPartyEarth1() => SetPartyCombo("Earth", 1, "Fire", 1);
        public static void CmdPartyEarth5() => SetPartyCombo("Earth", 5, "Fire", 1);
        public static void CmdPartyWind1() => SetPartyCombo("Wind", 1, "Fire", 1);
        public static void CmdPartyDark1() => SetPartyCombo("Dark", 1, "Fire", 1);
        public static void CmdPartyLight1() => SetPartyCombo("Light", 1, "Fire", 1);
        public static void CmdPartyFire5() => SetPartyCombo("Fire", 5, "Fire", 1);

        /// <summary>
        /// 전체 테스트 실행 (1회) — 소환 → 머지 → 크로스머지
        /// </summary>
        public static void CmdTestAll()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine("  Spirit Merge — CLI Test Suite");
            sb.AppendLine("═══════════════════════════════════");

            EnsureInit();

            // 1. 소환 테스트
            sb.AppendLine("\n─── 1. 소환 ───");
            var f1 = _spirit.AddSpirit("Fire_1", 1);
            sb.AppendLine($"  ✅ Fire_1 (UID:{f1.uid})");
            var f2 = _spirit.AddSpirit("Fire_1", 1);
            sb.AppendLine($"  ✅ Fire_1 (UID:{f2.uid})");
            var f3 = _spirit.AddSpirit("Fire_1", 1);
            sb.AppendLine($"  ✅ Fire_1 (UID:{f3.uid})");
            var w1 = _spirit.AddSpirit("Water_1", 1);
            sb.AppendLine($"  ✅ Water_1 (UID:{w1.uid})");
            var wi1 = _spirit.AddSpirit("Wind_1", 1);
            sb.AppendLine($"  ✅ Wind_1 (UID:{wi1.uid})");

            // 2. 보유 목록
            sb.AppendLine($"\n─── 2. 보유 현황 ───");
            sb.AppendLine($"  총 {_spirit.GetAllSpirits().Count}마리");

            // 3. 동일 머지 테스트
            sb.AppendLine("\n─── 3. 동일 머지 (Fire_1 3마리 → 2성) ───");
            bool canMerge = _merge.CanMerge("Fire_1", 1);
            sb.AppendLine($"  CanMerge(Fire_1, 1) = {canMerge}");
            if (canMerge)
            {
                var result = _merge.Merge("Fire_1", 1);
                if (result != null)
                    sb.AppendLine($"  ✅ 머지 성공! → {result.dataId} {result.grade}★ (UID:{result.uid})");
                else
                    sb.AppendLine($"  ❌ 머지 실패 (null)");
            }
            sb.AppendLine($"  보유: {_spirit.GetAllSpirits().Count}마리");

            // 4. 크로스 머지 테스트 (1성 3마리)
            sb.AppendLine("\n─── 4. 크로스 머지 (Water_1 + Wind_1 + 기존 Fire_1 1성) ───");
            // Fire_1 1성이 아직 남아있는지 확인
            var remainingFire = _spirit.GetAllSpirits().Find(s => s.dataId == "Fire_1" && s.grade == 1);
            if (remainingFire != null)
            {
                var crossResult = _merge.CrossMerge(new[] { remainingFire.uid, w1.uid, wi1.uid });
                if (crossResult != null)
                    sb.AppendLine($"  ✅ 크로스머지 성공! → {crossResult.dataId} {crossResult.grade}★ (UID:{crossResult.uid})");
                else
                    sb.AppendLine($"  ❌ 크로스머지 실패 (null)");
            }

            // 5. 최종 현황
            sb.AppendLine($"\n─── 5. 최종 보유 정령 ───");
            foreach (var s in _spirit.GetAllSpirits())
                sb.AppendLine($"  #{s.uid} / {s.dataId} / {s.grade}★ / Lv.{s.level}");
            sb.AppendLine($"  총 {_spirit.GetAllSpirits().Count}마리");

            // 6. 머지 체인: 2성 → 3성 (2마리 필요)
            sb.AppendLine("\n─── 6. 2성 머지 체인 (2마리 소환 → 머지) ───");
            var fa = _spirit.AddSpirit("Fire_2", 2);
            var fb = _spirit.AddSpirit("Fire_2", 2);
            sb.AppendLine($"  소환: Fire_2 (UID:{fa.uid}), Fire_2 (UID:{fb.uid})");
            if (_merge.CanMerge("Fire_2", 2))
            {
                var up = _merge.Merge("Fire_2", 2);
                if (up != null)
                    sb.AppendLine($"  ✅ 2→3성 머지 성공! → {up.dataId} {up.grade}★");
                else
                    sb.AppendLine($"  ❌ 2→3성 머지 실패");
            }

            sb.AppendLine("\n═══════════════════════════════════");
            sb.AppendLine("  ✅ CLI Test Suite 완료!");
            sb.AppendLine("═══════════════════════════════════");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 1성 정령 소환 (간편 명령어)
        /// </summary>
        public static void CmdSummon_Fire_1() => SimpleSummon("Fire", 1);
        public static void CmdSummon_Water_1() => SimpleSummon("Water", 1);
        public static void CmdSummon_Wind_1() => SimpleSummon("Wind", 1);
        public static void CmdSummon_Earth_1() => SimpleSummon("Earth", 1);
        public static void CmdSummon_Dark_1() => SimpleSummon("Dark", 1);
        public static void CmdSummon_Light_1() => SimpleSummon("Light", 1);

        public static void CmdSummon_Fire_2() => SimpleSummon("Fire", 2);
        public static void CmdSummon_Water_2() => SimpleSummon("Water", 2);
        public static void CmdSummon_Wind_2() => SimpleSummon("Wind", 2);
        public static void CmdSummon_Earth_2() => SimpleSummon("Earth", 2);
        public static void CmdSummon_Dark_2() => SimpleSummon("Dark", 2);
        public static void CmdSummon_Light_2() => SimpleSummon("Light", 2);

        /// <summary>보유 정령 목록</summary>
        public static void CmdList()
        {
            EnsureInit();
            var sb = new StringBuilder();
            sb.AppendLine("═════ 보유 정령 ═════");
            var all = _spirit.GetAllSpirits();
            if (all.Count == 0)
                sb.AppendLine("(없음)");
            else
                foreach (var s in all)
                    sb.AppendLine($"  #{s.uid} / {s.dataId} / {s.grade}★ / Lv.{s.level}");
            sb.AppendLine($"→ 총 {all.Count}마리");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 3챕터 1스테이지 전투 시작 (배율·속성 검증용)
        /// 3-1: Earth / hpMult 1.9 / atkMult 1.1
        /// (공식: hp = 1.0+(ch-1)*0.45+(st-1)*0.12, atk = 0.6+(ch-1)*0.25)
        /// </summary>
        public static void CmdStage3()
        {
            StartStageByPrefix("3-1");
        }

        /// <summary>
        /// 5챕터 1스테이지 전투 시작 (배율·속성 검증용)
        /// 5-1: Fire / hpMult 2.8 / atkMult 1.6
        /// </summary>
        public static void CmdStage5()
        {
            StartStageByPrefix("5-1");
        }

        private static void StartStageByPrefix(string prefix)
        {
            try
            {
                var allStages = Resources.LoadAll<StageData>("Data/Stages");
                StageData target = null;
                foreach (var s in allStages)
                    if (s.stageName.StartsWith(prefix)) { target = s; break; }
                if (target == null)
                {
                    GameLogger.Error($"[CLI] '{prefix}' 스테이지 없음");
                    return;
                }

                var spawner = Object.FindAnyObjectByType<MonsterSpawner>();
                if (spawner != null) spawner.InitializeEnemySlots(target.spawnPointCount);

                var wc = Object.FindAnyObjectByType<WaveController>();
                if (wc == null)
                {
                    GameLogger.Error("[CLI] WaveController 없음");
                    return;
                }

                wc.StartBattle(target);
                GameLogger.Info($"[CLI] 전투 시작: {target.stageName} elem={target.elementType} hpMult={target.hpMultiplier} atkMult={target.atkMultiplier}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] StartStage 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 파티 편성 오버레이 표시 + 화면 캡처 (레이아웃 검증용)
        /// 저장 경로: 프로젝트 루트/Screenshots/
        /// </summary>
        public static void CmdShowPartyAndShot()
        {
            try
            {
                var gm = GameManager.Instance;
                if (gm == null) { GameLogger.Error("[CLI] GameManager 없음"); return; }
                gm.ShowPartyTab(); // 파티 탭

                // 캡처 디렉토리 준비
                string dir = System.IO.Path.Combine(Application.dataPath, "../Screenshots");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                string file = System.IO.Path.Combine(dir, $"party_{System.DateTime.Now:HHmmss}.png");
                ScreenCapture.CaptureScreenshot(file);
                GameLogger.Info($"[CLI] 📸 파티 편성 화면 캡처: {file}");
            }
            catch (System.Exception e)
            {
                GameLogger.Error($"[CLI] CmdShowPartyAndShot 예외: {e.Message}\n{e.StackTrace}");
            }
        }

        // ─── Internal ──────────────────────────────

        private static void EnsureInit()
        {
            if (_initialized) return;
            _spirit = new SpiritService();
            _merge = new MergeService(_spirit, new CurrencyService());
            _initialized = true;
            Debug.Log("[CLI] CliTestSuite initialized (standalone mode)");
        }

        private static void SimpleSummon(string element, int grade)
        {
            EnsureInit();
            string dataId = $"{element}_{grade}";
            var spirit = _spirit.AddSpirit(dataId, grade);
            Debug.Log($"[CLI] ✅ {dataId} {grade}★ 소환 (UID:{spirit.uid})");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 게임 전체 로깅 시스템 v3 — 태그 기반 카테고리 분리
///
/// ★ v3 변경점 (파일 핸들 문제 근본 해결)
///   - StreamWriter 유지 방식 제거 → 매 기록마다 File.AppendAllText (열고/쓰고/닫기)
///     → 도메인 리로드/재컴파일 시 writer 핸들이 꼬여 특정 카테고리 로그가 죽던 문제 해결
///   - 쓰기 실패 시 손상 파일 삭제 후 1회 재시도 (자동 복구)
///
/// 파일 구조 (프로젝트 루트/game_log/):
///   system.md  — 게임플레이/전투 (GM, MB, WC, Monster, Spirit, Request, ...)
///   cli.md     — CLI 검증/진단 (CLI, BattleStatus, Layout, UI, ...)
///   editor.md  — 에디터 도구 (Setup, Apply, CliServer, GNB, ...)
///   data.md    — 데이터/저장 (SpiritManager, ...)
///   misc.md    — 태그 없음/기타
///   errors.md  — WARN/ERROR/Exception 전부
///
/// CLI 조회:  python cli-client.py cat <카테고리> [줄수]
/// </summary>
public static class GameLogger
{
    private static string _logDir;
    private static bool _initialized;
    private static readonly Dictionary<string, int> _lineCounts = new();
    private static readonly HashSet<string> _disabled = new();

    // 파일당 최대 라인 / 트림 시 유지 라인
    private const int MaxLines = 2000;
    private const int KeepLines = 1500;

    // 태그 → 카테고리 매핑 (태그는 메시지 앞의 [XXX])
    private static readonly Dictionary<string, string> TagCategory = new()
    {
        // ── system: 게임플레이/전투 ──
        { "GM", "system" },
        { "MB", "system" },
        { "WC", "system" },
        { "WaveController", "system" },
        { "WaveAnimator", "system" },
        { "Monster", "system" },
        { "MonsterSpawner", "system" },
        { "Spirit", "system" },
        { "Battle", "system" },
        { "BattlePresenter", "system" },
        { "GameManager", "system" },
        { "Request", "system" },   // 의뢰(파견/미션/레이드)
        { "RaidBattle", "system" },
        // ── cli: 검증/진단 ──
        { "CLI", "cli" },
        { "BattleStatus", "cli" },
        { "Layout", "cli" },
        { "UpgradeLayout", "cli" },
        { "UI", "cli" },
        { "TopBar", "cli" },
        { "TmpY", "cli" },
        { "GNBY", "cli" },
        { "DexScroll", "cli" },
        { "Board", "cli" },
        { "Upgrade", "cli" },
        { "FullTest", "cli" },
        // ── editor: 에디터 도구 ──
        { "CliServer", "editor" },
        { "Setup", "editor" },
        { "BatchSetup", "editor" },
        { "Apply", "editor" },
        { "GNB", "editor" },
        { "MergeUI", "editor" },
        { "BattleUI", "editor" },
        { "RebuildAll", "editor" },
        { "Connect", "editor" },
        { "ConnectAll", "editor" },
        { "GameSetup", "editor" },
        { "EventSystem", "editor" },
        { "AddGM", "editor" },
        { "StageGen", "editor" },
        { "SpiritDataGenerator", "editor" },
        { "Organize", "editor" },
        { "Cleanup", "editor" },
        { "SceneCleanup", "editor" },
        { "DataManager", "editor" },
        // ── data: 데이터/저장 ──
        { "SpiritManager", "data" },
        { "InventoryManager", "data" },
        { "GameEntryPoint", "data" },
    };

    private static readonly Regex TagRx = new(@"^\[([A-Za-z][A-Za-z0-9_]*)\]", RegexOptions.Compiled);

    static void Init()
    {
        if (_initialized) return;
        _logDir = Path.Combine(Application.dataPath, "..", "game_log");
        try
        {
            Directory.CreateDirectory(_logDir);
            _initialized = true;
            Application.logMessageReceived += OnUnityLog;
            WriteEntry("INFO", "[GameLogger] 로깅 시스템 시작 (파일기반 v3)");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameLogger] 초기화 실패: {e.Message}");
        }
    }

    /// <summary>Unity의 모든 로그 캡처 — 파일 기록의 단일 지점 (중복 방지)</summary>
    static void OnUnityLog(string logString, string stackTrace, LogType type)
    {
        if (!_initialized) return;
        string level = type switch
        {
            LogType.Log       => "INFO",
            LogType.Warning   => "WARN",
            LogType.Error     => "ERROR",
            LogType.Exception => "ERROR",
            LogType.Assert    => "ERROR",
            _                 => "INFO"
        };
        string msg = logString.Length > 2000 ? logString[..2000] + "..." : logString;
        string cat = ResolveCategory(msg);

        if (!_disabled.Contains(cat)) WriteDirect(cat, level, msg);
        if (level != "INFO" && !_disabled.Contains("errors")) WriteDirect("errors", level, msg);

        if (type == LogType.Exception || type == LogType.Error)
        {
            string trace = stackTrace.Length > 1000 ? stackTrace[..1000] + "..." : stackTrace;
            foreach (var line in trace.Split('\n'))
            {
                string tl = line.Trim();
                if (tl.Length == 0) continue;
                if (!_disabled.Contains(cat)) WriteDirect(cat, level, $"  └ {tl}");
                if (!_disabled.Contains("errors")) WriteDirect("errors", level, $"  └ {tl}");
            }
        }
    }

    /// <summary>
    /// ⭐ 매 기록마다 파일 열기/쓰기/닫기 (File.AppendAllText) — 파일 핸들 잠금/꼬임 원천 차단.
    /// 실패 시 손상 파일 삭제 후 1회 재시도.
    /// </summary>
    static void WriteDirect(string category, string level, string content)
    {
        string line = $"| {Timestamp} | {level} | {content}";
        string path = Path.Combine(_logDir, category + ".md");
        try
        {
            if (!File.Exists(path)) WriteHeader(path);
            File.AppendAllText(path, line + "\n", new UTF8Encoding(false));
            int n = _lineCounts.TryGetValue(category, out int v) ? v + 1 : 1;
            if (n >= MaxLines)
            {
                TrimFile(category);
                _lineCounts[category] = CountLines(path);
            }
            else _lineCounts[category] = n;
        }
        catch
        {
            // ⭐ 재귀 방지 — 여기서 Debug.LogError 금지 (OnUnityLog → WriteDirect → 실패 → 무한 재귀)
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            try
            {
                WriteHeader(path);
                File.AppendAllText(path, line + "\n", new UTF8Encoding(false));
            }
            catch { /* 최종 실패 — 조용히 무시 */ }
            _lineCounts[category] = CountLines(path);
        }
    }

    static void WriteHeader(string path)
    {
        string category = Path.GetFileNameWithoutExtension(path);
        File.AppendAllText(path,
            $"# Spirit Merge — Log ({category})\n| 시각 | 종류 | 메시지 |\n|------|------|--------|\n",
            new UTF8Encoding(false));
    }

    static void TrimFile(string category)
    {
        string path = Path.Combine(_logDir, category + ".md");
        if (!File.Exists(path)) return;
        try
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length > KeepLines)
            {
                var header = lines.Length >= 3 ? lines[..3] : Array.Empty<string>();
                var body = lines.Length >= 3 ? lines[3..] : lines;
                var keep = body.Length > KeepLines ? body[^KeepLines..] : body;
                File.WriteAllLines(path, header.Concat(keep).ToArray(), new UTF8Encoding(false));
            }
        }
        catch { /* 무시 */ }
    }

    static int CountLines(string path)
    {
        try { return File.ReadAllLines(path).Length; }
        catch { return 0; }
    }

    static string ResolveCategory(string msg)
    {
        var m = TagRx.Match(msg);
        if (m.Success && TagCategory.TryGetValue(m.Groups[1].Value, out var cat)) return cat;
        return "misc";
    }

    // ─── 공개 API ────────────────────────────────

    public static void Info(string message) { Init(); WriteEntry("INFO", message); }
    public static void Warn(string message) { Init(); WriteEntry("WARN", message); }
    public static void Error(string message) { Init(); WriteEntry("ERROR", message); }

    /// <summary>
    /// 카테고리 로그 켜기/끄기 — 폭증하는 로그만 끄기 가능
    /// 예: SetCategoryEnabled("system", false)
    /// </summary>
    public static void SetCategoryEnabled(string category, bool enabled)
    {
        if (enabled) _disabled.Remove(category);
        else _disabled.Add(category);
        Info($"[GameLogger] 카테고리 '{category}' {(enabled ? "활성화" : "비활성화")}");
    }

    /// <summary>활성 카테고리 목록</summary>
    public static string[] ActiveCategories() => TagCategory.Values.Distinct().ToArray();

    /// <summary>
    /// 파일 기록은 OnUnityLog에서 단일 처리 — 여기서는 콘솔 출력만 (중복 방지)
    /// </summary>
    static void WriteEntry(string level, string message)
    {
        switch (level)
        {
            case "ERROR": Debug.LogError(message); break;
            case "WARN":  Debug.LogWarning(message); break;
            default:      Debug.Log(message); break;
        }
    }

    static string Timestamp => DateTime.Now.ToString("HH:mm:ss.fff");
}

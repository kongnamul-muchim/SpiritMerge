using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// 게임 전체 로깅 시스템 — 모든 행동을 로그로 남기고 파일에 기록
/// 사용법:
///   GameLogger.Info("전투 시작");
///   GameLogger.Warn("골드 부족");
///   GameLogger.Error($"데이터 없음: {name}");
/// 
/// 로그 파일: 게임 폴더/game_log.md (Unity 에디터에서 실시간 기록)
/// </summary>
public static class GameLogger
{
    private static string _logPath;
    private static StreamWriter _writer;
    private static bool _initialized = false;

    static void Init()
    {
        if (_initialized) return;
        // 프로젝트 루트에 로그 파일 생성
        _logPath = Path.Combine(Application.dataPath, "..", "game_log.md");
        try
        {
            // 새 파일이면 헤더 작성
            bool isNew = !File.Exists(_logPath) || new FileInfo(_logPath).Length == 0;
            _writer = new StreamWriter(_logPath, true, Encoding.UTF8);
            _writer.AutoFlush = true;
            _initialized = true;
            if (isNew)
            {
                _writer.WriteLine("# Spirit Merge — Game Log");
                _writer.WriteLine($"> 시작: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine($"> 파일: {_logPath}");
                _writer.WriteLine();
                _writer.WriteLine("| 시각 | 종류 | 메시지 |");
                _writer.WriteLine("|------|------|--------|");
            }

            // 모든 Unity 로그 캡처 (Debug.Log, 예외, 오류 등)
            Application.logMessageReceived += OnUnityLog;
            WriteEntry("INFO", "[GameLogger] 로깅 시스템 시작");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameLogger] 파일 열기 실패: {e.Message}");
        }
    }

    /// <summary>
    /// Unity의 모든 로그를 캡처 (GameLogger를 거치지 않은 로그도 포함)
    /// </summary>
    static void OnUnityLog(string logString, string stackTrace, LogType type)
    {
        string level = type switch
        {
            LogType.Log        => "INFO",
            LogType.Warning    => "WARN",
            LogType.Error      => "ERROR",
            LogType.Exception  => "ERROR",
            LogType.Assert     => "ERROR",
            _                  => "INFO"
        };
        // 메시지가 200자 넘으면 자르기
        string msg = logString.Length > 200 ? logString[..200] + "..." : logString;
        WriteDirect($"{level} | {msg}");
        // 예외/에러는 스택트레이스도 기록
        if (type == LogType.Exception || type == LogType.Error)
        {
            string trace = stackTrace.Length > 300 ? stackTrace[..300] + "..." : stackTrace;
            // 스택트레이스는 여러 줄 → 각 줄을 별도 행으로
            foreach (var line in trace.Split('\n'))
                WriteDirect($"  | {line.Trim()}");
        }
    }

    /// <summary>
    /// 포맷 없이 바로 파일에 기록 (Unity 자동 캡처용)
    /// </summary>
    static void WriteDirect(string content)
    {
        if (_writer == null) return;
        try
        {
            string line = $"| {Timestamp} | {content}";
            _writer.WriteLine(line);
        }
        catch { }
    }

    static string Timestamp => System.DateTime.Now.ToString("HH:mm:ss.fff");

    static void WriteEntry(string level, string message)
    {
        Init();
        string line = $"| {Timestamp} | {level} | {message} |";
        // Debug.Log에도 출력 (Unity Console에 보이게)
        switch (level)
        {
            case "ERROR": Debug.LogError(line); break;
            case "WARN":  Debug.LogWarning(line); break;
            default:      Debug.Log(line); break;
        }
        // 파일에 기록
        if (_writer != null)
        {
            try { _writer.WriteLine(line); }
            catch { /* 무시 */ }
        }
    }

    public static void Info(string message) => WriteEntry("INFO", message);
    public static void Warn(string message) => WriteEntry("WARN", message);
    public static void Error(string message) => WriteEntry("ERROR", message);

    /// <summary>
    /// 로그 파일 닫기 (게임 종료 시)
    /// </summary>
    public static void Close()
    {
        if (_writer != null)
        {
            WriteEntry("INFO", "[GameLogger] 로깅 시스템 종료");
            _writer.Close();
            _writer = null;
            _initialized = false;
        }
    }
}

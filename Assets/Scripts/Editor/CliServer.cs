using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace SpiritMerge.Editor
{
    /// <summary>
    /// TCP CLI 서버 (포트 5555) — Unity 에디터 제어 + 태그별 로그 조회
    ///
    /// v3 변경점 (좀비 스레드 문제 수정):
    ///   - _listener를 static으로 승격 → Restart/TryStart에서 listener.Stop()으로
    ///     AcceptTcpClient() 차단을 깨워 스레드를 확실히 종료시킬 수 있음
    ///   - TryStart에서 좀비 감지: 스레드는 살아있는데 포트에 응답이 없으면
    ///     강제 정리 후 재시작 (기존엔 "Already running"만 반복하며 영영 안 뜸)
    ///   - _serverThread 정리는 스레드 종료 지점(루프 밖)에서만 수행
    /// </summary>
    [InitializeOnLoad]
    public static class CliServer
    {
        private const int Port = 5555;
        private static Thread _serverThread;
        private static TcpListener _listener;
        private static readonly Queue<Action> MainThreadQueue = new Queue<Action>();
        private static volatile bool _running;
        private static int _retryCount;

        static CliServer()
        {
            try
            {
                Debug.Log("[CliServer] Initializing...");
                // ⭐ 배치 모드(GUI 없음)에서는 서버를 띄우지 않음 — 배치는 컴파일 검증 전용
                //    배치 Unity가 5555를 점유하면 GUI 세션과 충돌/혼선 발생
                if (Application.isBatchMode)
                {
                    Debug.Log("[CliServer] Batch mode — server disabled (GUI 전용)");
                    return;
                }
                TryStart();
                EditorApplication.update += OnEditorUpdate;
            }
            catch (Exception e)
            {
                Debug.LogError($"[CliServer] Init error: {e.Message}");
            }
        }

        private static void TryStart()
        {
            // 살아있는 스레드가 실제로 서비스 중인지 확인
            if (_serverThread != null && _serverThread.IsAlive)
            {
                if (IsPortInUse())
                {
                    Debug.Log("[CliServer] Already running");
                    return;
                }
                // 스레드는 살아있지만 포트에 응답 없음 → 좀비. listener 중지로 스레드 종료 유도
                Debug.LogWarning("[CliServer] Zombie server thread — forcing cleanup");
                try { _listener?.Stop(); } catch { }
                try { _serverThread.Join(2000); } catch { }
                _serverThread = null;
            }

            if (_running) return;
            if (IsPortInUse())
            {
                Debug.Log("[CliServer] Port already in use by other process");
                return;
            }

            _running = true;
            _retryCount = 0;
            _serverThread = new Thread(ServerLoop) { IsBackground = true, Name = "CliServer" };
            _serverThread.Start();
        }

        private static bool IsPortInUse()
        {
            try
            {
                using var test = new TcpClient();
                test.Connect(IPAddress.Loopback, Port);
                test.ReceiveTimeout = 2000;
                test.SendTimeout = 2000;
                try
                {
                    using var stream = test.GetStream();
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                    using var reader = new StreamReader(stream, new UTF8Encoding(false));
                    writer.Write("ping\n");
                    writer.Flush();
                    var response = reader.ReadLine();
                    return response == "pong";
                }
                catch { return false; }
            }
            catch { return false; }
        }

        public static void Restart()
        {
            Debug.Log("[CliServer] Restart requested");
            _running = false;
            try { _listener?.Stop(); } catch { }        // accept 차단 해제
            try { _serverThread?.Join(1500); } catch { }
            _retryCount = 0;
            TryStart();
        }

        private static void OnEditorUpdate()
        {
            lock (MainThreadQueue)
            {
                while (MainThreadQueue.Count > 0)
                    MainThreadQueue.Dequeue()?.Invoke();
            }

            if (!_running && _retryCount < 10)
            {
                _retryCount++;
                TryStart();
            }
        }

        private static void ServerLoop()
        {
            while (_running)
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Loopback, Port);
                    _listener.ExclusiveAddressUse = false;
                    _listener.Start();
                    Debug.Log($"[CliServer] Listening on {Port}");

                    while (_running)
                    {
                        TcpClient client = null;
                        try
                        {
                            client = _listener.AcceptTcpClient();
                            using var stream = client.GetStream();
                            using var reader = new StreamReader(stream, Encoding.UTF8);
                            using var writer = new StreamWriter(stream, new UTF8Encoding(false));

                            string line = reader.ReadLine();
                            if (string.IsNullOrEmpty(line)) continue;

                            writer.Write(HandleCommand(line.Trim()));
                            writer.Flush();
                        }
                        catch (Exception e)
                        {
                            // 종료 유도 중(수신 중지)이면 조용히 무시
                            if (_running) Debug.LogWarning($"[CliServer] {e.Message}");
                        }
                        finally
                        {
                            if (client != null) try { client.Close(); } catch { }
                        }
                    }
                }
                catch (SocketException se)
                {
                    // 포트 바인딩 실패 — TIME_WAIT 등 예상 가능한 상황
                    if (se.SocketErrorCode == SocketError.AddressAlreadyInUse
                        || se.SocketErrorCode == SocketError.AccessDenied)
                    {
                        Debug.Log($"[CliServer] Port {Port} busy, retrying in 5s... ({se.SocketErrorCode})");
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        Debug.LogWarning($"[CliServer] Socket: {se.Message}");
                        Thread.Sleep(3000);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[CliServer] Recover: {e.Message}");
                }
                finally
                {
                    try { _listener?.Stop(); } catch { }
                    _listener = null;
                }

                if (_running) Thread.Sleep(3000);
            }

            // 스레드 종료 시점에만 상태 정리
            _running = false;
            _serverThread = null;
            Debug.Log("[CliServer] Cleanup");
        }

        private static string HandleCommand(string cmd)
        {
            try
            {
                switch (cmd)
                {
                    case "ping": return "pong\n";
                    case "quit": MainThreadQueue.Enqueue(() => _running = false); return "ok: quit\n";
                    case "restart": MainThreadQueue.Enqueue(Restart); return "ok: restart\n";
                    case "refresh": MainThreadQueue.Enqueue(AssetDatabase.Refresh); return "ok: refresh\n";
                    case "errors": return GetCompileErrors();
                    case "play": MainThreadQueue.Enqueue(() => EditorApplication.EnterPlaymode()); return "ok: play\n";
                    case "stop": MainThreadQueue.Enqueue(() => EditorApplication.ExitPlaymode()); return "ok: stop\n";
                    case "quit-unity": MainThreadQueue.Enqueue(() => EditorApplication.Exit(0)); return "ok: quit-unity\n";
                    case "build-webgl": MainThreadQueue.Enqueue(BuildWebGL); return "ok: build\n";
                    case "build-android": MainThreadQueue.Enqueue(BuildAndroid); return "ok: build\n";
                }

                if (cmd.StartsWith("cat "))
                {
                    // cat <카테고리> [줄수] — 태그별 로그 마지막 N줄 조회
                    var parts = cmd.Substring(4).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string category = parts.Length > 0 ? parts[0] : "errors";
                    int n = 40;
                    if (parts.Length > 1 && int.TryParse(parts[1], out int k)) n = k;
                    return GetLogTail(category, n);
                }
                if (cmd == "tags")
                {
                    return GetLogTags();
                }

                if (cmd.StartsWith("method:"))
                {
                    string m = cmd.Substring(7).Trim();
                    MainThreadQueue.Enqueue(() =>
                    {
                        Debug.Log($"[CliServer] Exec: {m}");
                        int dot = m.LastIndexOf('.');
                        if (dot < 0) { Debug.LogWarning($"[CliServer] Invalid: {m}"); return; }
                        string typeName = m.Substring(0, dot);
                        string methodName = m.Substring(dot + 1);

                        // Search all loaded assemblies for the type
                        System.Type type = null;
                        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = asm.GetType(typeName);
                            if (type != null) break;
                        }

                        if (type == null)
                        {
                            Debug.LogWarning($"[CliServer] Type not found: {typeName}");
                            return;
                        }
                        type.GetMethod(methodName,
                            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                            ?.Invoke(null, null);
                    });
                    return $"ok: {m}\n";
                }

                return $"unknown: {cmd}\n";
            }
            catch (Exception e) { return $"error: {e.Message}\n"; }
        }

        private static string GetCompileErrors()
        {
            var path = Path.Combine(Application.dataPath, "../Logs/Editor.log");
            if (!File.Exists(path)) return "ok: no log\n";
            try
            {
                var errors = new List<string>();
                // ⭐ 대용량 로그 대응: 파일 전체가 아닌 마지막 200KB만 읽어 에러 검색
                using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long len = fs.Length;
                    long start = Math.Max(0, len - 200000);
                    fs.Seek(start, SeekOrigin.Begin);
                    using var reader = new StreamReader(fs, Encoding.UTF8);
                    string content = reader.ReadToEnd();
                    foreach (var line in content.Split('\n'))
                        if (line.Contains("error CS")
                            && (line.Contains("Assets/Scripts") || line.Contains("Assets\\Scripts")))
                            errors.Add(line);
                }
                return errors.Count == 0 ? "ok: no errors\n" : string.Join("\n", errors) + "\n";
            }
            catch { return "ok: log busy\n"; }
        }

        private static void BuildWebGL()
        {
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/WebGL",
                BuildTarget.WebGL, BuildOptions.None);
        }

        /// <summary>Android APK 빌드 — 포트폴리오/폰 테스트용 (타겟 자동 전환 + 결과 로그)</summary>
        private static void BuildAndroid()
        {
            try
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                    Debug.LogWarning("[CliServer] Android 타겟 전환 실패 (모듈/SDK 확인 필요)");

                System.IO.Directory.CreateDirectory("Builds/Android");
                var scenePaths = new System.Collections.Generic.List<string>();
                foreach (var s in EditorBuildSettings.scenes)
                    if (s.enabled) scenePaths.Add(s.path);

                var opts = new BuildPlayerOptions
                {
                    scenes = scenePaths.ToArray(),
                    locationPathName = "Builds/Android/SpiritMerge.apk",
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };
                var report = BuildPipeline.BuildPlayer(opts);
                Debug.Log($"[CliServer] Android build 결과: {report.summary.result} → {opts.locationPathName}");
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    Debug.LogError($"[CliServer] Android build 실패 (에러 {report.summary.totalErrors}개) — Editor 로그 확인");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CliServer] Android build 예외: {e.Message}");
            }
        }

        // ─── 태그별 로그 조회 ──────────────────────────────

        private static string GetLogDir() => Path.Combine(Application.dataPath, "..", "game_log");

        /// <summary>game_log/ 카테고리 목록 반환</summary>
        private static string GetLogTags()
        {
            var dir = GetLogDir();
            if (!Directory.Exists(dir)) return "ok: 로그 없음\n";
            var tags = Directory.GetFiles(dir, "*.md")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(t => !string.IsNullOrEmpty(t));
            return "ok: " + string.Join(", ", tags) + "\n";
        }

        /// <summary>특정 카테고리 로그의 마지막 N줄 반환 (파일 전체를 주지 않아 에이전트 컨텍스트 보호)</summary>
        private static string GetLogTail(string category, int n)
        {
            var path = Path.Combine(GetLogDir(), category + ".md");
            if (!File.Exists(path)) return $"ok: 카테고리 없음 '{category}'\n";
            try
            {
                // ⭐ Unity가 쓰는 중인 파일도 읽을 수 있게 FileShare.ReadWrite 사용
                List<string> lines;
                using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs, Encoding.UTF8))
                {
                    lines = new List<string>();
                    while (!reader.EndOfStream)
                        lines.Add(reader.ReadLine());
                }
                int take = Math.Min(Math.Max(n, 1), lines.Count);
                return string.Join("\n", lines.GetRange(lines.Count - take, take)) + "\n";
            }
            catch { return "ok: 로그 읽기 중\n"; }
        }

        [MenuItem("SpiritMerge/CLI/Restart Server")]
        private static void RestartMenu() => Restart();

        [MenuItem("SpiritMerge/CLI/Status")]
        private static void StatusMenu()
        {
            EditorUtility.DisplayDialog("CLI Server",
                _running ? "Port 5555: Running" : "Port 5555: Stopped", "OK");
        }
    }
}

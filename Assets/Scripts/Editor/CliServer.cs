using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace SpiritMerge.Editor
{
    [InitializeOnLoad]
    public static class CliServer
    {
        private const int Port = 5555;
        private static Thread _serverThread;
        private static readonly Queue<Action> MainThreadQueue = new Queue<Action>();
        private static bool _running;
        private static int _retryCount;

        static CliServer()
        {
            try
            {
                Debug.Log("[CliServer] Initializing...");
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
            // 이전 스레드가 아직 살아있으면 중복 실행 방지
            if (_serverThread != null && _serverThread.IsAlive)
            {
                Debug.Log("[CliServer] Already running, skipping duplicate");
                return;
            }
            // 포트가 이미 사용 중이면 건너뜀 (이전 인스턴스가 아직 살아있음)
            if (IsPortInUse())
            {
                Debug.Log("[CliServer] Port already in use, waiting for cleanup");
                return;
            }
            if (_running) return;
            _running = true;
            _serverThread = new Thread(ServerLoop) { IsBackground = true, Name = "CliServer" };
            _serverThread.Start();
        }

        private static bool IsPortInUse()
        {
            try
            {
                using var test = new TcpClient();
                test.Connect(IPAddress.Loopback, Port);
                test.ReceiveTimeout = 2000; // 2초 타임아웃
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
            catch
            {
                return false;
            }
        }

        public static void Restart()
        {
            _running = false;
            Thread.Sleep(500);
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
                TcpListener listener = null;
                try
                {
                    listener = new TcpListener(IPAddress.Loopback, Port);
                    listener.ExclusiveAddressUse = false;
                    // Note: ReuseAddress not set on Windows — conflicts with ExclusiveAddressUse=false
                    // We rely on the retry loop below when TIME_WAIT is active
                    listener.Start();

                    while (_running)
                    {
                        TcpClient client = null;
                        try
                        {
                            client = listener.AcceptTcpClient();
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
                            Debug.LogWarning($"[CliServer] {e.Message}");
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
                    if (listener != null) try { listener.Stop(); } catch { }
                    Debug.Log("[CliServer] Cleanup");
                    // 스레드 종료 시 상태 완전 초기화
                    _running = false;
                    _serverThread = null;
                }

                if (_running) Thread.Sleep(3000);
            }
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
                    case "build-webgl": MainThreadQueue.Enqueue(BuildWebGL); return "ok: build\n";
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
                foreach (var line in File.ReadLines(path))
                    if (line.Contains("error CS") && line.Contains("Assets/Scripts"))
                        errors.Add(line);
                return errors.Count == 0 ? "ok: no errors\n" : string.Join("\n", errors) + "\n";
            }
            catch { return "ok: log busy\n"; }
        }

        private static void BuildWebGL()
        {
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/WebGL",
                BuildTarget.WebGL, BuildOptions.None);
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

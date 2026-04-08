using System;
using System.IO;
using UnityEngine;

public static class RuntimeLogCapture
{
    private static readonly object Sync = new object();
    private static string _logPath;
    private static bool _initialized;
    private static StreamWriter _writer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        var directory = GetLogDirectory();
        Directory.CreateDirectory(directory);
        _logPath = Path.Combine(directory, "runtime-player-log.txt");

        _writer = new StreamWriter(_logPath, true) { AutoFlush = true };
        _writer.WriteLine($"\n===== Session {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");

        Application.logMessageReceived += HandleLog;
        Application.logMessageReceivedThreaded += HandleLog;
        Debug.Log($"Runtime log enabled: {_logPath}");
    }

    private static string GetLogDirectory()
    {
#if UNITY_STANDALONE && !UNITY_EDITOR
        // AppDomain.CurrentDomain.BaseDirectory points to the folder containing the .exe.
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
#else
        // Keep editor and non-standalone targets writing to a guaranteed writable path.
        return Path.Combine(Application.persistentDataPath, "Logs");
#endif
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        try
        {
            lock (Sync)
            {
                if (_writer == null) return;
                _writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{type}] {condition}");
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    _writer.WriteLine(stackTrace);
                }
                _writer.WriteLine();
            }
        }
        catch
        {
            // Avoid recursive logging if writing fails.
        }
    }
}

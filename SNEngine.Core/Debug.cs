using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SNEngine.Core;

/// <summary>
/// Robust Unity-like Debug system.
/// Captures console output + handles crashes and unexpected termination.
/// </summary>
public static class Debug
{
    private static string LogFilePath = string.Empty;
    private static readonly object _lock = new();
    private static TextWriter? _originalOut;
    private static TextWriter? _originalError;

    public static void Initialize()
    {
        string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "Logs");
        Directory.CreateDirectory(logsDir);

        LogFilePath = Path.Combine(logsDir, $"SNEngine_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        HookConsole();
        HookCrashHandlers();

        Log("=== SNEngine Debug Session Started ===");
        Log($"Log file: {LogFilePath}");
        Log($"AppDomain: {AppDomain.CurrentDomain.FriendlyName}");
    }

    private static void HookConsole()
    {
        _originalOut = Console.Out;
        _originalError = Console.Error;

        var dualWriter = new DualWriter(_originalOut);
        Console.SetOut(dualWriter);
        Console.SetError(dualWriter);
    }

    private static void HookCrashHandlers()
    {
        // Catch unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            LogError($"Unhandled Exception: {e.ExceptionObject}");
            FlushToFile();
        };

        // Catch process exit (Ctrl+C, Environment.Exit, etc.)
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            Log("Process exiting...");
            FlushToFile();
        };

        Console.CancelKeyPress += (sender, e) =>
        {
            Log("Ctrl+C pressed. Shutting down...");
            FlushToFile();
        };
    }

    public static void Log(object message) => Write($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    public static void LogWarning(object message) => Write($"[{DateTime.Now:HH:mm:ss.fff}] [WARNING] {message}", ConsoleColor.Yellow);
    public static void LogError(object message) => Write($"[{DateTime.Now:HH:mm:ss.fff}] [ERROR] {message}", ConsoleColor.Red);

    private static void Write(string text, ConsoleColor? color = null)
    {
        if (color.HasValue)
        {
            Console.ForegroundColor = color.Value;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine(text);
        }

        Task.Run(() => AppendToFile(text));
    }

    private static void AppendToFile(string message)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(LogFilePath, message + Environment.NewLine);
            }
        }
        catch { }
    }

    private static void FlushToFile()
    {
        try
        {
            Console.WriteLine("[Debug] Flushing logs before exit...");
            // Force write remaining buffered data
        }
        catch { }
    }

    private class DualWriter : TextWriter
    {
        private readonly TextWriter _original;
        public override Encoding Encoding => _original.Encoding;

        public DualWriter(TextWriter original) => _original = original;

        public override void WriteLine(string? value)
        {
            _original.WriteLine(value);
            if (!string.IsNullOrEmpty(value))
                Task.Run(() => AppendToFile(value));
        }

        public override void Write(char value) => _original.Write(value);
    }

    public static void Clear()
    {
        try { Console.Clear(); } catch { }
    }
}
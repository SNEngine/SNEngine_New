using System;
using System.IO;
using System.Text;

namespace SNEngine.Scripting.CodeGen;

/// <summary>
/// Unified logger that captures ALL Console output and writes it to build.log
/// </summary>
public sealed class BuildLogger : IDisposable
{
    private readonly string _logPath;
    private readonly StreamWriter? _fileWriter;
    private readonly TextWriter _originalConsoleOut;

    public BuildLogger(string outputDirectory)
    {
        _logPath = Path.Combine(outputDirectory, "build.log");
        Directory.CreateDirectory(outputDirectory);

        _originalConsoleOut = Console.Out;

        try
        {
            _fileWriter = new StreamWriter(_logPath, append: false, Encoding.UTF8) { AutoFlush = true };

            // Хукаем консоль
            var dualWriter = new DualWriter(_originalConsoleOut, _fileWriter);
            Console.SetOut(dualWriter);

            Log("=== BUILD STARTED ===");
            Log($"Log file: {_logPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARNING] Failed to initialize BuildLogger: {ex.Message}");
        }
    }

    public void Log(string message)
    {
        Console.WriteLine(message); // будет записано и в файл благодаря DualWriter
    }

    public void LogError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] {message}");
        Console.ResetColor();
    }

    public void LogSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] {message}");
        Console.ResetColor();
    }

    public void Dispose()
    {
        Log("=== BUILD FINISHED ===");

        // Возвращаем оригинальный Console.Out
        if (_originalConsoleOut != null)
            Console.SetOut(_originalConsoleOut);

        _fileWriter?.Dispose();
    }

    // Внутренний класс для записи в два места одновременно
    private class DualWriter : TextWriter
    {
        private readonly TextWriter _console;
        private readonly TextWriter _file;

        public DualWriter(TextWriter console, TextWriter file)
        {
            _console = console;
            _file = file;
        }

        public override Encoding Encoding => _console.Encoding;

        public override void Write(char value)
        {
            _console.Write(value);
            _file.Write(value);
        }

        public override void Write(string? value)
        {
            _console.Write(value);
            _file.Write(value);
        }

        public override void WriteLine(string? value)
        {
            _console.WriteLine(value);
            _file.WriteLine(value);
        }
    }
}
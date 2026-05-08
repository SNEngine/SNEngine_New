using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SNEngine.Scripting.CodeGen;

public class GameAssemblyBuilder : IDisposable
{
    private readonly ScriptCodeGenerator _generator = new();
    private BuildLogger? _logger;

    /// <summary>
    /// Callback для вывода сообщений (можно подключить GUI)
    /// </summary>
    public Action<string>? OnLog { get; set; }

    /// <summary>
    /// Callback для прогресса: (current, total)
    /// </summary>
    public Action<int, int>? OnProgress { get; set; }

    public GameAssemblyBuilder()
    {
        _generator.RegisterAll(typeof(GameAssemblyBuilder).Assembly);
    }

    public async Task<BuildResult> BuildAsync(string inputDirectory, string outputDllPath = "game.dll")
    {
        var stopwatch = Stopwatch.StartNew();
        string outputDir = Path.GetDirectoryName(outputDllPath) ?? inputDirectory;
        _logger = new BuildLogger(outputDir);

        Log("=== BUILD STARTED ===");
        Log($"Input directory: {inputDirectory}");
        Log($"Output DLL: {outputDllPath}");

        if (!Directory.Exists(inputDirectory))
        {
            LogError($"Directory not found: {inputDirectory}");
            return new BuildResult(false, 0);
        }

        string genDirectory = Path.Combine(inputDirectory, "script_gen");
        EnsureDirectoryClean(genDirectory);

        var allCsFiles = new List<string>();

        var snFiles = Directory.GetFiles(inputDirectory, "*.sn", SearchOption.AllDirectories)
                               .OrderBy(f => f).ToArray();

        Log($"Found {snFiles.Length} .sn files");

        int processed = 0;
        int total = snFiles.Length;

        foreach (var snPath in snFiles)
        {
            processed++;
            OnProgress?.Invoke(processed, total);

            string fileName = Path.GetFileName(snPath);
            Log($"[{processed}/{total}] Processing {fileName}");

            try
            {
                string source = await File.ReadAllTextAsync(snPath);
                string csCode = SnToCsConverter.ConvertToCSharp(source, fileName);

                string tempCsPath = Path.Combine(genDirectory, Path.GetFileNameWithoutExtension(snPath) + ".generated.cs");
                await File.WriteAllTextAsync(tempCsPath, csCode);
                allCsFiles.Add(tempCsPath);

                LogSuccess($"Compiled {fileName}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to parse {fileName}: {ex.Message}");
                if (ex.InnerException != null)
                    LogError($"Inner: {ex.InnerException.Message}");
            }
        }

        var manualCsFiles = Directory.GetFiles(inputDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("script_gen") && !f.EndsWith(".generated.cs"))
            .ToList();

        allCsFiles.AddRange(manualCsFiles);

        Log($"Total files for compilation: {allCsFiles.Count}");

        if (allCsFiles.Count == 0)
        {
            LogError("No files to compile.");
            return new BuildResult(false, 0);
        }

        bool success = await CompileToDllAsync(allCsFiles, outputDllPath);

        stopwatch.Stop();
        double seconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);

        if (success)
        {
            LogSuccess($"BUILD SUCCEEDED in {seconds} seconds");
            await PrintDllTreeAsync(outputDllPath);
        }
        else
        {
            LogError("BUILD FAILED");
        }

        return new BuildResult(success, seconds);
    }

    private void Log(string message)
    {
        if (OnLog != null)
            OnLog(message);
        else
            _logger?.Log(message);
    }

    private void LogSuccess(string message)
    {
        if (OnLog != null)
            OnLog($"[✓] {message}");
        else
            _logger?.LogSuccess(message);
    }

    private void LogError(string message)
    {
        if (OnLog != null)
            OnLog($"[✗] {message}");
        else
            _logger?.LogError(message);
    }

    private async Task PrintDllTreeAsync(string dllPath)
    {
        if (!File.Exists(dllPath)) return;

        try
        {
            var assembly = Assembly.LoadFrom(dllPath);
            var types = assembly.GetTypes()
                .Where(t => t.Namespace?.StartsWith("SNEngine.Game") == true || t.BaseType?.Name == "SNScript")
                .OrderBy(t => t.Name)
                .ToList();

            Log("");
            Log("=== DLL STRUCTURE ===");
            Log($"Assembly: {assembly.GetName().Name}");
            Log($"Total script classes: {types.Count}");
            Log("");

            foreach (var type in types)
            {
                Log($"📦 {type.Name}");

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName)
                    .OrderBy(m => m.Name);

                foreach (var method in methods)
                {
                    string parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Log($"   └── {method.ReturnType.Name} {method.Name}({parameters})");
                }

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    Log($"   └── field: {field.FieldType.Name} {field.Name}");
                }

                Log("");
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to analyze DLL: {ex.Message}");
        }
    }

    private async Task<bool> CompileToDllAsync(List<string> csFiles, string outputDllPath)
    {
        var syntaxTrees = new List<SyntaxTree>();

        foreach (var path in csFiles)
        {
            string code = await File.ReadAllTextAsync(path);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(code, path: path));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "SNEngine.Game",
            syntaxTrees: syntaxTrees,
            references: GetFullReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu));

        using var dllStream = File.Create(outputDllPath);
        var result = compilation.Emit(dllStream);

        if (result.Success)
        {
            LogSuccess($"SUCCESS! Built {outputDllPath} ({syntaxTrees.Count} files)");
            return true;
        }
        else
        {
            LogError($"Build failed with {result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error)} errors");

            foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                var line = d.Location.GetLineSpan();
                LogError($"   [{line.Path}:{line.StartLinePosition.Line + 1}] {d.GetMessage()}");
            }

            if (File.Exists(outputDllPath)) File.Delete(outputDllPath);
            return false;
        }
    }

    private List<MetadataReference> GetFullReferences()
    {
        var refs = new List<MetadataReference>();
        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
            .Split(Path.PathSeparator);

        var neededAssemblies = new[]
        {
            "System.Runtime", "System.Collections", "System.Console", "System.Linq",
            "System.Private.CoreLib", "System.Runtime.InteropServices", "System.Reflection",
            "mscorlib", "netstandard"
        };

        foreach (var path in trustedAssembliesPaths)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            if (neededAssemblies.Contains(fileName))
            {
                refs.Add(MetadataReference.CreateFromFile(path));
            }
        }

        refs.Add(MetadataReference.CreateFromFile(typeof(SNEngine.API.SNScript).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(SNEngine.Core.Debug).Assembly.Location));

        return refs;
    }

    private static void EnsureDirectoryClean(string path)
    {
        if (Directory.Exists(path))
        {
            try { Directory.Delete(path, true); } catch { }
        }
        Directory.CreateDirectory(path);
    }

    public void Dispose()
    {
        _logger?.Dispose();
    }
}

public record BuildResult(bool Success, double Seconds);
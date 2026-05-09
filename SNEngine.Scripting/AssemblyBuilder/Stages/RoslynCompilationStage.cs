using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SNEngine.Scripting.AssemblyBuilder.Attributes;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SNEngine.Scripting.AssemblyBuilder.Stages;

/// <summary>
/// Compiles all C# files (generated + manual) into game.dll using Roslyn.
/// </summary>
[BuildStage(40, "Roslyn Compilation")]
public class RoslynCompilationStage : ISnBuildStage
{
    public string Name => "Roslyn Compilation";

    public async Task ExecuteAsync(BuildContext context)
    {
        // Add manual .cs files
        var manualCsFiles = Directory.GetFiles(context.InputDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("script_gen") && !f.EndsWith(".generated.cs"))
            .ToList();

        context.AllCsFiles.AddRange(context.GeneratedCsFiles);
        context.AllCsFiles.AddRange(manualCsFiles);

        context.Log.OnNext($"Compiling {context.AllCsFiles.Count} C# files...");

        if (context.AllCsFiles.Count == 0)
        {
            context.Log.OnNext("[Error] No files to compile");
            return;
        }

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var path in context.AllCsFiles)
        {
            string code = await File.ReadAllTextAsync(path);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(code, path: path));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "SNEngine.Game",
            syntaxTrees: syntaxTrees,
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithPlatform(Platform.AnyCpu));

        using var dllStream = File.Create(context.OutputDllPath);
        var result = compilation.Emit(dllStream);

        if (result.Success)
        {
            context.Log.OnNext($"[✓] Successfully built {context.OutputDllPath}");
        }
        else
        {
            context.Log.OnNext($"[✗] Compilation failed with {result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error)} errors");

            foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                var line = d.Location.GetLineSpan();
                context.Log.OnNext($"   [{line.Path}:{line.StartLinePosition.Line + 1}] {d.GetMessage()}");
            }

            if (File.Exists(context.OutputDllPath))
                File.Delete(context.OutputDllPath);
        }
    }

    /// <summary>
    /// Returns all required assembly references for compilation
    /// </summary>
    private List<MetadataReference> GetReferences()
    {
        var refs = new List<MetadataReference>();

        // Platform trusted assemblies
        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
            .Split(Path.PathSeparator);

        var neededAssemblies = new[]
        {
            "System.Runtime", "System.Collections", "System.Console", "System.Linq",
            "System.Private.CoreLib", "System.Runtime.InteropServices", "System.Reflection",
            "mscorlib", "netstandard", "System.Threading.Tasks"
        };

        foreach (var path in trustedAssembliesPaths)
        {
            if (string.IsNullOrEmpty(path)) continue;

            var fileName = Path.GetFileNameWithoutExtension(path);
            if (neededAssemblies.Contains(fileName))
            {
                refs.Add(MetadataReference.CreateFromFile(path));
            }
        }

        // SNEngine core references
        refs.Add(MetadataReference.CreateFromFile(typeof(SNEngine.API.SNScript).Assembly.Location));
        refs.Add(MetadataReference.CreateFromFile(typeof(SNEngine.Core.Debug).Assembly.Location));

        return refs;
    }
}
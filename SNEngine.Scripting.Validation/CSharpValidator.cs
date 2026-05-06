using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SNEngine.Scripting.Validation;

public static class CSharpValidator
{
    private static readonly Lazy<List<MetadataReference>> _references = new(CreateReferences);

    private static List<MetadataReference> CreateReferences()
    {
        var refs = new List<MetadataReference>();

        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(Path.PathSeparator);

        var neededAssemblies = new[]
        {
            "System.Runtime",
            "System.Collections",
            "System.Console",
            "System.Linq",
            "System.Private.CoreLib",
            "System.Runtime.InteropServices",
            "mscorlib"
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

    public static ValidationResult Validate(string csharpCode, string fileName = "Generated.cs")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);

        var compilation = CSharpCompilation.Create(
            assemblyName: "SNEngine.GeneratedTest",
            syntaxTrees: new[] { syntaxTree },
            references: _references.Value,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithPlatform(Platform.AnyCpu));

        var diagnostics = compilation.GetDiagnostics();

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors.Select(e => FormatDiagnostic(e, fileName)).ToList(),
            Warnings = warnings.Select(w => FormatDiagnostic(w, fileName)).ToList()
        };
    }

    private static string FormatDiagnostic(Diagnostic d, string fileName)
    {
        var span = d.Location.GetLineSpan().StartLinePosition;
        return $"[{fileName}:{span.Line + 1}:{span.Character + 1}] {d.GetMessage()}";
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public void PrintToConsole()
        {
            if (IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Generated C# code is valid and compilable!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Compilation failed with {Errors.Count} error(s):");
                foreach (var err in Errors.Take(15))
                    Console.WriteLine($"   {err}");
                if (Errors.Count > 15)
                    Console.WriteLine($"   ... and {Errors.Count - 15} more errors.");
            }

            if (Warnings.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️ {Warnings.Count} warnings");
            }

            Console.ResetColor();
        }
    }
}
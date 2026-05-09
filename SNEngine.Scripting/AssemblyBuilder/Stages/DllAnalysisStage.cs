using SNEngine.Scripting.AssemblyBuilder.Attributes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SNEngine.Scripting.AssemblyBuilder.Stages;

/// <summary>
/// Analyzes the built DLL and prints its structure (classes, methods, fields).
/// </summary>
[BuildStage(50, "DLL Structure Analysis")]
public class DllAnalysisStage : ISnBuildStage
{
    public string Name => "DLL Structure Analysis";

    public Task ExecuteAsync(BuildContext context)
    {
        if (!File.Exists(context.OutputDllPath))
            return Task.CompletedTask;

        try
        {
            var assembly = Assembly.LoadFrom(context.OutputDllPath);
            var types = assembly.GetTypes()
                .Where(t => t.Namespace?.StartsWith("SNEngine.Game") == true ||
                           t.BaseType?.Name == "SNScript")
                .OrderBy(t => t.Name)
                .ToList();

            context.Log.OnNext("");
            context.Log.OnNext("=== DLL STRUCTURE ===");
            context.Log.OnNext($"Assembly: {assembly.GetName().Name}");
            context.Log.OnNext($"Total script classes: {types.Count}");
            context.Log.OnNext("");

            foreach (var type in types)
            {
                context.Log.OnNext($"📦 {type.Name}");

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName)
                    .OrderBy(m => m.Name);

                foreach (var method in methods)
                {
                    string parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    context.Log.OnNext($"   └── {method.ReturnType.Name} {method.Name}({parameters})");
                }

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    context.Log.OnNext($"   └── field: {field.FieldType.Name} {field.Name}");
                }

                context.Log.OnNext("");
            }
        }
        catch (Exception ex)
        {
            context.Log.OnNext($"[Warning] Failed to analyze DLL: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
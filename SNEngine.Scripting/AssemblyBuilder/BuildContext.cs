using R3;
using System.Collections.Generic;
using System.IO;

namespace SNEngine.Scripting.AssemblyBuilder;

/// <summary>
/// Contains all data and services needed during the build process.
/// Acts as a shared context passed between build stages.
/// </summary>
public class BuildContext
{
    /// <summary>
    /// Source directory containing .sn and .cs files
    /// </summary>
    public string InputDirectory { get; }

    /// <summary>
    /// Full path to the output game.dll
    /// </summary>
    public string OutputDllPath { get; }

    /// <summary>
    /// Temporary directory for generated C# files
    /// </summary>
    public string GenDirectory { get; }

    // R3 Reactive channels
    public Subject<string> Log { get; init; } = new();
    public Subject<(int Current, int Total)> Progress { get; init; } = new();

    // Build data
    public List<string> SnFiles { get; set; } = new();
    public List<string> GeneratedCsFiles { get; set; } = new();
    public List<string> AllCsFiles { get; set; } = new();

    public BuildContext(string inputDirectory, string outputDllPath = "game.dll")
    {
        InputDirectory = inputDirectory;
        OutputDllPath = outputDllPath;
        GenDirectory = Path.Combine(inputDirectory, "script_gen");
    }
}
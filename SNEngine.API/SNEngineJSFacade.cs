using System;

namespace SNEngine.API;

/// <summary>
/// Public entry point for the build-time generated JavaScript facade.
/// 
/// The actual JS code is produced by the Roslyn generator (SNEngine.API.Generators)
/// at dotnet build time and embedded as a string constant.
/// 
/// This approach was chosen because:
/// - Generation is expensive → must happen at build time, not runtime.
/// - We do not want to put generated files inside assets/ (pollutes source + virtual FS).
/// - The JS lives "next to" the binary (inside the assembly), making it always available
///   regardless of whether ui.snpk is loaded or not.
/// </summary>
public static class SNEngineJSFacade
{
    /// <summary>
    /// The complete generated JavaScript code that defines the 'sn' / 'SNEngine' global object.
    /// 
    /// Contains nice short methods such as:
    ///   sn.ShowBackground(name)
    ///   sn.ShowCharacter(name, emotion)
    ///   etc.
    /// 
    /// This string is generated at build time from BackgroundAPI, CharacterAPI and other *API classes.
    /// </summary>
    public static string GeneratedCode => SNEngineJSBindings.GeneratedFacade;

    /// <summary>
    /// Returns the generated facade JS.
    /// </summary>
    public static string GetCode() => GeneratedCode;

    /// <summary>
    /// (Optional) Hook point for the runtime bridge to inject the facade into a View.
    /// Implementation will be added when the JS bridge is ready.
    /// </summary>
    internal static void InjectInto(object viewOrContext)
    {
        // Will be implemented when we have the real Ultralight injection point.
        // For now this is just a placeholder so the generator output is usable.
    }
}

using SNEngine.Core.Engine.Systems.DialogSystem;
using System;
using System.Diagnostics;

namespace SNEngine.Core.Engine;

/// <summary>
/// Responsible for collecting runtime data from core systems 
/// (FPS, Dialogue, etc.) and pushing it to all active UI elements.
/// </summary>
public class RuntimeDataPusher
{
    private readonly UI.UiManager? _uiManager;
    private readonly FrameProfiler? _profiler;
    private DialogueSystem? _dialogueSystem; // cached to avoid dictionary lookup every frame

    public RuntimeDataPusher(UI.UiManager? uiManager, FrameProfiler? profiler)
    {
        _uiManager = uiManager;
        _profiler = profiler;
    }

    /// <summary>
    /// Collects current runtime snapshot and pushes it to all visible UI elements.
    /// Should be called once per update frame.
    /// </summary>
    public void PushData()
    {
        if (_uiManager == null || _uiManager.Elements.Count == 0)
            return;

        if (_dialogueSystem == null)
        {
            _dialogueSystem = SNEngineHost.Current.GetSystem<DialogueSystem>();
        }

        var snapshot = new RuntimeSnapshot
        {
            Fps = _profiler?.NativeFps ?? 0.0,
            Dialogue = _dialogueSystem?.GetSnapshot() ?? default
        };

        foreach (var element in _uiManager.Elements)
        {
            if (!element.Visible) continue;

            try
            {
                element.ReceiveRuntimeData(in snapshot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RuntimeDataPusher] Error pushing data to UI element: {ex.Message}");
            }
        }
    }
}

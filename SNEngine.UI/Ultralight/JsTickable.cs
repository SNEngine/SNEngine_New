// SNEngine.UI.Ultralight/JsTickable.cs
using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Базовый класс для JS-хелперов, которые нужно обновлять каждый кадр (tick)
/// </summary>
public abstract class JsTickable
{
    protected readonly JsWindowHelper JsHelper;
    protected readonly View View;

    protected JsTickable(View view)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
        JsHelper = new JsWindowHelper(view);
    }

    /// <summary>
    /// Вызывается каждый кадр
    /// </summary>
    public abstract void Tick();

    /// <summary>
    /// Инициализация (один раз)
    /// </summary>
    public virtual void Initialize() { }
}
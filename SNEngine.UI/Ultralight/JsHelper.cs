// SNEngine.UI.Ultralight/JsUpdater.cs
using UltralightNet;

namespace SNEngine.UI.Ultralight;

/// <summary>
/// Базовый класс для JS-хелперов, которые нужно обновлять каждый кадр
/// </summary>
public abstract class JsUpdater
{
    protected readonly JsWindowHelper JsHelper;
    protected readonly View View;

    protected JsUpdater(View view)
    {
        View = view;
        JsHelper = new JsWindowHelper(view);
    }

    /// <summary>
    /// Вызывается каждый кадр
    /// </summary>
    public abstract void Update();

    /// <summary>
    /// Инициализация (один раз при создании View)
    /// </summary>
    public virtual void Initialize() { }
}
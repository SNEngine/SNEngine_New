# Архитектура поддержки нескольких HTML-элементов в SNEngine

## Текущая проблема
Сейчас `UltralightOverlay` поддерживает только **один** `View` на весь экран. Метод `LoadScreen()` заменяет всё содержимое. Это не позволяет показывать одновременно, например:
- HUD (здоровье, инвентарь)
- Диалоговое окно
- Полноэкранное меню
- Всплывающие подсказки и т.д.

## Основные варианты архитектуры

### Вариант 1: Один Renderer + много View (Рекомендуемый)

**Идея:**
- Оставляем **один** `Renderer` (это правильно по дизайну Ultralight).
- Создаём много `View` (по одному на каждый независимый HTML-элемент).
- Каждый `View` имеет свою позицию, размер, z-order и свою текстуру в TrippyGL.
- В `Render()` вызываем `renderer.Update()` и `renderer.Render()` **один раз**, а потом композим все видимые View.

**Плюсы:**
- Эффективно по CPU (один цикл обновления/рендеринга Ultralight).
- Хорошая изоляция контента и JavaScript между элементами.
- Можно делать маленькие View (экономия памяти и CPU).

**Минусы:**
- Нужно аккуратно управлять множеством текстур и их аплоадом.
- Нужно реализовать систему z-order и hit-testing для ввода.

**Предлагаемая структура:**

```csharp
// В SNEngine.UI
public class UltralightElement
{
    public View View { get; }
    public Texture2D? Texture { get; private set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public int ZIndex { get; set; }
    public bool Visible { get; set; } = true;

    public void LoadHtml(string html);
    public void LoadScreen(string screenName);
    // события и т.д.
}

public class UltralightUiSystem : IUiOverlay   // или отдельный менеджер
{
    private Renderer _renderer;
    private List<UltralightElement> _elements = new();

    public UltralightElement CreateElement(int width, int height, int zIndex = 0);
    public void RemoveElement(UltralightElement element);

    // В Render() — один Update/Render + отрисовка всех элементов по Z
}
```

### Вариант 2: Полностью отделить UI-систему от IUiOverlay (Более чистая архитектура)

**Идея:**
- `IUiOverlay` оставить как низкоуровневый "рендерер backend" (только инициализация контекста и финальная отрисовка).
- Ввести новый высокоуровневый `UiManager` / `UiSystem`, который:
  - Управляет несколькими `IUiElement`.
  - Каждый `IUiElement` может быть Ultralight-элементом, а в будущем — ImGui, или даже обычной текстурой.

**Плюсы:**
- Отличная расширяемость.
- Можно миксовать разные технологии UI.
- Чистое разделение ответственности.

**Минусы:**
- Больше работы по рефакторингу.
- Нужно менять `SNEngineHost` и публичный API.

### Вариант 3: Несколько независимых UltralightOverlay

Самый простой, но плохой вариант.

**Минусы:**
- Каждый оверлей создаёт свой `Renderer` → дорого.
- Сложно управлять порядком отрисовки и вводом.
- Проблемы с shared font loader / file system.

**Не рекомендуется**, кроме очень простых случаев.

### Вариант 4: Один большой View + внутреннее HTML-композитирование

Все "окна" рисуются внутри одного большого HTML-документа с помощью CSS `position: absolute`, `<div>` и т.д.

**Минусы для игры:**
- Сильная связанность.
- Сложно изолировать JavaScript разных частей.
- Плохо масштабируется при динамическом создании/удалении UI.

**Использовать только как fallback.**

## Рекомендуемая стратегия (поэтапная)

### Этап 1 (ближайший)
1. Переименовать/расширить `UltralightOverlay` → `UltralightUiSystem`.
2. Добавить поддержку `List<UltralightElement>`.
3. Каждый элемент имеет свою маленькую `Texture2D`.
4. В `Render()`:
   - `renderer.Update()` + `renderer.Render()` один раз.
   - Для каждого видимого элемента — аплоад поверхности → своя текстура → `Batcher.Draw` с позицией и z-order.
5. Добавить в публичный API:
   ```csharp
   SNEngine.Ui.CreateHtmlElement(width, height);
   element.LoadScreen("hud");
   element.SetPosition(...);
   element.ZIndex = 10;
   ```

### Этап 2
- Ввести интерфейс `IUiElement`.
- Сделать `UiManager`, который агрегирует все элементы (Ultralight + будущие).
- Перенести управление вводом (мышь/клавиатура) на уровень `UiManager`.

### Этап 3 (долгосрочно)
- Рассмотреть переход на `IsAccelerated = true` + custom `IGPUDriver`.
- Это позволит рендерить View напрямую в текстуры игры без копирования пикселей через CPU.

## Дополнительные важные вопросы

- **Input routing**: Как понять, какое View должно получить клик мыши?
- **Z-ordering**: Должен быть явный `ZIndex` + стабильная сортировка.
- **Performance**: Не стоит делать 20+ больших View одновременно. Для HUD-элементов лучше маленькие View или даже один View с несколькими "окнами" внутри.
- **Resize**: При изменении разрешения окна нужно корректно ресайзить все View.
- **AssetManager**: Каждый элемент должен иметь доступ к `AssetManager` для загрузки своих `index.html`.

## Вывод

**Лучший путь** — Вариант 1 с постепенным переходом к Варианту 2.

Это позволит относительно быстро получить возможность показывать несколько HTML одновременно, сохранив текущую производительность и не ломая сильно существующий код.

Хочешь, я начну набросок реализации `UltralightElement` + обновлённого `UltralightUiSystem`?

# SNEngine.Scripting

**Declarative scripting module** for the SNEngine visual novel engine (C# / .NET).

Write scenes in a simple `.sn` format and automatically generate clean, production-ready C# code.

---

## What is this?

`SNEngine.Scripting` is a **code generation system** that lets you write game scenes using a lightweight, human-readable scripting language instead of writing C# manually.

### Example `.sn` file:

```sn
name: testScene
Show Background class_bg
Show Character Nagatoro angry
Nagatoro says "Hello, Senpai~!"
end
```

### Generated C# (automatically):

```csharp
using SNEngine.API;
using SNEngine.Core.Scenes;

public class testScene : EmptyScene
{
    public override void OnLoad()
    {
        BackgroundAPI.Show("class_bg");
        CharacterAPI.Show("Nagatoro", "angry");
        CharacterAPI.Say("Nagatoro", "Hello, Senpai~!");
    }
}
```

---

## Why use it?

- **Fast prototyping** — write scenes in minutes, not hours
- **Clean architecture** — generated code inherits from `SNScript` / `EmptyScene`
- **Extensible** — easily add new commands (`Show Background`, `Show Character`, `says`, etc.)
- **No runtime interpretation** — everything compiles into `game.dll`
- **Perfect for visual novels** and story-driven games

---

## Architecture

| Layer              | Purpose                              | Key Classes                          |
|--------------------|--------------------------------------|--------------------------------------|
| **Parser**         | `.sn` → AST                          | `ScriptParser`, `CommandParserFactory` |
| **CodeGen**        | AST → C# source code                 | `ScriptCodeGenerator`, `ICommandCodeGenerator` |
| **Base Classes**   | Shared behavior for all scripts      | `SNScript`, `EmptyScene`             |
| **High-level API** | Easy conversion from files/strings   | `SnToCsConverter`                    |

---

## How to use

### From command line

```bash
SNEngine.Scripting.Utils.exe "path/to/scene.sn"
```

Generates `scene.cs` next to the original file.

### As a library

```csharp
string csharpCode = SnToCsConverter.ConvertToCSharp(snSource);

SnToCsConverter.ConvertFile("intro.sn", "IntroScene.cs");
```

---

## Adding a new command

1. Create `Ast/MyNewCommandNode.cs`
2. Implement `IParsableCommand` with a static `Parser`
3. Create `CodeGen/MyNewCommandCodeGenerator.cs` (implement `ICommandCodeGenerator`)
4. Add `[SnCommand("...")]` and `[SnCodeGenerator(...)]` attributes
5. Done — everything works automatically

---

## Current Status

- ✅ Basic commands (`Show Background`, `Show Character`, `says`)
- ✅ Clean code generation (inherits from `SNScript`)
- ✅ Automatic parser & generator registration
- ✅ CLI tool + library usage
- 🔄 Full scene system integration (in progress)
- 🔄 `OnLoad` / `OnUpdate` support

---

## Philosophy

> Write stories like a writer.  
> Generate code like a programmer.  
> Keep everything clean, typed, and fast.
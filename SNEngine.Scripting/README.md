# SNEngine.Scripting

**Declarative scripting module** for the SNEngine visual novel engine (C# / .NET).

Write scenes in a simple `.sn` format and automatically generate clean, production-ready C# code.

---

## What is this?

`SNEngine.Scripting` is a **code generation system** that lets you write game scenes using a lightweight, human-readable scripting language instead of writing C# manually.

### Example `scene2.sn`

```sn
name: scene2

playerHealth = 35
enemyLevel = 20

if playerHealth < 50 then
    Quit
else
    print "You are strong!"
endif

call ah()

function ah()
    if playerHealth < 50 then
        Quit
    else
        print "You are strong!"
    endif
endfunc
```

### Generated C# (automatically)

```csharp
public class scene2 : SNScript
{
    public override void Execute()
    {
        SetVar("playerHealth", 35);
        SetVar("enemyLevel", 20);

        if (GetVar("playerHealth").AsInt() < 50)
        {
            SNEngine.API.SNEngine.Quit();
        }
        else
        {
            Debug.Log("You are strong!");
        }

        ah();
    }

    private void ah()
    {
        if (GetVar("playerHealth").AsInt() < 50)
        {
            SNEngine.API.SNEngine.Quit();
        }
        else
        {
            Debug.Log("You are strong!");
        }
    }
}
```

---

## Features

- ✅ `if ... then ... else ... endif` (including nested)
- ✅ `function ... endfunc` + `call`
- ✅ `name: SceneName`
- ✅ Comments (`//` and `#`)
- ✅ Automatic command registration via `[SnCommand]` attribute
- ✅ Clean architecture with `StatementParser` as the single point of extension
- ✅ Multi-token Lexer (ready for expressions and better error reporting)

---

## Architecture

| Layer                   | Key Classes                          | Responsibility |
|-------------------------|--------------------------------------|----------------|
| **Lexer**               | `ScriptLexer`, `ScriptToken`         | Text → Tokens |
| **Reader**              | `TokenReader`                        | Navigation & reconstruction |
| **Core Parser**         | `ScriptParserCore`, `StatementParser`| Orchestration |
| **Special Parsers**     | `IfBlockParser`, `FunctionParser`    | Complex constructs |
| **Command Parser**      | Pidgin + `[SnCommand]`               | Simple commands |
| **Code Generation**     | `SnToCsConverter`                    | AST → C# |

---

## How to Use

### CLI

```bash
SNEngine.Scripting.Utils build "path/to/project" "output/game.dll"
```

### As Library

```csharp
var lexer = new ScriptLexer();
var tokens = lexer.Tokenize(snSource);

var parser = new ScriptParserCore(commandParser);
var ast = parser.Parse(tokens);

string csharpCode = new SnToCsConverter().Convert(ast);
```

---

## Adding a New Command

1. Create a class with `[SnCommand("mycommand")]`
2. Add parser logic (via `CommandParserFactory`)
3. Create a code generator
4. Done — it registers automatically

---

## Current Status (commit 2bf763ee...)

- ✅ Full support for `if/else`, `function`, `call`
- ✅ Stable multi-token Lexer
- ✅ Clean, extensible architecture
- ✅ Automatic keyword registration
- 🔄 Planning: `while`, `for`, full expressions in conditions, better error reporting

**SNEngine.Scripting** — the bridge between creative writing and professional C# development.
```
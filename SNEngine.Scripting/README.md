# SNEngine.Scripting

**Declarative scripting module** for the SNEngine visual novel engine (C# / .NET 8).

Write scenes in a simple `.sn` format and automatically generate clean, production-ready C# code.

---

## What is this?

`SNEngine.Scripting` is a **powerful code generation system** that lets you write game scenes using a lightweight, human-readable scripting language instead of writing raw C# manually.

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

switch playerChoice
switchcase 1
    print "First path chosen"
endcase
switchcase 2
    call secretEnding()
endcase
default
    print "Unknown choice"
endswitch

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
    private int playerHealth;
    private int enemyLevel;
    private int playerChoice;

    public override void Execute()
    {
        playerHealth = 35;
        enemyLevel = 20;

        if (playerHealth < 50)
        {
            SNEngine.API.SNEngine.Quit();
        }
        else
        {
            Debug.Log("You are strong!");
        }

        switch (playerChoice)
        {
            case 1:
                Debug.Log("First path chosen");
                break;
            case 2:
                secretEnding();
                break;
            default:
                Debug.Log("Unknown choice");
                break;
        }

        ah();
    }

    private void ah()
    {
        if (playerHealth < 50)
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

## Current Features

- ✅ `if ... then ... else ... endif` (including nested)
- ✅ `switch ... switchcase ... default ... endswitch`
- ✅ `for ... endfor`
- ✅ `function ... endfunc` + `call`
- ✅ `name: SceneName`
- ✅ Comments (`//` and `#`)
- ✅ Automatic generation of real C# variables (`int`, `double`, `string`, `bool`)
- ✅ Clean architecture based on AST + Roslyn Code Generation
- ✅ Automatic registration via attributes

---

## Architecture (2026)

| Layer                    | Key Classes                                              | Responsibility                  |
|--------------------------|----------------------------------------------------------|---------------------------------|
| **Lexer + Reader**       | `ScriptLexer`, `TokenReader`                             | Text → Tokens                   |
| **Core Parser**          | `ScriptParserCore`, `StatementParser`                    | Orchestration                   |
| **Block Parsers**        | `IfBlockParser`, `ForBlockParser`, `SwitchBlockParser`   | Complex constructs              |
| **AST Nodes**            | `IfCommandNode`, `ForCommandNode`, `SwitchCommandNode`   | Command tree                    |
| **Code Generators**      | `IfCodeGenerator`, `ForCodeGenerator`, `SwitchCodeGenerator` | AST → Roslyn C#             |
| **Registry**             | `CodeGeneratorRegistry`                                  | Automatic generator registration |

---

## How to Add a New Block Construct (e.g. `while` or custom loop)

We use a **two-stage approach** (exactly as we implemented `switch` and `for`):

### Stage 1: AST + Parser

1. Create AST node in `Ast/`:
   ```csharp
   public sealed class WhileCommandNode : CommandNode
   {
       public string Condition { get; }
       public List<CommandNode> Body { get; }
   }
   ```

2. Create parser in `Parsing/`:
   ```csharp
   public sealed class WhileBlockParser : BlockParserBase
   {
       public WhileCommandNode? Parse(TokenReader reader) { ... }
   }
   ```

3. Register it in `StatementParser.cs` and `ScriptParserCore.cs`

### Stage 2: Code Generator

1. Create generator in `CodeGen/`:
   ```csharp
   [SnCodeGenerator(typeof(WhileCommandNode))]
   public sealed class WhileCodeGenerator : ICommandCodeGenerator
   {
       public StatementSyntax Generate(CommandNode node) { ... }
   }
   ```

2. Done — the system will automatically pick it up via `CodeGeneratorRegistry`.

---

## How to Add a Simple Command (via attribute)

```csharp
[SnCommand("mycommand")]
public sealed class MyCommandNode : CommandNode, IParsableCommand
{
    // ...
}
```

---

## Current Status

- ✅ Full support for `if/else`, `switch`, `for`, `function`
- ✅ Generation of real C# variables (no more `SetVar`/`GetVar`)
- ✅ Roslyn-based code generation
- ✅ Clean and extensible architecture

**SNEngine.Scripting** — the bridge between creative writing and professional C# development.
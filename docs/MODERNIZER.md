# SpModernizer

Opt-in SourcePawn **dialect** rewriter: legacy/tag syntax → [Transitional Syntax](https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax).

SpFormatter stays a pure formatter (preserves old tags). SpModernizer is a sibling library in this repo.

## Authority sources

1. [SourcePawn Transitional Syntax](https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax)
2. [SourceMod 1.7.0 Release Notes](https://wiki.alliedmods.net/SourceMod_1.7.0_Release_Notes)
3. [Introduction to SourcePawn (legacy syntax)](https://wiki.alliedmods.net/Introduction_to_SourcePawn_(legacy_syntax))
4. [Introduction to SourcePawn 1.7](https://wiki.alliedmods.net/Introduction_to_SourcePawn_1.7)
5. [alliedmodders/sourcepawn README](https://github.com/alliedmodders/sourcepawn)
6. [BAILOPAN forum announcement](https://forums.alliedmods.net/showthread.php?t=244092)

No citation → no rule.

## Old → new (sourced)

| Old | New | Source |
|---|---|---|
| `new Float:x = 5.0;` | `float x = 5.0;` | Transitional Syntax New Declarators |
| `new y = 7;` | `int y = 7;` | same (`_:` / no tag → `int`) |
| `new String:name[32];` | `char name[32];` | same (`String:` → `char`) |
| `static const String:x[4][]` | `static const char x[4][]` | Arrays (no dims on both type and name) |
| `new x[MaxClients];` | `int[] x = new int[MaxClients];` | Arrays (non-constant size → dynamic) |
| `new x[MAXPLAYERS+1];` | `int x[MAXPLAYERS+1];` | Arrays (macro-like size stays fixed) |
| bare `MenuHandler(...)` | `int MenuHandler(...)` | menus.inc `MenuHandler` is `function int` (void only on SM 1.12+) |
| bare fn with valued `return` | `int` / `bool` / `Action` | inferred from return exprs (`Plugin_*` → Action) |
| `Float:array.Get(i)` | `view_as<float>(array.Get(i))` | View As |
| `functag public Action:SrvCmd(args);` | `typedef SrvCmd = function Action (int args);` | Typedefs |
| `funcenum Timer { ... };` | `typeset Timer { ... };` | Typedefs |
| `{Float,bool}:param` | *(no rewrite; diagnostic)* | 1.7 notes: multi-tag removed |

Builtin map (wiki): `Float`→`float`, `String`→`char`, `_`→`int`, `bool`→`bool`, `void`→`void`.

## API

```csharp
using var modernizer = new SourcePawnModernizer(new ModernizeOptions
{
    FormatAfter = false, // library default
});
var result = modernizer.ModernizeWithResult(source);
```

CLI defaults `FormatAfter = true` (`--no-format` to skip).

## Pipeline

Parse (`SourcePawnParser`) → rule edits → apply right-to-left → optional `SourcePawnFormatter.Format`.

## Rule catalog

| Id | Status | Citation |
|---|---|---|
| `old-type-cast` | Supported | View As |
| `old-builtins` | Supported (capability) | New Declarators |
| `old-types` | Supported | New Declarators |
| `old-variables` | Supported | New Declarators / Arrays |
| `tagged-signatures` | Supported | wiki Examples / BAILOPAN |
| `multi-tag` | Diagnostic only | 1.7 release notes |
| `functag` | Supported | Typedefs |
| `funcenum` | Supported | Typedefs |
| `old-struct-fields` | Supported (grammar) | struct field shape |
| `legacy-while` | Experimental | fork grammar legacy while |

## Non-goals

- Changing SpFormatter Format defaults or goldens
- Methodmap / Transitional API ports
- Inventing multi-tag replacements
- Web compile lab (parked)

See [MODERNIZER_ROADMAP.md](MODERNIZER_ROADMAP.md).

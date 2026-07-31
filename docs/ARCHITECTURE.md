# Architecture

## Goal

Valid SourcePawn source in, consistently formatted SourcePawn out.

Parse and print are separate. Invalid input fails closed unless `AllowSyntaxRecovery` is enabled.

## Current shape

```
source -> SourcePawnParser -> SourcePawnFormatter facade
                              -> AstPrinter (typed constructs)
                              -> LayoutRules (spacing / indent)
                              -> Legacy.UnknownNodePrinter (fallback)
                              -> Recovery.SyntaxRecovery (opt-in only)
```

`FormatWithResult` returns `FormatResult`. `Format` throws on failure for CLI/UI compatibility.

## Module responsibilities

| Module | Responsibility |
|---|---|
| `SourcePawnParser` | Tree-sitter only. Keep small. |
| `AstPrinter` | Emit from known node types. |
| `LayoutRules` | Spaces, newlines, indent from options. |
| `FormatResult` | Success text or structured errors. |
| `Legacy.UnknownNodePrinter` | Last-resort join for unowned nodes (mostly recovery). |
| `Recovery.SyntaxRecovery` | ERROR hacks / expression wrappers. Gated by `AllowSyntaxRecovery`. |

## What must not live in the printer

- New silent brace injection for bare `if`
- Silent top-level include/function reordering
- Regex spacing on clean AstPrinter paths
- New options without diverging true/false tests

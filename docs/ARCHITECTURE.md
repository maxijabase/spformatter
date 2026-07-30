# Architecture

## Goal

Valid SourcePawn source in, consistently formatted SourcePawn out.

Parse and print are separate. Invalid input should fail closed (report errors), not silently recover, unless Recovery is explicitly enabled later.

## Current shape (legacy)

```
source -> SourcePawnParser (Tree-sitter) -> SourcePawnFormatter (god class) -> text
```

Almost all behavior lives in `src/SpFormatter/SourcePawnFormatter.cs`:

- typed `FormatX` methods for some node types
- ERROR-node fallbacks and expression wrapping
- regex / string surgery for spacing
- top-level include/def reordering
- brace injection for bare `if` bodies

That mix is why small fixes break unrelated goldens.

## Target shape

```
source -> Parse -> typed print path -> LayoutRules(options) -> text
                 \-> FormatResult errors (fail closed)
```

| Module | Responsibility |
|---|---|
| `SourcePawnParser` | Tree-sitter only. Keep small. |
| `AstPrinter` / construct printers | Emit from known node types. |
| `LayoutRules` | Spaces, newlines, indent from options. One place for spacing policy. |
| `FormatResult` | Success text or structured errors. |
| `Recovery` (later, optional) | ERROR hacks / wrappers. Gated. Must not affect clean trees. |

`SourcePawnFormatter.Format` stays as the public facade so CLI/UI keep compiling while internals move.

## What must not live in the printer

- Grammar fixes that belong upstream in tree-sitter-sourcepawn
- Silent structure rewrites (brace wrap, top-level reorder) as defaults
- Duplicate spacing policy (AST path + regex path disagreeing)
- Dead options that UI exposes but the engine ignores

## Projects

| Project | Role |
|---|---|
| `src/SpFormatter` | Library: parse + format |
| `src/SpFormatter.Cli` | CLI |
| `src/SpFormatter.UI` | WPF playground |
| `tests/SpFormatter.Tests` | Unit / golden tests |

Solution file: `SpFormatter.slnx`

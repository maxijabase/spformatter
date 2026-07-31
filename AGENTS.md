# AGENTS.md

SourcePawn formatter for .NET. Parse with Tree-sitter, print with a pretty-printer.

## Read first

1. [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
2. [docs/STYLE.md](docs/STYLE.md)
3. [docs/SUPPORTED.md](docs/SUPPORTED.md)
4. [docs/AI_WORKFLOW.md](docs/AI_WORKFLOW.md)

## Current phase

Expressions, calls, variables, and simple functions/natives route through `AstPrinter` + `LayoutRules`. Next construct candidates: `if` without brace rewrite, then `for` / `while` / `switch`.

Misparsed `if`/`for`/call-as-function shapes still fall back to legacy recovery. Blocks, returns, and control structures remain Partial.

## Hard stops

- Do not "make the whole suite green" in one session.
- One language construct (or one printer layer) per session.
- Do not add regex / string post-processors outside a named Recovery module.
- Do not add `HasError` special-cases without classifying the case and updating `docs/SUPPORTED.md`.
- Do not invent new `FormattingOptions` until the option is implemented and has true/false tests that actually differ.
- Do not mass-update golden files just to silence failures.
- Do not brace-inject or reorder top-level declarations as silent defaults in new code.

## Build

```powershell
dotnet build SpFormatter.slnx
dotnet test SpFormatter.slnx
```

## Commits

- One commit per logical chunk.
- Subject starts with a lowercase letter.
- Short body explaining why.
- Plain human language. No em-dashes. No filler like "comprehensive", "robust", "seamless".

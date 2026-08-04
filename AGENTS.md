# AGENTS.md

SourcePawn formatter for .NET. Parse with Tree-sitter, print with a pretty-printer.

Sibling product **SpModernizer** rewrites legacy/tag syntax to Transitional Syntax. It is not part of Format. See [docs/MODERNIZER.md](docs/MODERNIZER.md) and [docs/MODERNIZER_ROADMAP.md](docs/MODERNIZER_ROADMAP.md).

## Read first

1. [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
2. [docs/STYLE.md](docs/STYLE.md)
3. [docs/SUPPORTED.md](docs/SUPPORTED.md)
4. [docs/AI_WORKFLOW.md](docs/AI_WORKFLOW.md)
5. [docs/ROADMAP.md](docs/ROADMAP.md) for the ordered backlog

## Current phase

Printer migration in [docs/ROADMAP.md](ROADMAP.md) is complete. Typed constructs print through `AstPrinter` + `LayoutRules`. `SourcePawnFormatter` is a thin facade. ERROR recovery lives under `Recovery/` and is opt-in via `AllowSyntaxRecovery`.

Deferred product asks (alignment, optional semicolons, grammar work) stay out of scope unless explicitly requested.

SpModernizer work follows MODERNIZER docs. Do not add modernize behavior to `FormattingOptions` or `SourcePawnFormatter`.

## Hard stops

- Do not "make the whole suite green" in one session.
- One language construct (or one printer layer) per session.
- Do not add regex / string post-processors outside a named Recovery module.
- Do not add `HasError` special-cases without classifying the case and updating `docs/SUPPORTED.md`.
- Do not invent new `FormattingOptions` until the option is implemented and has true/false tests that actually differ.
- Every honored option is one vertical slice: engine + CLI flag + playground control + desktop UI, listed in `FormattingOptionsCatalog`. Do not ship an option on only one surface.
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

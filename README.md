# SpFormatter

Formats SourcePawn (`.sp` / `.inc`) source using Tree-sitter and a .NET pretty-printer.

## Requirements

- .NET 10 SDK

## Build and test

```powershell
dotnet build SpFormatter.slnx
dotnet test SpFormatter.slnx
```

## CLI

```powershell
dotnet run --project src/SpFormatter.Cli -- path\to\file.sp
dotnet run --project src/SpFormatter.Cli -- path\to\dir --dir --check
dotnet run --project src/SpFormatter.Cli -- plugin.sp --backup
dotnet run --project src/SpFormatter.Cli -- plugin.sp --indent 2 --quiet
```

Useful flags: `--output`, `--stdin` (editor-friendly), `--backup` (in-place with `.bak`), `--check`, `--dir`, `--indent`, `--use-tabs`, `--space-before-paren`, `--no-space-around-operators`, `--unsafe-macros`.

## Macros

SpFormatter formats **SourcePawn**, not the preprocessor language. Files with function-like `#define Name(` macros are **refused by default** so format-on-save cannot silently break plugins. Object-like `#define MAX 64` is fine. Override with `--unsafe-macros` / `AllowUnsafeMacros` only if you accept the risk. See `corpus/macro_abuse/` and [docs/SUPPORTED.md](docs/SUPPORTED.md).

## Projects

- `src/SpFormatter` – core library
- `src/SpFormatter.Cli` – command-line tool
- `src/SpFormatter.UI` – WPF playground
- `src/SpFormatter.Playground` – web playground (Monaco + API; Railway-ready)
- `src/SpFormatter.Tests` – formatter tests and golden fixtures
- `src/SpModernizer` – transitional-syntax modernizer library
- `src/SpModernizer.Cli` – modernizer CLI
- `src/SpModernizer.Tests` – modernizer tests and golden fixtures
- `corpus/macro_abuse/` – intentional preprocessor stress cases

## Docs

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/STYLE.md](docs/STYLE.md)
- [docs/SUPPORTED.md](docs/SUPPORTED.md)
- [docs/AI_WORKFLOW.md](docs/AI_WORKFLOW.md)
- [docs/ROADMAP.md](docs/ROADMAP.md) ordered migration backlog
- [AGENTS.md](AGENTS.md) for agent/session rules

## Status

Printer migration is largely done. Defaults fail closed on syntax errors and on function-like macros.

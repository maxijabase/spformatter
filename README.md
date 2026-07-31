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

Useful flags: `--output`, `--stdin` (editor-friendly), `--backup` (in-place with `.bak`), `--check`, `--dir`, `--indent`, `--use-tabs`, `--space-before-paren`, `--no-space-around-operators`.

## Projects

- `src/SpFormatter` – core library
- `src/SpFormatter.Cli` – command-line tool
- `src/SpFormatter.UI` – WPF playground
- `tests/SpFormatter.Tests` – tests and golden fixtures

## Docs

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/STYLE.md](docs/STYLE.md)
- [docs/SUPPORTED.md](docs/SUPPORTED.md)
- [docs/AI_WORKFLOW.md](docs/AI_WORKFLOW.md)
- [docs/ROADMAP.md](docs/ROADMAP.md) ordered migration backlog
- [AGENTS.md](AGENTS.md) for agent/session rules

## Status

This repo is being salvaged toward a layered printer. Expect incomplete language coverage and a mix of legacy and new paths while that work lands.

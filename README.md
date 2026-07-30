# SpFormatter

Formats SourcePawn (`.sp` / `.inc`) source using Tree-sitter and a .NET pretty-printer.

## Requirements

- .NET 10 SDK

## Build and test

```powershell
dotnet build SpFormatter.slnx
dotnet test SpFormatter.slnx
```

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
- [AGENTS.md](AGENTS.md) for agent/session rules

## Status

This repo is being salvaged toward a layered printer. Expect incomplete language coverage and a mix of legacy and new paths while that work lands.

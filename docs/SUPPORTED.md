# Supported constructs

Living checklist. Update this when status changes.

Statuses:

- **Supported**: new printer path owns it; goldens + invariants trusted
- **Partial**: formats often, but legacy paths / recovery still involved
- **Legacy-only**: works only through the old god class
- **Out of scope**: not attempting yet

## Baseline (post .NET 10 retarget)

Recorded with `dotnet test SpFormatter.slnx` on Windows after the net10 upgrade:

- Passed: 82
- Failed: 78
- Total: 160

Many exact-match failures are CRLF in expected fixtures vs `\n` formatter output. Treat current green as legacy signal, not gospel.

## Checklist

| Construct | Status | Notes |
|---|---|---|
| literals / identifiers / types | Legacy-only | mostly passthrough |
| binary / unary / update expressions | Partial | goldens exist; spacing also hit by regex helpers |
| assignment | Partial | |
| call expressions / args | Partial | |
| variable declarations (local / global / old-style) | Partial | |
| function definitions / declarations | Partial | includes misparse fallbacks for control structures |
| native declarations | Partial | |
| if / else | Partial | syntax-only tests common; brace injection exists |
| for / while | Partial | |
| switch / case | Partial | |
| return / break / continue | Partial | |
| blocks | Partial | |
| includes / preprocessor | Partial | preprocessor often raw `node.Text`; sort-includes opt-in |
| comments | Partial | |
| ternary | Legacy-only | |
| arrays / indexed access | Partial | |
| expression fragments (no full file) | Legacy-only | wrapper recovery; fail-closed preferred going forward |
| ERROR-tree recovery | Out of scope for new work | keep behind Recovery if needed later |
| alignment options | Out of scope | options exist but unused |
| optional semicolon removal | Out of scope | option unused; tests misleading |

## Unhandled / default path

Anything not listed that hits `FormatUnknownNode` or `default` in `FormatNode` is Legacy-only / accidental. Prefer adding a typed printer path over extending `FormatUnknownNode`.

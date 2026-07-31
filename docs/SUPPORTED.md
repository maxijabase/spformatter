# Supported constructs

Living checklist. Update this when status changes.

Statuses:

- **Supported**: new printer path owns it; goldens + invariants trusted
- **Partial**: formats often, but legacy paths / recovery still involved
- **Legacy-only**: works only through the old god class
- **Out of scope**: not attempting yet

## Baseline

Recorded with `dotnet test SpFormatter.slnx`:

- Passed: 253
- Failed: 0
- Skipped: 6
- Total: 259

See [BASELINE.md](BASELINE.md) for history. Treat green as a contract for Supported constructs, not as proof that Partial/Legacy paths are finished.

## Checklist

| Construct | Status | Notes |
|---|---|---|
| literals / identifiers / types | Supported | routed through `AstPrinter` |
| binary / unary / update expressions | Supported | `AstPrinter` + `LayoutRules` (legacy regex helpers may still touch unknown paths) |
| assignment | Supported | `AstPrinter` |
| call expressions / args | Supported | `call_expression` and `call_arguments` both in `AstPrinter`; comma spacing via `LayoutRules.JoinComma` |
| variable declarations (local / global / old-style) | Supported | `AstPrinter` + `LayoutRules.JoinDeclarationParts`; array dimensions still format via legacy `FormatArrayAccess` when present |
| function definitions / declarations | Supported | clean defs/decls in `AstPrinter`; misparsed control/call shapes still use legacy fallbacks |
| native declarations | Supported | `AstPrinter` (same signature join as function declarations) |
| if / else | Partial | syntax-only tests common; brace injection exists |
| for / while | Partial | |
| switch / case | Partial | |
| return / break / continue | Partial | |
| blocks | Partial | |
| includes / preprocessor | Partial | preprocessor often raw `node.Text`; sort-includes opt-in |
| comments | Partial | |
| ternary | Supported | `AstPrinter` honors `SpaceAroundOperators` |
| arrays / indexed access | Partial | |
| expression fragments (no full file) | Legacy-only | wrapper recovery; fail-closed preferred going forward |
| ERROR-tree recovery | Out of scope for new work | keep behind Recovery if needed later |
| alignment options | Out of scope | options exist but unused |
| optional semicolon removal | Out of scope | option unused; tests misleading |

## Unhandled / default path

Anything not listed that hits `FormatUnknownNode` or `default` in `FormatNode` is Legacy-only / accidental. Prefer adding a typed printer path over extending `FormatUnknownNode`.

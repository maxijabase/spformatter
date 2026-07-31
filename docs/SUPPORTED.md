# Supported constructs

Living checklist. Update this when status changes.

Statuses:

- **Supported**: new printer path owns it; goldens + invariants trusted
- **Partial**: formats often, but legacy paths / recovery still involved
- **Legacy-only**: works only through the old god class
- **Out of scope**: not attempting yet

## Baseline

Recorded with `dotnet test SpFormatter.slnx`:

- Passed: 339
- Failed: 0
- Skipped: 0
- Total: 339

See [BASELINE.md](BASELINE.md) for history. Treat green as a contract for Supported constructs, not as proof that Partial/Legacy paths are finished.

## Checklist

| Construct | Status | Notes |
|---|---|---|
| literals / identifiers / types | Supported | routed through `AstPrinter` |
| binary / unary / update expressions | Supported | `AstPrinter` + `LayoutRules` (legacy regex helpers may still touch unknown paths) |
| assignment | Supported | `AstPrinter` |
| call expressions / args | Supported | `call_expression` and `call_arguments` both in `AstPrinter`; comma spacing via `LayoutRules.JoinComma` |
| variable declarations (local / global / old-style) | Supported | `AstPrinter` + `LayoutRules.JoinDeclarationParts`; old tags stay `Tag:name` |
| old_type / old_type_cast | Supported | `AstPrinter`; colon glued (`Handle:x`, `Float:0`); no spaces around `:` |
| function definitions / declarations | Supported | clean defs/decls in `AstPrinter`; misparse fallbacks removed |
| native declarations | Supported | `AstPrinter` (same signature join as function declarations) |
| if / else | Supported | `AstPrinter`; preserves bare single-statement bodies; trailing comments after `)` stay on the if line; no brace injection |
| char_literal | Supported | `AstPrinter` prints `node.Text` (`'\0'` stays glued; was mis-typed as `character_literal`) |
| for / while | Supported | `AstPrinter`; preserves bare bodies; for-header slot spacing |
| switch / case | Supported | `AstPrinter`; case label spacing + body blocks |
| return / break / continue | Supported | `AstPrinter`; RequireSemicolons honored |
| expression statements | Supported | `AstPrinter`; does not drop present semicolons |
| blocks | Supported | multiline `block` + compact via `PrintCompactBlock`; RequireSemicolons preserved |
| includes / preprocessor | Supported | `AstPrinter` prints `node.Text`; SortIncludes remains opt-in at source_file |
| comments | Supported | `AstPrinter`; indent only, text preserved |
| ternary | Supported | `AstPrinter` honors `SpaceAroundOperators` |
| arrays / indexed access | Supported | `array_access` / `array_indexed_access` / `fixed_dimension` in `AstPrinter`; `SpaceInArrayBrackets` via options |
| source_file top-level order | Supported | preserves declaration order; no silent include/function bucketing |
| expression fragments (no full file) | Recovery-only | gated by `AllowSyntaxRecovery`; fail-closed by default |
| ERROR-tree recovery | Recovery-only | gated by `AllowSyntaxRecovery`; regex spacing only on this path |
| alignment options | Out of scope | options exist but unused |
| optional semicolon removal | Out of scope | option unused; tests misleading |
| blank lines inside blocks | Partial | AST does not retain intra-block blanks; PreserveEmptyLines/MaxConsecutive apply to printer-emitted blanks |
| methodmap | Supported | `AstPrinter`; inheritance spacing, natives/aliases/methods/properties; trailing `;` on methodmap only |
| enum_struct | Supported | `AstPrinter`; fields + methods; brace layout matches STYLE |
| typedef / typedef_expression | Supported | `AstPrinter`; strips optional outer parens; reuses parameter printers |
| typeset | Supported | `AstPrinter`; member `typedef_expression`s; brace layout matches STYLE |
| functag | Supported | `AstPrinter`; `old_type` prints as `Tag:` without spaces around `:` |
| funcenum | Supported | `AstPrinter`; members keep trailing commas; brace layout matches STYLE |
| struct / struct_declaration | Supported | `AstPrinter`; modern fields + Plugin myinfo constructor; trailing `;` |
| enum | Supported | `AstPrinter`; named/anon, optional increment `(<<= n)`, trailing commas + `;` |
| alias_declaration / alias_assignment | Supported | `AstPrinter`; `operator++` / `operator*` stay glued; legacy overloads kept on purpose |

## Unhandled / default path

Anything not listed that hits `FormatUnknownNode` or `default` in `FormatNode` is Legacy-only / accidental. Prefer adding a typed printer path over extending `FormatUnknownNode`.

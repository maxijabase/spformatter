# Supported constructs

Living checklist. Update this when status changes.

Statuses:

- **Supported**: new printer path owns it; goldens + invariants trusted
- **Partial**: formats often, but legacy paths / recovery still involved
- **Legacy-only**: works only through the old god class
- **Out of scope**: not attempting yet

## Baseline

Recorded with `dotnet test SpFormatter.slnx`:

- Passed: 423
- Failed: 0
- Skipped: 0
- Total: 423

See [BASELINE.md](BASELINE.md) for history. Treat green as a contract for Supported constructs, not as proof that Partial/Legacy paths are finished.

## Checklist

| Construct | Status | Notes |
|---|---|---|
| literals / identifiers / types | Supported | routed through `AstPrinter` |
| binary / unary / update expressions | Supported | `AstPrinter` + `LayoutRules` (legacy regex helpers may still touch unknown paths) |
| assignment | Supported | `AstPrinter` |
| call expressions / args | Supported | `call_expression` and `call_arguments` both in `AstPrinter`; comma spacing via `LayoutRules.JoinComma`; comments are not args (no invented `/* x */,`) |
| variable declarations (local / global / old-style) | Supported | `AstPrinter` + `LayoutRules.JoinDeclarationParts`; old tags stay `Tag:name` |
| old_type / old_type_cast | Supported | `AstPrinter`; colon glued (`Handle:x`, `Float:0`); no spaces around `:` |
| function definitions / declarations | Supported | clean defs/decls in `AstPrinter`; misparse fallbacks removed; legacy tagged returns stay glued (`Action:Foo`, not dropped / not `Action: Foo`) |
| parameter list comments | Supported | same as call args: comments are not parameters (no invented `,` around `/* ... */`) |
| native declarations | Supported | `AstPrinter` (same signature join as function declarations) |
| if / else | Supported | `AstPrinter`; preserves bare single-statement bodies; trailing comments after `)` stay on the if line; `#else` / `#endif` siblings mid-if are not treated as the body; no brace injection |
| char_literal | Supported | `AstPrinter` prints `node.Text` (`'\0'` stays glued; was mis-typed as `character_literal`) |
| for / while | Supported | `AstPrinter`; preserves bare bodies; for-header slot spacing; `old_for_loop_variable_declaration_statement` (`new i = 0, s`) via declaration printer (no bogus `;` mid-header); `#else` / `#endif` siblings mid-for are not treated as the body |
| switch / case | Supported | `AstPrinter`; case label spacing; block bodies and bare statement bodies after `:`; fall-through chains |
| return / break / continue | Supported | `AstPrinter`; RequireSemicolons honored |
| delete | Supported | `AstPrinter`; space after `delete` even when operand is indexed (`delete h_timer[X]`) |
| expression statements | Supported | `AstPrinter`; does not drop present semicolons |
| blocks | Supported | multiline `block` + compact via `PrintCompactBlock`; RequireSemicolons preserved |
| includes / preprocessor directives | Supported | `AstPrinter` prints directive `node.Text` (trim trailing CR/LF only); SortIncludes remains opt-in at source_file; `#define` values with `http://` rejoined when the lexer splits them into define + `//` comment |
| function-like macros (`#define Name(`) | Partial | **refused by default** (`AllowUnsafeMacros` / `--unsafe-macros` to override). AST rewrite cannot see expansions; formatting can break compiling plugins (see `corpus/macro_abuse/`). Object-like `#define NAME value` is fine. |
| comments | Supported | `AstPrinter`; indent only, text preserved |
| ternary | Supported | `AstPrinter` honors `SpaceAroundOperators` |
| arrays / indexed access | Supported | `array_access` / `array_indexed_access` / `fixed_dimension` in `AstPrinter`; `SpaceInArrayBrackets` via options |
| array_literal | Supported | `AstPrinter`; compact when single-line; multiline when source has newlines or `//` comments; trailing/leading block comments stay on elements (no invented commas); commas after comments preserved |
| declaration `//` before initializer | Supported | line comment after `=` breaks before `{` / following declarators so `//` cannot eat the rest of the line |
| source_file top-level order | Supported | preserves declaration order; no silent include/function bucketing |
| expression fragments (no full file) | Recovery-only | gated by `AllowSyntaxRecovery`; fail-closed by default |
| ERROR-tree recovery | Recovery-only | gated by `AllowSyntaxRecovery`; regex spacing only on this path |
| alignment options | Out of scope | options exist but unused |
| optional semicolon removal | Out of scope | option unused; tests misleading |
| blank lines between siblings | Supported | restored from original source gaps between AST siblings; capped by `MaxConsecutiveEmptyLines` (default 2); off when `PreserveEmptyLines` is false |
| methodmap | Supported | `AstPrinter`; inheritance spacing, natives/aliases/methods/properties; trailing `;` on methodmap only |
| enum_struct | Supported | `AstPrinter`; fields + methods; brace layout matches STYLE |
| typedef / typedef_expression | Supported | `AstPrinter`; strips optional outer parens; reuses parameter printers |
| typeset | Supported | `AstPrinter`; member `typedef_expression`s; brace layout matches STYLE |
| functag | Supported | `AstPrinter`; `old_type` prints as `Tag:` without spaces around `:` |
| funcenum | Supported | `AstPrinter`; members keep trailing commas; brace layout matches STYLE |
| struct / struct_declaration | Supported | `AstPrinter`; modern fields + Plugin myinfo constructor; trailing `;`; field comments stay on the prior field; preprocessor lines keep column 0 and get no invented trailing `,` |
| enum | Supported | `AstPrinter`; named/anon, optional increment `(<<= n)`, trailing commas + `;` |
| alias_declaration / alias_assignment | Supported | `AstPrinter`; `operator++` / `operator*` stay glued; legacy overloads kept on purpose |

## Unhandled / default path

Anything not listed that hits `FormatUnknownNode` or `default` in `FormatNode` is Legacy-only / accidental. Prefer adding a typed printer path over extending `FormatUnknownNode`.

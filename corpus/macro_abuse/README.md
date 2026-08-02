# Macro abuse corpus

Intentional preprocessor stress cases for SpFormatter policy.

All `.sp` sources here are meant to **compile** with SourceMod's `spcomp` (tested with 1.12) and to exercise unexpanded macro shapes Tree-sitter cannot treat as ordinary SourcePawn.

## Policy

By default SpFormatter **refuses** files that contain function-like `#define Name(` macros. That prevents silent corruption such as inventing semicolons after `BEGIN_IF(...)`.

Override only with `AllowUnsafeMacros` / `--unsafe-macros`.

## Files

| File | Shape |
|---|---|
| `01_for_brace_inject.sp` | Control-flow + brace injection (`FOR`) |
| `02_begin_end_block.sp` | Paired brace macros (`BEGIN_IF` / `END`) |
| `03_function_factory.sp` | Macro emits whole functions |
| `04_nested_ifdef_token_soup.sp` | Nested `#if` + layered macros |
| `05_switch_case_macros.sp` | Case labels from macros |
| `06_decl_and_call_mix.sp` | Decl/call wrappers |

`formatted/` is local scratch from experiments; not a golden set.

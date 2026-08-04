# SpModernizer roadmap

Ordered slices for SpModernizer. SpFormatter Format behavior stays untouched.

## Status

| Step | Status |
|---|---|
| 1. Scaffold (lib, CLI, tests, docs) | Done |
| 2. `old-type-cast` | Done |
| 3. `old-builtins` + `old-types` | Done |
| 4. `old-variables` | Done |
| 5. `tagged-signatures` + multi-tag diagnostics | Done |
| 6. CLI polish | Done |
| 7. `functag` → `typedef` | Done |
| 8. `funcenum` → `typeset` | Done |
| 9. `old-struct-fields` + `legacy-while` | Done |
| 10. Playground `/api/modernize` mode | Done |
| 11. Compile lab | Parked |

## Standing rules

- Cite wiki/README for every rule
- Do not strip modern `new Type()` / `new T[n]`
- Fail closed on ERROR trees
- Refuse function-like macros by default
- Never edit SpFormatter goldens for modernize expectations

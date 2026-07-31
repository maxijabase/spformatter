# Migration roadmap

Single ordered backlog for finishing the printer salvage. When you want continuous work, tell the agent:

> Run [docs/ROADMAP.md](ROADMAP.md) nonstop. Do each open step in order. Commit after each step. Stop only at a STOP gate or when every step is Done.

Do not invent new phases outside this file. Update step status here as you finish.

## How to run this

1. Read [AGENTS.md](../AGENTS.md), [STYLE.md](STYLE.md), [SUPPORTED.md](SUPPORTED.md), [AI_WORKFLOW.md](AI_WORKFLOW.md).
2. Find the first step whose status is `Todo`.
3. Do only that step (one construct or one hygiene item).
4. Keep `dotnet test SpFormatter.slnx` green.
5. Update [SUPPORTED.md](SUPPORTED.md) and this file.
6. Commit with a lowercase subject and a short human body.
7. If the user asked for nonstop, continue to the next `Todo` step.
8. If you hit a **STOP** step, finish the commit for the previous work and wait for a human decision.

### Progress signals (not only green tests)

A step is Done only when all of these are true:

- Construct (or task) is implemented on `AstPrinter` / `LayoutRules` where applicable
- Legacy path for that construct is removed or reduced to an explicit fallback noted in SUPPORTED.md
- Related goldens stay green without mass rebasing
- Idempotency still holds for Expressions / Functions / Variables / ControlStructures
- SUPPORTED.md status is honest (Supported / Partial / Out of scope)
- This roadmap step is marked Done

Green tests alone are not enough. Mass golden edits, new regex post-processors, or new `HasError` glue mean the step failed even if CI is green.

### Standing rules (every step)

- No new formatting options unless implemented with diverging true/false tests
- No brace injection for bare `if` as a silent default
- No top-level include/function reordering as a silent default
- No expression-wrapper recovery in new code paths
- Prefer fail closed on syntax errors for new paths
- Do not push to remote unless the user asks

## Already done

| Item | Notes |
|---|---|
| .NET 10 + `.slnx` | Platform modernized |
| Agent docs and Cursor rules | AGENTS, STYLE, SUPPORTED, ARCHITECTURE, AI_WORKFLOW |
| Options shrink | Dead knobs removed |
| Expression AstPrinter | literals, binary/unary/update, assignment, ternary |
| Variable declarations | AstPrinter + JoinDeclarationParts |
| Call args | AstPrinter + JoinComma |
| Simple functions / natives / params | AstPrinter; misparse fallbacks still legacy |
| Test pyramid basics | CRLF normalize, idempotency, golden discovery, CI, corpus |

## Open steps (do in this order)

### Step 1. Arrays and indexed access

- Status: `Todo`
- Move `array_access`, `array_indexed_access`, `fixed_dimension` into `AstPrinter` using `LayoutRules.ArrayAccess` / `SpaceInArrayBrackets`
- Remove legacy `FormatArrayAccess`
- Mark arrays Supported in SUPPORTED.md (or Partial only if a real edge remains)
- Commit

### Step 2. Blocks

- Status: `Todo`
- Move `block` (and compact block behavior needed by callers) into `AstPrinter`
- Preserve statement shape. Semicolon policy via existing options only
- Remove or shrink legacy `FormatBlock` / `FormatBlockCompact`
- Commit

### Step 3. Expression statements and break/continue

- Status: `Todo`
- Move `expression_statement`, `break_statement`, `continue_statement` into `AstPrinter`
- Keep RequireSemicolons behavior correct (do not drop semis the way the old bug did)
- Commit

### Step 4. Return statements

- Status: `Todo`
- Move `return_statement` into `AstPrinter`
- Exact goldens under ControlStructures/ReturnStatements must stay honest to STYLE.md
- Commit

### Step 5. If / else without brace rewrite

- Status: `Todo`
- Move `condition_statement` into `AstPrinter`
- **Do not** wrap bare single-statement bodies in braces unless STYLE.md gains an explicit option later
- Prefer preserving source brace shape
- Replace brace-injection behavior in the new path
- Revisit skipped test `TestSpaceAroundOperators_ComparisonOperators` if it was about brace rewrite
- Mark if/else Supported when clean trees no longer need legacy FormatConditionStatement
- Commit

### Step 6. For and while

- Status: `Todo`
- Move `for_statement` and `while_statement` into `AstPrinter`
- SpaceBeforeOpenParen and semicolon spacing in for-headers via LayoutRules
- No structure rewrite beyond spacing/indent
- Commit

### Step 7. Switch and case

- Status: `Todo`
- Move `switch_statement` and `switch_case` into `AstPrinter`
- Exact-match control-structure goldens where expected files exist
- Commit

### Step 8. Comments

- Status: `Todo`
- Move line/block comments into `AstPrinter`
- Preserve comment text. Indent only
- Remove debug `Console.WriteLine` leftovers if still present near unknown-node handling
- Commit

### Step 9. Preprocessor and includes (print only)

- Status: `Todo`
- Move `preproc_*` nodes into `AstPrinter`
- Default: preserve source order (do not silently sort includes)
- `SortIncludes` stays opt-in only
- Commit

### Step 10. Source file layout without silent reorder

- Status: `Todo`
- Move `source_file` printing into `AstPrinter` (or a dedicated file printer)
- Preserve top-level declaration order by default
- Remove category bucketing (includes → defs → functions) as a silent default
- Empty-line policy via existing PreserveEmptyLines / MaxConsecutiveEmptyLines, fixed to match STYLE.md
- Un-skip empty-line option tests only if outputs genuinely differ and match STYLE
- Commit

### Step 11. Remove legacy regex spacing on clean trees

- Status: `Todo`
- Delete or quarantine `AddSpacesAroundBinaryOperators` / `RemoveSpacesAroundUnaryOperators` so clean AstPrinter paths never call them
- Unknown-node path must not reintroduce regex as the primary formatter
- Commit

### Step 12. Fail closed for invalid input

- Status: `Todo`
- New default: syntax errors → `FormatResult` failure / exception, no expression wrapping
- Move `TryFormatAsExpression` and ERROR-tree formatting behind an explicit Recovery flag or delete if unused
- Update SUPPORTED.md: expression fragments and ERROR recovery Out of scope or Recovery-only
- Commit

### Step 13. Retire function misparse fallbacks

- Status: `Todo`
- **STOP** if removing fallbacks breaks real plugin corpus or common goldens and the fix needs a grammar change
- Otherwise remove `FormatControlStructureAsFunctionFallback` / `FormatFunctionCallAsFunctionFallback` once control structures and calls are Supported on clean trees
- Commit

### Step 14. Revisit skipped option goldens

- Status: `Todo`
- For each `[Fact(Skip=...)]` in FormattingOptionsTests: either implement the real behavior, rewrite the golden to STYLE.md, or delete the vacuous test
- Do not un-skip by mass-rebasing junk
- Commit per option cluster if large

### Step 15. Shrink SourcePawnFormatter facade

- Status: `Todo`
- `SourcePawnFormatter` should mostly parse, call AstPrinter, return FormatResult
- Legacy helpers gone or isolated under `Legacy/` or `Recovery/`
- Update ARCHITECTURE.md to match reality
- Commit

### Step 16. Roadmap complete hygiene

- Status: `Todo`
- Refresh BASELINE.md counts
- Ensure AGENTS.md current phase says printer migration complete (or names only Recovery leftovers)
- Confirm corpus `--check` still passes
- Final commit: `close printer migration roadmap` (or similar)

## Deferred forever unless product asks

Do not schedule these unless the user explicitly expands scope:

- Alignment options
- Optional semicolon / `#pragma semicolon 0` mode as a full product feature
- VS Code / editor extensions
- Grammar rewrite of tree-sitter-sourcepawn
- New formatting options beyond what LayoutRules already honors

## Suggested user prompts

**One step:**

> Do the next Todo step in docs/ROADMAP.md only, then stop.

**Nonstop:**

> Run docs/ROADMAP.md nonstop. Complete every Todo step in order. Commit after each step. Pause only at STOP gates.

**Status:**

> Summarize docs/ROADMAP.md: which steps are Done, Todo, or blocked.

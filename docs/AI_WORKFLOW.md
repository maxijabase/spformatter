# AI workflow

How to work on this repo with agents without recreating the thrash loop.

For the full ordered backlog, use [ROADMAP.md](ROADMAP.md). Prefer roadmap steps over ad-hoc feature requests.

## Session shape

1. Read `AGENTS.md` and the docs it points to.
2. If continuing migration, take the first `Todo` step in `ROADMAP.md`.
3. Otherwise pick one construct or one layer (for example `LayoutRules`, or `if` statements).
4. Plan: node types, style rules from `STYLE.md`, fixtures to touch.
5. Implement only that scope.
6. Add / update invariants first when possible, then one golden, then edge cases.
7. Update `SUPPORTED.md` and mark the roadmap step Done.
8. Commit the chunk.
9. Stop, unless the user asked to run the roadmap nonstop (then continue to the next Todo, and pause only at STOP gates).

## Forbidden

- "Make all tests pass" across the whole suite in one go
- Mass golden rebases to silence failures
- New regex post-processors on clean AST output
- New `HasError` glue without classifying the case
- New formatting options that the engine does not honor
- Mixing platform chores, docs, and printer refactors in one commit

## When you hit a parse error

Stop and classify:

1. Grammar bug (upstream / tree-sitter)
2. Unsupported construct (mark Out of scope or Partial in `SUPPORTED.md`)
3. Intentional recovery (only inside a Recovery module, with tests proving clean trees are untouched)

Do not paste `node.Text` and regex it into shape as the default fix.

## Commits

- One logical chunk per commit
- Subject starts lowercase
- Short body with the why
- Sound like a person. No em-dashes. No marketing adjectives.

Examples:

```text
add layout rules for operator spacing

centralize binary/comma spacing so construct printers stop inventing their own.
```

```text
route binary expressions through ast printer

leave other node types on the legacy formatter until their turn.
```

## Tests preference

1. Invariants: idempotent, output parses, no `;;`
2. Small unit checks for `LayoutRules`
3. One minimal golden per rule
4. Real-plugin corpus later (few files)

Exact byte goldens are fine for a single rule. Do not multiply near-duplicate goldens.

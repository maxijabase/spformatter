# Test baseline

## Current (after course-correction phases 0–3)

Command:

```powershell
dotnet test SpFormatter.slnx --verbosity minimal
```

| Metric | Count |
|---|---|
| Passed | 250 |
| Failed | 0 |
| Skipped | 8 |
| Total | 258 |

Skipped items are intentional: CLI backup (incomplete product surface), and a few legacy option goldens that expect compact one-liners the printer no longer preserves.

## Earlier snapshot (right after .NET 10 retarget)

| Metric | Count |
|---|---|
| Passed | 82 |
| Failed | 78 |
| Skipped | 0 |
| Total | 160 |

Many of those failures were CRLF in fixtures vs `\n` formatter output.

## Notes

- Exact-match helpers normalize newlines and trim trailing blank lines.
- Idempotency covers Expressions / Functions / Variables / ControlStructures inputs.
- Golden discovery auto-loads exact pairs under those categories.
- CLI tests fail if the CLI binary is missing (project reference builds it).

# Test baseline

## Current

Command:

```powershell
dotnet test SpFormatter.slnx --verbosity minimal
```

| Metric | Count |
|---|---|
| Passed | 420 |
| Failed | 0 |
| Skipped | 0 |
| Total | 420 |

Printer migration roadmap is complete. Remaining Recovery path is opt-in via `AllowSyntaxRecovery`. Sibling blank lines are restored from source gaps (Step 24).

## Earlier snapshot (mid-migration)

| Metric | Count |
|---|---|
| Passed | 253 |
| Failed | 0 |
| Skipped | 6 |
| Total | 259 |

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
- CLI tests resolve `SpFormatter.Cli` from Release or Debug under `bin/` (CI uses Release).
- CLI `--backup` writes in place with a `.bak` file.
- Fail closed is the default; recovery tests opt into `AllowSyntaxRecovery`.

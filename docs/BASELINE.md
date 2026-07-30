# Test baseline

Captured after upgrading to .NET 10 (`10.0.204`) on Windows.

Command:

```powershell
dotnet test SpFormatter.slnx --verbosity minimal
```

Result:

| Metric | Count |
|---|---|
| Passed | 82 |
| Failed | 78 |
| Skipped | 0 |
| Total | 160 |

## Notes

- Many exact-match failures show CRLF (`\r\n`) in expected fixtures versus `\n` from the formatter (`FormattingOptions.LineEnding = "\n"` in tests). Fix fixtures / readers before trusting pass/fail as style signal.
- Control-structure coverage is mostly syntax-validity, not golden equality.
- Some options tests use identical true/false expected files (cannot fail).
- CLI integration tests can skip silently if the CLI binary is missing.

Treat this baseline as a snapshot of legacy behavior, not a quality bar to preserve forever.

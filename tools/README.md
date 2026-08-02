# Corpus probes

Local confidence scripts. Run from the repo root.

```powershell
dotnet run tools/corpus-probe.cs -- <corpusRoot> [--limit N] [--out artifacts/report.json]

dotnet run tools/corpus-compile-probe.cs -- <corpusRoot> `
  --spcomp C:\path\to\spcomp.exe `
  --include C:\path\to\include `
  [--limit N] [--out artifacts/report.json]
```

Scratch files matching `tools/_*` are gitignored.

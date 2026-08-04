# SpFormatter Playground

ASP.NET host for the online toolkit: Monaco UI + `/api/format` + `/api/modernize`.

Mode toggle: **Format** (layout only) vs **Modernize** (legacy → transitional syntax). Each mode loads its own sample script. `?mode=modernize` deep-links into Modernize.

## Local

```powershell
dotnet run --project src/SpFormatter.Playground
```

Open the printed URL. Health: `GET /api/health`.

## Railway

1. Create a Railway service from this repo.
2. Builder uses [`Dockerfile`](../../Dockerfile) via [`railway.toml`](../../railway.toml).
3. Set custom domain DNS to the Railway service (CNAME/ALIAS as Railway shows).
4. Confirm `https://your-domain/api/health` returns `"ok": true`.

The container builds `tree-sitter-sourcepawn.so` from `maxijabase/tree-sitter-sourcepawn` during image build. Override with Docker build-args `GRAMMAR_REPO` / `GRAMMAR_REF` if needed.

`PORT` from Railway is honored automatically.

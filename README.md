# ToDoApplication

Monorepo scaffold: a Next.js frontend, an ASP.NET Core Web API backend
with EF Core (PostgreSQL), and Docker Compose to run everything together.
This is a foundation to build real features on — see `apps/web` and
`apps/api` for each app's own code.

```
.
├── apps/
│   ├── web/   Next.js 15 (App Router, TypeScript, Tailwind)
│   └── api/   ASP.NET Core 10 Web API + EF Core (Npgsql)
├── docker-compose.yml
└── .config/dotnet-tools.json   (dotnet-ef, for migrations)
```

## Run everything with Docker (recommended for a first try)

```bash
docker compose up --build
```

- Frontend: http://localhost:3002 (not 3000 — that's often already taken
  by another local Next.js dev server; change it in `docker-compose.yml`
  if you'd rather use 3000)
- API (Swagger/OpenAPI in dev): http://localhost:8080/openapi/v1.json
- Postgres: localhost:5434 (user/pass `postgres` / `postgres`, db `todoapp`)
  — 5434, not the standard 5432/5433, since those are often already
  taken by another local Postgres; change it in `docker-compose.yml` if
  you'd rather not use 5434.

The `api` service applies no migrations automatically — run them once
against the running Postgres container:
```bash
cd apps/api
dotnet tool restore
dotnet ef database update --connection "Host=localhost;Port=5434;Database=todoapp;Username=postgres;Password=postgres"
```
(or add applying migrations as a container startup step later).

## Run locally without Docker

**Database** — start a local Postgres (or reuse the Docker one:
`docker compose up db`, reachable at `localhost:5434`), matching
`apps/api/appsettings.json`'s `ConnectionStrings:Default` (defaults to
`todoapp` / `postgres` / `postgres` on `localhost:5432` — the *native*
Postgres port; edit it to `5434` if you're pointing at the Docker one
instead).

**Backend**
```bash
cd apps/api
dotnet tool restore          # installs dotnet-ef into this repo, once
dotnet ef database update    # applies the InitialCreate migration
dotnet run                   # http://localhost:5000 / https://localhost:5001 by default
```

**Frontend**
```bash
cd apps/web
cp .env.local.example .env.local   # points at the API
npm install
npm run dev                        # http://localhost:3000
```

## Database migrations (EF Core)

```bash
cd apps/api
dotnet ef migrations add <Name> -o Data/Migrations   # create a new migration
dotnet ef database update                            # apply pending migrations
```

`Data/AppDbContext.cs` and `Models/TodoItem.cs` are the starting domain
model (a single `TodoItem` — id, title, completed, created-at) — extend
these as real stories are implemented.

## What's deliberately NOT here yet

This is a bootstrap, not a finished app: no auth (the Story LLD's
Entra ID/OIDC plan isn't wired up), no CI, no tests, and the API/DB
credentials in `appsettings.json`/`docker-compose.yml` are plain
local-dev defaults — replace them (environment variables / a secrets
manager) before this ever runs anywhere but a laptop.

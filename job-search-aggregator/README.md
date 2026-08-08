# Job Search Aggregator

A local-only, single-user AI-powered job search aggregator. It automatically discovers
jobs from free/public sources, deduplicates them, scores them against your skills using
a hybrid deterministic + LLM matching engine, and surfaces everything in a dashboard with
email notifications for high-match roles.

Full project vision, architecture, phase-by-phase progress, and testing status are tracked in
[PROJECT_PLAN.md](PROJECT_PLAN.md) — read that for the big picture. This README only covers
**how to set up and run the project**.

---

## Prerequisites

| Tool | Version used | Check with |
|---|---|---|
| .NET SDK | 9.0.x | `dotnet --version` |
| Node.js | 22.x | `node --version` |
| Angular CLI | 20.x | `ng version` |
| Docker Desktop | any recent version, with Compose v2 | `docker compose version` |
| EF Core CLI tool | 9.x/10.x | `dotnet ef --version` (install with `dotnet tool install --global dotnet-ef` if missing) |

> **Windows/port note:** if you already have a native PostgreSQL service installed and
> listening on port 5432, this project's Docker Postgres container is intentionally mapped
> to host port **5433** instead (see `docker/docker-compose.yml`) to avoid silently
> connecting to the wrong database. All connection strings in this repo already point at 5433.

---

## Quick start (recommended)

From the repo root, in PowerShell:

```powershell
.\scripts\start.ps1
```

This will:
1. Start Postgres (port 5433) + Redis (port 6379) via Docker Compose and wait for Postgres to become healthy.
2. Apply any pending EF Core migrations (`dotnet ef database update`).
3. Launch the .NET API in its own PowerShell window (`http://localhost:5071`) — this also
   hosts the background scheduler that periodically runs job providers.
4. Launch the Angular dev server in its own PowerShell window (`http://localhost:4200`),
   proxying `/api` and `/health` requests to the API.

Useful flags:

```powershell
.\scripts\start.ps1 -SkipMigration   # skip the EF migration step (faster restart)
.\scripts\start.ps1 -NoFrontend      # only start Docker + API, no Angular dev server
```

To stop everything:

```powershell
.\scripts\stop.ps1                  # stops Postgres/Redis containers, keeps data
.\scripts\stop.ps1 -RemoveVolumes   # also wipes the database (full reset)
```

(The API and Angular windows opened by `start.ps1` run independently — close them or
press `Ctrl+C` in each window when you're done.)

---

## Manual step-by-step (if you don't want to use the script)

```powershell
# 1. Start infrastructure
docker compose -f docker/docker-compose.yml up -d

# 2. Apply database migrations
dotnet ef database update `
  --project src/Infrastructure/JobSearchAggregator.Infrastructure.csproj `
  --startup-project src/Api/JobSearchAggregator.Api.csproj

# 3. Run the backend API (new terminal)
dotnet run --project src/Api/JobSearchAggregator.Api.csproj

# 4. Run the frontend (separate new terminal)
cd frontend
npm install   # first time only
npm start
```

---

## URLs once running

| Service | URL |
|---|---|
| Frontend | http://localhost:4200 |
| API Swagger UI | http://localhost:5071/swagger |
| API health check | http://localhost:5071/health |
| API base | http://localhost:5071/api |
| Postgres | `localhost:5433` (db `job_search_aggregator`, user/pass `postgres`/`postgres`) |
| Redis | `localhost:6379` |

---

## Running tests

```powershell
# Backend (xUnit) - Domain.Tests + Application.Tests
dotnet test

# Frontend (Karma/Jasmine, once Angular tests are added)
cd frontend
npm test
```

---

## Building for verification (no run)

```powershell
# Backend
dotnet build JobSearchAggregator.slnx

# Frontend
cd frontend
ng build
```

---

## Project structure

```
job-search-aggregator/
  JobSearchAggregator.slnx        # .NET solution
  src/
    Domain/                       # Entities, enums, exceptions - no dependencies
    Common/                       # Result<T>, Guard - no dependencies
    Shared/                       # ApiResponse<T>, PagedResult<T> - cross-boundary contracts
    Application/                  # CQRS (MediatR), interfaces, DTOs, validators
    Infrastructure/                # EF Core, Npgsql, Redis, repositories, scheduler
    Api/                          # ASP.NET Core Web API - Program.cs, controllers
  tests/
    Domain.Tests/
    Application.Tests/
  frontend/                       # Angular 20 + Angular Material app
  docker/docker-compose.yml       # Postgres + Redis for local dev
  docs/                           # Phase design docs (tech-stack, architecture, task plans)
  PROJECT_PLAN.md                 # Master phase tracker - read this for project state
  scripts/start.ps1, stop.ps1     # Local dev startup/shutdown helpers
```

---

## Building this project with AI agents

This codebase is being built iteratively using a set of specialized Copilot custom
subagents, routed by task type. The full **Agent Responsibility Matrix** (which agent
handles design vs. implementation vs. UI vs. testing, for each phase) lives in
[PROJECT_PLAN.md](PROJECT_PLAN.md#4-agent-responsibility-matrix). These agents run inside
the Copilot Chat session (there is no standalone CLI/script to invoke them outside the
editor) — `scripts/start.ps1`/`stop.ps1` above are strictly for running the *application
itself* (Docker + API + frontend), not for orchestrating the AI agents.

---

## Known gotchas

See [PROJECT_PLAN.md § Known Issues / Gotchas Log](PROJECT_PLAN.md#7-known-issues--gotchas-log)
for the full list (Postgres port conflict, corporate NuGet feed, `dotnet remove` being broken
on some machines, etc.).

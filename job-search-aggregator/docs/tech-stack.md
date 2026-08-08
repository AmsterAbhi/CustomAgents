# Tech Stack Decision — Phase 2 Scheduler

> Scope note: the overall stack (.NET 9, EF Core/Npgsql, Postgres, Redis, MediatR,
> FluentValidation, Polly, Serilog, Angular 20) is already fixed — see
> `PROJECT_PLAN.md`. This document covers **one open decision**: the Phase 2
> scheduler/job-execution engine.

---

## 1. Decision Inventory

- **Scheduler / recurring job execution engine** — runs the provider-fetch batch on
  an interval, supports manual "run now", and supports "retry just this one failed
  provider" without re-running the whole batch.

---

## 2. Decision: Scheduler / Recurring Job Engine

### Options Considered

1. **Quartz.NET** — mature, fully open-source (.NET) job scheduling library with
   cron/interval triggers, job stores, misfire handling.
2. **Hangfire** — open-core background job library with fire-and-forget/recurring
   jobs, retries, and a built-in web dashboard.
3. **`BackgroundService` + `PeriodicTimer`** (built into `Microsoft.Extensions.Hosting`)
   with Polly (already installed) for retry, and a small custom trigger mechanism for
   manual/retry-single-provider runs.

### 1) Persistence requirements

| | Quartz.NET | Hangfire | `BackgroundService` |
|---|---|---|---|
| Default store | `RAMJobStore` (in-memory, jobs lost on restart) | Requires a durable store — SQL Server, Redis, or community Postgres provider (`Hangfire.PostgreSql`) | None needed — no job-store concept at all |
| Durable store | `AdoJobStore` — creates/needs **~11 of its own tables** (`QRTZ_JOB_DETAILS`, `QRTZ_TRIGGERS`, etc.) in Postgres | Creates/needs **its own schema** (`hangfire.*` tables) in Postgres for job/state tracking | N/A |
| Overlap with our design | We **already** persist `SchedulerRunHistory` / `ProviderRunHistory` ourselves. A durable job store would be a second, largely redundant persistence layer tracking job execution state we already track ourselves. | Same redundancy problem — Hangfire's own job/state tables would duplicate what `SchedulerRunHistory`/`ProviderRunHistory` already record. | No duplication — the timer's "state" is just "is a run currently in progress", trivially held in memory; real state lives in our own entities as designed. |

**Verdict on this dimension**: for a single-node app that already has its own run-history
entities, both Quartz's `AdoJobStore` and Hangfire's storage add schema and operational
surface area that duplicates existing persistence, for no real benefit (no need to survive
a crash mid-job — a partially-run batch is already visible via `Status = Running` rows that
can be reconciled on next startup).

### 2) Recurring / manual / single-provider-retry support

| | Quartz.NET | Hangfire | `BackgroundService` |
|---|---|---|---|
| Recurring interval | `SimpleScheduleBuilder.WithIntervalInHours(n).RepeatForever()` — native, but interval must be set at trigger-build time, not read live from a DB-backed setting without re-scheduling logic | `RecurringJob.AddOrUpdate` with cron only (no native "every N hours from now" primitive — must express as cron, e.g. every 12h as `0 */12 * * *`, which is "every 12 hours on the clock" not "every 12 hours since last run") | `PeriodicTimer` re-read each loop iteration — trivially supports "every N hours where N comes from `UserSettings.SchedulerIntervalHours` and can change at runtime" |
| Manual "run now" | `scheduler.TriggerJob(jobKey)` — supported, straightforward | `BackgroundJob.Enqueue<T>(...)` — supported, straightforward | Signal a `SemaphoreSlim`/`Channel` from the API endpoint to wake the loop immediately — a few lines |
| Retry single failed provider | **Not a built-in concept** — you'd pass provider identity via `JobDataMap` and write your own "run just this one" job class regardless | **Not a built-in concept** — same: you'd enqueue a separate parameterized job yourself | Same: this is **application logic** either way (iterate providers, catch/record per-provider failure, re-invoke the one provider's fetch+persist logic). No library gives this "for free" — it's the same amount of code in all three options. |

**Key finding**: none of the three libraries provide "retry one failed sub-task within a
batch" as a built-in primitive — that's inherently our own orchestration logic (loop over
`IJobProvider`s, wrap each in Polly retry, record per-provider outcome). The scheduler
library's job here is only "wake up on a timer or on demand and call our orchestrator" —
which is exactly what `BackgroundService` already does with zero extra ceremony.

### 3) Licensing

| | Quartz.NET | Hangfire | `BackgroundService` |
|---|---|---|---|
| License | Apache 2.0, fully free, no tiers, no commercial edition | **Hangfire Core is free (LGPLv3)** — recurring/fire-and-forget jobs, retries, and the dashboard are all in the free Core package. **Hangfire Pro** is a separate paid add-on (batches/continuations, some extra dashboard widgets, SQL Server-specific extras) — **not required** for interval + manual-trigger use case, but worth flagging since the project already has one MediatR-style "surprise license nag" | Part of the .NET runtime/`Microsoft.Extensions.Hosting` — no license, no package at all beyond what's already referenced |
| Relevance here | No cost, no risk | No cost for what we need (Core only), but same "open-core" shape as MediatR that already tripped this project up once — worth being deliberate about *only* referencing `Hangfire.Core` + `Hangfire.PostgreSql`, never `Hangfire.Pro.*` | No cost, no risk — this removes an entire licensing question from Phase 2 |

Given the project already has one live open-source-license flag to resolve (MediatR), avoiding
introducing a second "free-for-now, paid-tier-exists" library where a same-cost-in-code
alternative exists is a reasonable risk-reduction move.

### 4) Operational simplicity for a solo local user

| Dimension | Quartz.NET | Hangfire | `BackgroundService` |
|---|---|---|---|
| New NuGet dependency | Yes (`Quartz`, `Quartz.Extensions.Hosting`) | Yes (`Hangfire.Core`, `Hangfire.AspNetCore`, `Hangfire.PostgreSql`) | **None** — already part of `Microsoft.Extensions.Hosting`, referenced transitively by the ASP.NET Core SDK |
| New DB schema/migration | Yes, if using `AdoJobStore` (else in-memory, defeating the point of "durable scheduling") | Yes — `Hangfire.PostgreSql` auto-creates its schema on first run | None |
| Dashboard/UI | No official dashboard (3rd-party `Quartzmin`, unmaintained-risk) | **Built-in web dashboard** (`/hangfire`) showing job history/retries — a genuine nice-to-have | None built-in, but we're already building our own dashboard (Phase 4) that will surface `SchedulerRunHistory`/`ProviderRunHistory` directly — a Hangfire dashboard would be a second, overlapping UI for the same information |
| Setup complexity | Moderate — trigger/job/scheduler factory boilerplate, `IJob` interface, DI integration package | Moderate — server + storage + dashboard middleware registration | Minimal — implement `BackgroundService`, register with `AddHostedService<T>()`, done |
| Learning curve | New API surface (`IJob`, `IJobDetail`, `ITrigger`, `IScheduler`) | New API surface (`IBackgroundJobClient`, recurring job registration, storage config) | **Already-known .NET primitives** (`BackgroundService`, `PeriodicTimer`, `IServiceScopeFactory` for scoped DI inside the loop) |

### Comparison Summary

| Dimension | Quartz.NET | Hangfire | `BackgroundService` |
|---|---|---|---|
| Fit | Overkill — built for complex multi-job cron scheduling we don't need | Overkill — built for high-volume background job queues across many job types/workers | Exact fit — one recurring batch job, single node, manual trigger |
| Complexity | Medium-high (its own store/schema, trigger model) | Medium-high (its own store/schema, dashboard middleware) | Low — a few dozen lines using primitives already in the SDK |
| Scalability | Scales to distributed/clustered scheduling we'll never need | Scales to many workers/queues we'll never need | Scales fine to "one process, one batch every N hours" — the actual requirement |
| Cost | Free | Free (Core only) — but open-core shape is a repeat of the MediatR situation | Free, zero new dependency |
| Team Fit | New API to learn | New API to learn, dashboard is genuinely nice | Already known — plain ASP.NET Core hosting model |

**Decisive differentiator**: the app already owns its own durable "job history" model
(`SchedulerRunHistory`/`ProviderRunHistory`) and will already have its own dashboard
(Phase 4). Both Quartz's and Hangfire's core value-adds — a durable job store and a
run-history dashboard — are things this project is building itself anyway. Pulling in
either library would mean paying their complexity/dependency/schema cost for capabilities
that are redundant here, while still having to hand-write the one thing that's actually
hard (per-provider retry orchestration), because no scheduler library does that for you.

---

## 3. Full Stack Summary (Phase 2 addition)

| Component | Technology | Why |
|---|---|---|
| Scheduler / recurring execution | `BackgroundService` (`Microsoft.Extensions.Hosting`) + `PeriodicTimer`, interval read live from `UserSettings.SchedulerIntervalHours` | Zero new dependencies, zero licensing risk, no redundant job-store schema — durable state already lives in `SchedulerRunHistory`/`ProviderRunHistory` |
| Manual "run now" trigger | API endpoint (`POST /api/scheduler/run`) signals an in-process `SemaphoreSlim`/`Channel<T>` that the `BackgroundService` loop awaits alongside the timer | Simple producer/consumer pattern, no extra package |
| Retry single failed provider | API endpoint (`POST /api/scheduler/providers/{name}/retry`) invokes the same provider-orchestration method scoped to one `IJobProvider`, tagged with `SchedulerTriggerType.RetryFailedProvider`, recorded as its own `ProviderRunHistory` row | Same orchestration code path as the full run — no special-casing needed in a scheduler library either way |
| Per-provider resilience | Polly 8.7.0 (already installed) retry/circuit-breaker policy wrapped around each `IJobProvider.FetchJobsAsync()` call | Already in the project, unused — this is exactly its intended use |
| Concurrency guard | A simple `SemaphoreSlim(1,1)` (or an `Interlocked` flag) around the whole-batch run, so a manual trigger can't overlap an in-flight automatic run | Trivial to implement, no library needed for single-node mutual exclusion |

---

## 4. Stack Cohesion Analysis

- The scheduler becomes a thin `BackgroundService` that calls into the same MediatR
  command handler (e.g. `RunSchedulerCommand`) that the manual-trigger and
  retry-provider API endpoints call — one orchestration path, three entry points
  (timer, manual API call, retry API call). This keeps CQRS/MediatR as the single
  place business logic lives, consistent with the rest of the app.
- Polly wraps each provider call inside that same handler — no new resilience
  abstraction needed, reuses what's already registered in `DependencyInjection.cs`.
- `SchedulerRunHistory`/`ProviderRunHistory` remain the single source of truth for
  "what happened and when," which is also what Phase 4's dashboard will query — no
  second dashboard (Hangfire) or missing dashboard (Quartz) to reconcile against it.
- No new Postgres schema, no new Docker service, no new NuGet package — the smallest
  possible footprint addition to an already-working Phase 1 baseline.

**Friction points**: `BackgroundService` loops need to create a **new DI scope per run**
(via `IServiceScopeFactory`) since `AppDbContext` and repositories are scoped, not
singleton — a common gotcha but a one-line fix (`using var scope = _scopeFactory.CreateScope();`).

---

## 5. When NOT to Use This Stack

This custom `BackgroundService` approach would be the **wrong** choice if any of these
become true later:
- **Multiple worker processes/machines** need to coordinate so only one runs a given
  batch at a time (needs distributed locking — Quartz clustering or Hangfire's storage-
  backed distributed locks solve this; a single in-process semaphore does not).
- **Many distinct job types** with complex dependency graphs, chained/continuation jobs,
  or dozens of differently-scheduled tasks — at that point Hangfire's dashboard and
  built-in continuation/batch support earn their complexity.
- A **non-technical operator** needs to inspect/retry jobs via a UI without waiting for
  Phase 4's dashboard — Hangfire's built-in dashboard would deliver that immediately.
- The **user count grows beyond one/single-machine** — this whole analysis is predicated
  on "single local user, single node," which is explicitly the current and near-term
  scope per `PROJECT_PLAN.md`.

---

## 6. Migration Path

- **Easy to reverse**: the `RunSchedulerCommand` MediatR handler (the actual orchestration:
  iterate providers, dedupe, persist, record history) is scheduler-agnostic — it doesn't
  know or care whether it was invoked by a `PeriodicTimer`, Quartz, or Hangfire. Swapping
  the *trigger mechanism* later (e.g., to Hangfire for its dashboard, or Quartz for
  clustering) means writing a new thin adapter that calls the same handler — the business
  logic and EF entities don't change.
- **Hard to reverse**: none of this decision touches the database schema or domain model,
  so there is no hard-to-reverse part. If Hangfire/Quartz is adopted later, it only adds
  new tables/dependencies alongside the existing ones — no destructive migration required.

---

## Final Verdict

**Implement the scheduler as a plain ASP.NET Core `BackgroundService` using
`PeriodicTimer` for the recurring interval (read live from `UserSettings.SchedulerIntervalHours`),
a `SemaphoreSlim`/`Channel`-based signal for the manual "run now" API trigger, Polly
(already installed) wrapped around each `IJobProvider` call for per-provider retry, and a
single MediatR `RunSchedulerCommand`/`RetryProviderCommand` pair that all three trigger
paths (timer, manual, retry-one-provider) invoke — do not adopt Quartz.NET or Hangfire.**
Both libraries' main value-adds (a durable job store, a run-history dashboard) duplicate
work this project is already doing itself via `SchedulerRunHistory`/`ProviderRunHistory`
and its own Phase 4 dashboard, neither library provides "retry one failed sub-task" as a
built-in primitive (so that code is identical effort either way), and Hangfire additionally
carries the same open-core "free-for-now, paid-tier-exists" shape that already flagged a
concern with MediatR — for a single-user, single-node app, pulling in either dependency
would add schema, licensing surface, and API surface to learn without buying any
capability this project doesn't already have or isn't already building.

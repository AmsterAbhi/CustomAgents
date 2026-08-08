# Phase 2 Implementation Task Sequence — Scheduler & Provider Architecture

> Sequencing of [phase2-scheduler-architecture.md](./phase2-scheduler-architecture.md)
> into small, independently compile-checkable increments. Rule: solution must
> build with 0 errors after every task before moving to the next.
> Planning only — no code written here. Hand to `coding-agent` one task (or
> small batch) at a time.

---

## 1. Provider contract + `RawJobListing` DTO
- **Files**: `src/Application/Providers/IJobProvider.cs`, `src/Application/Providers/RawJobListing.cs`
- **Done**: Both types compile; `IJobProvider.FetchJobsAsync` signature matches architecture doc §4.1; solution builds with 0 errors.
- **Depends on**: none (first task).

## 2. Job provider registry
- **Files**: `src/Application/Providers/IJobProviderRegistry.cs`, `src/Infrastructure/Providers/JobProviderRegistry.cs`
- **Done**: `JobProviderRegistry` resolves DI-registered `IEnumerable<IJobProvider>`, filters by `UserSettings.EnabledProviders`, logs+skips unknown names; solution builds with 0 errors. Not yet wired into `DependencyInjection.cs` (no providers exist yet — defer registration wiring to task 13 alongside the background service, or add a no-op `AddScoped<IJobProviderRegistry, JobProviderRegistry>()` here since it has no provider dependency).
- **Depends on**: Task 1 (`IJobProvider`).

## 3. Fake provider test double
- **Files**: `tests/Application.Tests/Scheduler/Fakes/FakeJobProvider.cs`
- **Done**: Configurable to return canned `RawJobListing`s, throw always, or throw N times then succeed; never registered in production DI; test project builds with 0 errors.
- **Depends on**: Task 1.

## 4. Dedup hash utility + unit tests
- **Files**: `src/Application/Scheduler/Services/IJobHashCalculator.cs`, `.../JobHashCalculator.cs`, `tests/Application.Tests/Scheduler/JobHashCalculatorTests.cs`
- **Done**: Pure function per architecture doc §6.1 (normalize 5 fields, SHA256, 64-char hex). Tests assert: identical hash for inputs differing only in case/whitespace/trailing-slash/query-string on `ApplyUrl`; different hash when any of the 5 fields differ. `dotnet test` passes for this file; solution builds with 0 errors.
- **Depends on**: none (pure, no dependency on tasks 1–3).

## 5. Scheduler concurrency gate + unit tests
- **Files**: `src/Application/Scheduler/Services/ISchedulerRunGate.cs`, `src/Infrastructure/Scheduler/SchedulerRunGate.cs`, `tests/Application.Tests/Scheduler/SchedulerRunGateTests.cs`
- **Done**: `SemaphoreSlim(1,1)`-backed, non-blocking `TryEnter()` + `Release()`. Tests assert `TryEnter()` → `true`, second `TryEnter()` while held → `false`, `true` again after `Release()`. Solution builds with 0 errors.
- **Depends on**: none.

## 6. Scheduler trigger signal
- **Files**: `src/Application/Scheduler/Services/ISchedulerTriggerSignal.cs`, `src/Infrastructure/Scheduler/SchedulerTriggerSignal.cs`
- **Done**: `Channel<SchedulerTriggerType>`-based singleton wrapper with a write (signal) and read (wait) method; compiles; solution builds with 0 errors. Not consumed yet (wired in task 13).
- **Depends on**: none.

## 7. Scheduler DTOs
- **Files**: `src/Application/Scheduler/SchedulerRunDto.cs`, `src/Application/Scheduler/ProviderRunDto.cs`
- **Done**: DTOs mirror `SchedulerRunHistory`/`ProviderRunHistory` fields needed for API responses (per architecture doc §9); solution builds with 0 errors.
- **Depends on**: none.

## 8. `RunSchedulerCommand` + handler (core orchestration)
- **Files**: `src/Application/Scheduler/Commands/RunSchedulerCommand.cs` (command + handler, one file)
- **Done**: Implements full pipeline from architecture doc §5.2–§5.5: gate acquire/release, `SchedulerRunHistory` create+finalize, sequential per-provider loop via registry, Polly retry wrap, `ProviderRunHistory` per provider, dedup via `IJobHashCalculator` + pre-insert existence check, status rollup (`Success`/`PartialSuccess`/`Failed`). MediatR handler registered automatically via assembly scan (already configured in `Application/DependencyInjection.cs`). Solution builds with 0 errors; no tests yet.
- **Depends on**: Tasks 1, 2, 4, 5, 7 (needs `IJobProvider`, registry, hasher, gate, DTOs).

## 9. `RunSchedulerCommandHandler` unit tests
- **Files**: `tests/Application.Tests/Scheduler/RunSchedulerCommandHandlerTests.cs`
- **Done**: Using `FakeJobProvider` (task 3) + mocked `IJobProviderRegistry`/`IRepository<Job>`/`IRepository<Company>`/`IUnitOfWork` (Moq) + a real `SchedulerRunGate` instance. Covers scenarios from architecture doc §10.1: all-success, all-fail, retry-then-succeed (asserts `RetryCount`), cross-provider hash collision (dedup skip). `dotnet test` passes; solution builds with 0 errors.
- **Depends on**: Tasks 3, 8.

## 10. `RetryProviderCommand` + handler
- **Files**: `src/Application/Scheduler/Commands/RetryProviderCommand.cs`
- **Done**: Per architecture doc §5.6 — always creates a **new** `SchedulerRunHistory` (`TriggerType = RetryFailedProvider`), validates `{OriginalRunId}`/`{ProviderName}` corresponds to an existing `Failed`/`PartialSuccess` `ProviderRunHistory` (validation error otherwise), reuses the same gate/hasher/dedup logic as task 8 for the single provider. Solution builds with 0 errors; no tests yet.
- **Depends on**: Tasks 1, 2, 4, 5, 7, 8 (reuses shared orchestration helpers from task 8 — extract shared per-provider logic into a private/internal method if needed to avoid duplication).

## 11. `RetryProviderCommandHandler` unit tests
- **Files**: `tests/Application.Tests/Scheduler/RetryProviderCommandHandlerTests.cs`
- **Done**: Asserts new `SchedulerRunHistory` row created with correct `TriggerType`, exactly one `ProviderRunHistory` child; asserts validation failure when `{id}`/`{providerName}` doesn't match an existing failed provider run. `dotnet test` passes; solution builds with 0 errors.
- **Depends on**: Task 10.

## 12. Scheduler read queries
- **Files**: `src/Application/Scheduler/Queries/GetSchedulerRunsQuery.cs`, `GetSchedulerRunByIdQuery.cs`, `GetSchedulerStatusQuery.cs`
- **Done**: Paged list (newest-first), detail-with-nested-`ProviderRuns`, and status (`IsRunning` via gate state + `LastRunAtUtc`/`NextEstimatedRunAtUtc`) queries + handlers, each returning the DTOs from task 7 wrapped as specified in architecture doc §9. Solution builds with 0 errors.
- **Depends on**: Tasks 5 (gate, for `IsRunning`), 7.

## 13. `SchedulerBackgroundService` + DI registration
- **Files**: `src/Infrastructure/Scheduler/SchedulerBackgroundService.cs`, edits to `src/Infrastructure/DependencyInjection.cs`
- **Done**: Hosted service using `PeriodicTimer` (interval re-read each iteration from `UserSettings.SchedulerIntervalHours` via a new DI scope), races timer tick against `ISchedulerTriggerSignal`, sends `RunSchedulerCommand` via `ISender` inside a fresh `IServiceScopeFactory` scope. Registered via `AddHostedService<SchedulerBackgroundService>()`; `SchedulerRunGate`/`SchedulerTriggerSignal`/`JobProviderRegistry` registered as singletons/scoped per architecture doc. Solution builds with 0 errors; `dotnet run` starts without throwing.
- **Depends on**: Tasks 2, 6, 8.

## 14. `SchedulerController` (5 endpoints)
- **Files**: `src/Api/Controllers/SchedulerController.cs`
- **Done**: `POST /api/scheduler/run`, `POST /api/scheduler/runs/{id}/retry-provider/{providerName}`, `GET /api/scheduler/runs`, `GET /api/scheduler/runs/{id}`, `GET /api/scheduler/status` — `ISender`-based, `ApiResponse<T>` envelope (same convention as `SettingsController`), `409` on gate conflict, `404` on missing run/provider. Solution builds with 0 errors; smoke test via Swagger UI or `.http` file: `POST /api/scheduler/run` returns 200 with a `SchedulerRunDto` (zero providers enabled is fine — status should be `Failed`/no-op per §5.2 step 5).
- **Depends on**: Tasks 8, 10, 12.

---

## Dependency Map (build order)

```
1 → 2 ─────────────┐
1 → 3 ──────────────┤
4 (standalone) ─────┤
5 (standalone) ─────┼──→ 8 → 9
6 (standalone) ─────┤     │
7 (standalone) ─────┘     ├──→ 10 → 11
                           │
                           ├──→ 12
                           ├──→ 13 (also needs 2, 6)
                           └──→ 14 (also needs 10, 12)
```

Tasks 3, 4, 5, 6, 7 have no interdependencies and can be built/handed off in
any order (or batched together) before task 8.

using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Shared.Contracts;
using Mapster;
using MediatR;

namespace JobSearchAggregator.Application.Scheduler.Queries;

/// <summary>
/// Returns a paged, newest-first list of scheduler runs (automatic, manual,
/// and single-provider retries alike - <c>SchedulerRunHistory</c> is a
/// uniform feed of every scheduler-level operation).
/// </summary>
public record GetSchedulerRunsQuery(PagedRequest Request) : IRequest<PagedResult<SchedulerRunDto>>;

public class GetSchedulerRunsQueryHandler : IRequestHandler<GetSchedulerRunsQuery, PagedResult<SchedulerRunDto>>
{
    private readonly IRepository<SchedulerRunHistory> _schedulerRunHistoryRepository;
    private readonly IRepository<ProviderRunHistory> _providerRunHistoryRepository;

    public GetSchedulerRunsQueryHandler(
        IRepository<SchedulerRunHistory> schedulerRunHistoryRepository,
        IRepository<ProviderRunHistory> providerRunHistoryRepository)
    {
        _schedulerRunHistoryRepository = schedulerRunHistoryRepository;
        _providerRunHistoryRepository = providerRunHistoryRepository;
    }

    public async Task<PagedResult<SchedulerRunDto>> Handle(GetSchedulerRunsQuery request, CancellationToken cancellationToken)
    {
        // IRepository<T> has no paging/ordering method (per Phase 2 design decision) -
        // load all rows and sort/page in memory. Acceptable for a local single-user
        // app with a small run-history table.
        var allRuns = await _schedulerRunHistoryRepository.ListAsync(cancellationToken);
        var orderedRuns = allRuns.OrderByDescending(r => r.StartedAtUtc).ToList();

        var pageNumber = request.Request.PageNumber < 1 ? 1 : request.Request.PageNumber;
        var pageSize = request.Request.PageSize < 1 ? 1 : request.Request.PageSize;

        var pagedRuns = orderedRuns
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var runIds = pagedRuns.Select(r => r.Id).ToList();
        var providerRuns = runIds.Count == 0
            ? new List<ProviderRunHistory>()
            : await _providerRunHistoryRepository.ListAsync(
                pr => pr.SchedulerRunHistoryId != null && runIds.Contains(pr.SchedulerRunHistoryId!.Value),
                cancellationToken);

        var providerRunsByRunId = providerRuns
            .GroupBy(pr => pr.SchedulerRunHistoryId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = pagedRuns.Select(run =>
        {
            var dto = run.Adapt<SchedulerRunDto>();
            dto.ProviderRuns = providerRunsByRunId.TryGetValue(run.Id, out var runs)
                ? runs.Select(pr => pr.Adapt<ProviderRunDto>()).ToList()
                : new List<ProviderRunDto>();
            return dto;
        }).ToList();

        return new PagedResult<SchedulerRunDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = orderedRuns.Count,
        };
    }
}

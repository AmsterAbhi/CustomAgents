using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Domain.Exceptions;
using Mapster;
using MediatR;

namespace JobSearchAggregator.Application.Scheduler.Queries;

/// <summary>
/// Returns a single scheduler run's details, including its nested
/// per-provider <see cref="ProviderRunHistory"/> rows.
/// </summary>
public record GetSchedulerRunByIdQuery(Guid Id) : IRequest<SchedulerRunDto>;

public class GetSchedulerRunByIdQueryHandler : IRequestHandler<GetSchedulerRunByIdQuery, SchedulerRunDto>
{
    private readonly IRepository<SchedulerRunHistory> _schedulerRunHistoryRepository;
    private readonly IRepository<ProviderRunHistory> _providerRunHistoryRepository;

    public GetSchedulerRunByIdQueryHandler(
        IRepository<SchedulerRunHistory> schedulerRunHistoryRepository,
        IRepository<ProviderRunHistory> providerRunHistoryRepository)
    {
        _schedulerRunHistoryRepository = schedulerRunHistoryRepository;
        _providerRunHistoryRepository = providerRunHistoryRepository;
    }

    public async Task<SchedulerRunDto> Handle(GetSchedulerRunByIdQuery request, CancellationToken cancellationToken)
    {
        var run = await _schedulerRunHistoryRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(SchedulerRunHistory), request.Id);

        var providerRuns = await _providerRunHistoryRepository.ListAsync(
            pr => pr.SchedulerRunHistoryId == request.Id,
            cancellationToken);

        var dto = run.Adapt<SchedulerRunDto>();
        dto.ProviderRuns = providerRuns.Select(pr => pr.Adapt<ProviderRunDto>()).ToList();
        return dto;
    }
}

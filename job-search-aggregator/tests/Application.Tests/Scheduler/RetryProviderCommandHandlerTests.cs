using System.Linq.Expressions;
using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Providers;
using JobSearchAggregator.Application.Scheduler.Commands;
using JobSearchAggregator.Application.Scheduler.Services;
using JobSearchAggregator.Application.Tests.Scheduler.Fakes;
using JobSearchAggregator.Domain.Common;
using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Domain.Enums;
using JobSearchAggregator.Domain.Exceptions;
using JobSearchAggregator.Infrastructure.Scheduler;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace JobSearchAggregator.Application.Tests.Scheduler;

public class RetryProviderCommandHandlerTests
{
    private readonly List<Job> _jobStore = new();
    private readonly List<Company> _companyStore = new();
    private readonly List<ProviderRunHistory> _providerRunStore = new();
    private readonly List<SchedulerRunHistory> _schedulerRunStore = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IJobProviderRegistry> _providerRegistryMock = new();

    private RetryProviderCommandHandler CreateHandler(ISchedulerRunGate? runGate = null)
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var jobRepository = CreateInMemoryRepositoryMock(_jobStore);
        var companyRepository = CreateInMemoryRepositoryMock(_companyStore);
        var providerRunRepository = CreateInMemoryRepositoryMock(_providerRunStore);
        var schedulerRunRepository = CreateInMemoryRepositoryMock(_schedulerRunStore);

        var hashCalculator = new JobHashCalculator();

        var providerRunExecutor = new ProviderRunExecutor(
            jobRepository.Object,
            companyRepository.Object,
            providerRunRepository.Object,
            _unitOfWorkMock.Object,
            hashCalculator,
            NullLogger<ProviderRunExecutor>.Instance);

        return new RetryProviderCommandHandler(
            runGate ?? new SchedulerRunGate(),
            _providerRegistryMock.Object,
            providerRunExecutor,
            schedulerRunRepository.Object,
            providerRunRepository.Object,
            _unitOfWorkMock.Object);
    }

    private static Mock<IRepository<TEntity>> CreateInMemoryRepositoryMock<TEntity>(List<TEntity> backingStore)
        where TEntity : BaseEntity
    {
        var mock = new Mock<IRepository<TEntity>>();

        mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => backingStore.FirstOrDefault(e => e.Id == id));

        mock.Setup(r => r.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => backingStore.ToList());

        mock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> predicate, CancellationToken _) => backingStore.Where(predicate.Compile()).ToList());

        mock.Setup(r => r.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))
            .Callback((TEntity entity, CancellationToken _) => backingStore.Add(entity))
            .Returns(Task.CompletedTask);

        mock.Setup(r => r.Update(It.IsAny<TEntity>()));

        return mock;
    }

    private SchedulerRunHistory SeedOriginalRunWithFailedProvider(string providerName = "ProviderA", ProviderRunStatus status = ProviderRunStatus.Failed)
    {
        var originalRun = new SchedulerRunHistory
        {
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            FinishedAtUtc = DateTime.UtcNow.AddMinutes(-55),
            TriggerType = SchedulerTriggerType.Automatic,
            Status = SchedulerRunStatus.Failed,
        };
        _schedulerRunStore.Add(originalRun);

        var failedProviderRun = new ProviderRunHistory
        {
            ProviderName = providerName,
            SchedulerRunHistoryId = originalRun.Id,
            StartedAtUtc = originalRun.StartedAtUtc,
            FinishedAtUtc = originalRun.FinishedAtUtc,
            Status = status,
            ErrorMessage = "Simulated failure.",
        };
        _providerRunStore.Add(failedProviderRun);

        return originalRun;
    }

    private static RawJobListing CreateListing(string companyName = "Acme Corp", string title = "Software Engineer", string applyUrl = "https://acme.com/apply/1") =>
        new()
        {
            CompanyName = companyName,
            Title = title,
            Location = "Remote",
            Description = "A great job.",
            ApplyUrl = applyUrl,
            ExternalId = "ext-1",
            PostedAtUtc = DateTime.UtcNow,
        };

    [Fact]
    public async Task Handle_ValidFailedProviderRun_CreatesNewSchedulerRunHistoryWithRetryTriggerTypeAndOneProviderRun()
    {
        var originalRun = SeedOriginalRunWithFailedProvider();

        var provider = new FakeJobProvider("ProviderA", JobSourceType.Greenhouse, new[] { CreateListing() });
        _providerRegistryMock.Setup(r => r.GetEnabledProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { provider });

        var handler = CreateHandler();

        var result = await handler.Handle(new RetryProviderCommand(originalRun.Id, "ProviderA"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SchedulerTriggerType.RetryFailedProvider, result.TriggerType);
        Assert.Single(result.ProviderRuns);
        Assert.Equal("ProviderA", result.ProviderRuns[0].ProviderName);
        Assert.NotEqual(originalRun.Id, result.Id);
        Assert.Equal(1, result.TotalProvidersRun);

        // A brand-new SchedulerRunHistory row was created (not attached to the original).
        Assert.Equal(2, _schedulerRunStore.Count);
        Assert.Contains(_schedulerRunStore, r => r.Id == result.Id && r.TriggerType == SchedulerTriggerType.RetryFailedProvider);
    }

    [Fact]
    public async Task Handle_ProviderRunWasPartialSuccess_StillAllowsRetry()
    {
        var originalRun = SeedOriginalRunWithFailedProvider(status: ProviderRunStatus.PartialSuccess);

        var provider = new FakeJobProvider("ProviderA", JobSourceType.Greenhouse, new[] { CreateListing() });
        _providerRegistryMock.Setup(r => r.GetEnabledProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { provider });

        var handler = CreateHandler();

        var result = await handler.Handle(new RetryProviderCommand(originalRun.Id, "ProviderA"), CancellationToken.None);

        Assert.Equal(SchedulerTriggerType.RetryFailedProvider, result.TriggerType);
        Assert.Single(result.ProviderRuns);
    }

    [Fact]
    public async Task Handle_ProviderRunDidNotFail_ThrowsNotFoundException()
    {
        // Provider run exists, but succeeded - not eligible for retry.
        var originalRun = SeedOriginalRunWithFailedProvider(status: ProviderRunStatus.Success);

        var provider = new FakeJobProvider("ProviderA", JobSourceType.Greenhouse, new[] { CreateListing() });
        _providerRegistryMock.Setup(r => r.GetEnabledProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { provider });

        var handler = CreateHandler();

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new RetryProviderCommand(originalRun.Id, "ProviderA"), CancellationToken.None));

        Assert.Single(_schedulerRunStore); // no new run was created
    }

    [Fact]
    public async Task Handle_SchedulerRunIdDoesNotExist_ThrowsNotFoundException()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new RetryProviderCommand(Guid.NewGuid(), "ProviderA"), CancellationToken.None));

        Assert.Empty(_schedulerRunStore);
    }

    [Fact]
    public async Task Handle_ProviderNameDoesNotMatchFailedRun_ThrowsNotFoundException()
    {
        var originalRun = SeedOriginalRunWithFailedProvider(providerName: "ProviderA");

        var handler = CreateHandler();

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new RetryProviderCommand(originalRun.Id, "ProviderB"), CancellationToken.None));

        Assert.Single(_schedulerRunStore); // no new run was created
    }

    [Fact]
    public async Task Handle_GateAlreadyHeld_ThrowsSchedulerRunInProgressException()
    {
        var originalRun = SeedOriginalRunWithFailedProvider();

        var provider = new FakeJobProvider("ProviderA", JobSourceType.Greenhouse, new[] { CreateListing() });
        _providerRegistryMock.Setup(r => r.GetEnabledProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { provider });

        var runGate = new SchedulerRunGate();
        runGate.TryEnter();

        var handler = CreateHandler(runGate);

        await Assert.ThrowsAsync<SchedulerRunInProgressException>(
            () => handler.Handle(new RetryProviderCommand(originalRun.Id, "ProviderA"), CancellationToken.None));

        Assert.Single(_schedulerRunStore); // no new run was created
    }
}

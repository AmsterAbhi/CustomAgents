using System.Linq.Expressions;
using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Providers;
using JobSearchAggregator.Application.Scheduler.Commands;
using JobSearchAggregator.Application.Scheduler.Services;
using JobSearchAggregator.Application.Tests.Scheduler.Fakes;
using JobSearchAggregator.Domain.Common;
using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Domain.Enums;
using JobSearchAggregator.Infrastructure.Scheduler;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace JobSearchAggregator.Application.Tests.Scheduler;

public class RunSchedulerCommandHandlerTests
{
    private readonly List<Job> _jobStore = new();
    private readonly List<Company> _companyStore = new();
    private readonly List<ProviderRunHistory> _providerRunStore = new();
    private readonly List<SchedulerRunHistory> _schedulerRunStore = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IJobProviderRegistry> _providerRegistryMock = new();

    private RunSchedulerCommandHandler CreateHandler()
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

        var runGate = new SchedulerRunGate();

        return new RunSchedulerCommandHandler(
            runGate,
            _providerRegistryMock.Object,
            providerRunExecutor,
            schedulerRunRepository.Object,
            _unitOfWorkMock.Object,
            NullLogger<RunSchedulerCommandHandler>.Instance);
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
    public async Task Handle_AllProvidersSucceed_CreatesJobsAndMarksRunSuccess()
    {
        var provider = new FakeJobProvider("ProviderA", JobSourceType.Greenhouse, new[] { CreateListing() });
        _providerRegistryMock.Setup(r => r.GetEnabledProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { provider });

        var handler = CreateHandler();

        var result = await handler.Handle(new RunSchedulerCommand(SchedulerTriggerType.Manual), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SchedulerRunStatus.Success, result!.Status);
        Assert.Equal(1, result.TotalProvidersRun);
        Assert.Equal(1, result.TotalJobsFound);
        Assert.Equal(1, result.TotalJobsAdded);
        Assert.Single(_jobStore);
        Assert.Single(_companyStore);
    }

    [Fact]
    public async Task Handle_AllProvidersFail_MarksRunFailedAndRecordsRetryCount()
    {
        var provider = new FakeJobProvider("ProviderA", JobSourceType.Greenhouse) { ThrowAlways = true };
        _providerRegistryMock.Setup(r => r.GetEnabledProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { provider });

        var handler = CreateHandler();

        var result = await handler.Handle(new RunSchedulerCommand(SchedulerTriggerType.Manual), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SchedulerRunStatus.Failed, result!.Status);
        Assert.Single(result.ProviderRuns);
        Assert.Equal(ProviderRunStatus.Failed, result.ProviderRuns[0].Status);
        Assert.Equal(2, result.ProviderRuns[0].RetryCount);
        Assert.Empty(_jobStore);
    }

    [Fact]
    public async Task Handle_ProviderFailsThenSucceeds_RecordsRetryCountAndSucceeds()
    {
        var provider = new FakeJobProvider("ProviderA", JobSourceType.Greenhouse, new[] { CreateListing() }, throwCount: 1);
        _providerRegistryMock.Setup(r => r.GetEnabledProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { provider });

        var handler = CreateHandler();

        var result = await handler.Handle(new RunSchedulerCommand(SchedulerTriggerType.Manual), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SchedulerRunStatus.Success, result!.Status);
        Assert.Equal(1, result.ProviderRuns[0].RetryCount);
        Assert.Single(_jobStore);
    }

    [Fact]
    public async Task Handle_CrossProviderDuplicateHash_OnlyInsertsJobOnce()
    {
        var listing = CreateListing();
        var providerA = new FakeJobProvider("ProviderA", JobSourceType.Greenhouse, new[] { listing });
        var providerB = new FakeJobProvider("ProviderB", JobSourceType.Greenhouse, new[] { listing });
        _providerRegistryMock.Setup(r => r.GetEnabledProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IJobProvider[] { providerA, providerB });

        var handler = CreateHandler();

        var result = await handler.Handle(new RunSchedulerCommand(SchedulerTriggerType.Manual), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.TotalJobsFound);
        Assert.Equal(1, result.TotalJobsAdded);
        Assert.Single(_jobStore);
    }

    [Fact]
    public async Task Handle_AutomaticTrigger_WhenGateAlreadyHeld_ReturnsNullWithoutCreatingRun()
    {
        var runGate = new SchedulerRunGate();
        runGate.TryEnter();

        var providerRunExecutor = new ProviderRunExecutor(
            CreateInMemoryRepositoryMock(_jobStore).Object,
            CreateInMemoryRepositoryMock(_companyStore).Object,
            CreateInMemoryRepositoryMock(_providerRunStore).Object,
            _unitOfWorkMock.Object,
            new JobHashCalculator(),
            NullLogger<ProviderRunExecutor>.Instance);

        var handler = new RunSchedulerCommandHandler(
            runGate,
            _providerRegistryMock.Object,
            providerRunExecutor,
            CreateInMemoryRepositoryMock(_schedulerRunStore).Object,
            _unitOfWorkMock.Object,
            NullLogger<RunSchedulerCommandHandler>.Instance);

        var result = await handler.Handle(new RunSchedulerCommand(SchedulerTriggerType.Automatic), CancellationToken.None);

        Assert.Null(result);
        Assert.Empty(_schedulerRunStore);
    }

    [Fact]
    public async Task Handle_ManualTrigger_WhenGateAlreadyHeld_ThrowsSchedulerRunInProgressException()
    {
        var runGate = new SchedulerRunGate();
        runGate.TryEnter();

        var providerRunExecutor = new ProviderRunExecutor(
            CreateInMemoryRepositoryMock(_jobStore).Object,
            CreateInMemoryRepositoryMock(_companyStore).Object,
            CreateInMemoryRepositoryMock(_providerRunStore).Object,
            _unitOfWorkMock.Object,
            new JobHashCalculator(),
            NullLogger<ProviderRunExecutor>.Instance);

        var handler = new RunSchedulerCommandHandler(
            runGate,
            _providerRegistryMock.Object,
            providerRunExecutor,
            CreateInMemoryRepositoryMock(_schedulerRunStore).Object,
            _unitOfWorkMock.Object,
            NullLogger<RunSchedulerCommandHandler>.Instance);

        await Assert.ThrowsAsync<JobSearchAggregator.Domain.Exceptions.SchedulerRunInProgressException>(
            () => handler.Handle(new RunSchedulerCommand(SchedulerTriggerType.Manual), CancellationToken.None));
    }
}

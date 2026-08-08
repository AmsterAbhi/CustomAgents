using System.Diagnostics;
using JobSearchAggregator.Application.Common.Interfaces;
using JobSearchAggregator.Application.Providers;
using JobSearchAggregator.Domain.Entities;
using JobSearchAggregator.Domain.Enums;
using Microsoft.Extensions.Logging;
using Polly;

namespace JobSearchAggregator.Application.Scheduler.Services;

/// <summary>
/// Default implementation of <see cref="IProviderRunExecutor"/>. Per
/// architecture doc §5.3-§5.4 and §6: wraps <see cref="IJobProvider.FetchJobsAsync"/>
/// with a simple fixed-delay Polly retry, then resolves companies
/// (find-or-create, persisted before any Job rows so the FK is always
/// valid), computes each listing's dedup hash, batch-checks which hashes
/// already exist, and inserts only the new ones.
/// </summary>
public class ProviderRunExecutor : IProviderRunExecutor
{
    private const int MaxRetries = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly IRepository<Job> _jobRepository;
    private readonly IRepository<Company> _companyRepository;
    private readonly IRepository<ProviderRunHistory> _providerRunHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJobHashCalculator _jobHashCalculator;
    private readonly ILogger<ProviderRunExecutor> _logger;

    public ProviderRunExecutor(
        IRepository<Job> jobRepository,
        IRepository<Company> companyRepository,
        IRepository<ProviderRunHistory> providerRunHistoryRepository,
        IUnitOfWork unitOfWork,
        IJobHashCalculator jobHashCalculator,
        ILogger<ProviderRunExecutor> logger)
    {
        _jobRepository = jobRepository;
        _companyRepository = companyRepository;
        _providerRunHistoryRepository = providerRunHistoryRepository;
        _unitOfWork = unitOfWork;
        _jobHashCalculator = jobHashCalculator;
        _logger = logger;
    }

    public async Task<ProviderRunHistory> ExecuteAsync(IJobProvider provider, Guid schedulerRunHistoryId, CancellationToken cancellationToken)
    {
        var providerRun = new ProviderRunHistory
        {
            ProviderName = provider.ProviderName,
            SchedulerRunHistoryId = schedulerRunHistoryId,
            StartedAtUtc = DateTime.UtcNow,
            Status = ProviderRunStatus.Running,
        };

        await _providerRunHistoryRepository.AddAsync(providerRun, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        var retryCount = 0;

        try
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: MaxRetries,
                    sleepDurationProvider: _ => RetryDelay,
                    onRetry: (_, _, _, _) => retryCount++);

            var rawListings = await retryPolicy.ExecuteAsync(
                ct => provider.FetchJobsAsync(ct),
                cancellationToken);

            var jobsAdded = await PersistNewJobsAsync(provider, rawListings, cancellationToken);

            stopwatch.Stop();

            providerRun.Status = ProviderRunStatus.Success;
            providerRun.JobsFound = rawListings.Count;
            providerRun.JobsAdded = jobsAdded;
            providerRun.RetryCount = retryCount;
            providerRun.FinishedAtUtc = DateTime.UtcNow;
            providerRun.DurationMs = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Provider '{ProviderName}' failed after {RetryCount} retries.", provider.ProviderName, retryCount);

            providerRun.Status = ProviderRunStatus.Failed;
            providerRun.JobsFound = 0;
            providerRun.JobsAdded = 0;
            providerRun.RetryCount = retryCount;
            providerRun.FinishedAtUtc = DateTime.UtcNow;
            providerRun.DurationMs = stopwatch.ElapsedMilliseconds;
            providerRun.ErrorMessage = ex.Message;
        }

        _providerRunHistoryRepository.Update(providerRun);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return providerRun;
    }

    private async Task<int> PersistNewJobsAsync(IJobProvider provider, IReadOnlyList<RawJobListing> rawListings, CancellationToken cancellationToken)
    {
        if (rawListings.Count == 0)
        {
            return 0;
        }

        var companyIdByName = await ResolveCompanyIdsAsync(rawListings, cancellationToken);

        var seenHashesInBatch = new HashSet<string>(StringComparer.Ordinal);
        var jobsToAdd = new List<Job>();

        var hashes = rawListings
            .Select(listing => _jobHashCalculator.ComputeHash(listing.CompanyName, listing.Title, listing.Location, provider.SourceType, listing.ApplyUrl))
            .ToList();

        var existingJobs = await _jobRepository.ListAsync(job => hashes.Contains(job.UniqueHash), cancellationToken);
        var existingHashes = new HashSet<string>(existingJobs.Select(job => job.UniqueHash), StringComparer.Ordinal);

        for (var i = 0; i < rawListings.Count; i++)
        {
            var listing = rawListings[i];
            var hash = hashes[i];

            if (existingHashes.Contains(hash) || !seenHashesInBatch.Add(hash))
            {
                continue;
            }

            if (!companyIdByName.TryGetValue(listing.CompanyName.Trim(), out var companyId))
            {
                _logger.LogWarning(
                    "Skipping listing '{Title}' from provider '{ProviderName}' - could not resolve company '{CompanyName}'.",
                    listing.Title, provider.ProviderName, listing.CompanyName);
                continue;
            }

            jobsToAdd.Add(new Job
            {
                CompanyId = companyId,
                Title = listing.Title,
                Location = listing.Location,
                WorkMode = listing.WorkMode,
                SalaryMin = listing.SalaryMin,
                SalaryMax = listing.SalaryMax,
                SalaryCurrency = listing.SalaryCurrency,
                ExperienceMinYears = listing.ExperienceMinYears,
                ExperienceMaxYears = listing.ExperienceMaxYears,
                EmploymentType = listing.EmploymentType,
                Department = listing.Department,
                RequiredSkills = listing.RequiredSkills,
                PreferredSkills = listing.PreferredSkills,
                Description = listing.Description,
                Responsibilities = listing.Responsibilities,
                Benefits = listing.Benefits,
                ApplyUrl = listing.ApplyUrl,
                CompanyCareerUrl = listing.CompanyCareerUrl,
                Source = provider.SourceType,
                SourceName = provider.ProviderName,
                ExternalId = listing.ExternalId,
                PostedAtUtc = listing.PostedAtUtc,
                ScrapedAtUtc = DateTime.UtcNow,
                UniqueHash = hash,
                Status = JobStatus.New,
            });
        }

        foreach (var job in jobsToAdd)
        {
            await _jobRepository.AddAsync(job, cancellationToken);
        }

        if (jobsToAdd.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return jobsToAdd.Count;
    }

    private async Task<Dictionary<string, Guid>> ResolveCompanyIdsAsync(IReadOnlyList<RawJobListing> rawListings, CancellationToken cancellationToken)
    {
        var distinctCompanyNames = rawListings
            .Select(listing => listing.CompanyName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingCompanies = await _companyRepository.ListAsync(
            company => distinctCompanyNames.Contains(company.Name),
            cancellationToken);

        var companyIdByName = existingCompanies
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var missingNames = distinctCompanyNames
            .Where(name => !companyIdByName.ContainsKey(name))
            .ToList();

        if (missingNames.Count > 0)
        {
            var newCompanies = new List<Company>();
            foreach (var name in missingNames)
            {
                var careerUrl = rawListings.FirstOrDefault(l => string.Equals(l.CompanyName.Trim(), name, StringComparison.OrdinalIgnoreCase))?.CompanyCareerUrl;

                var newCompany = new Company
                {
                    Name = name,
                    CareerPageUrl = careerUrl,
                };

                newCompanies.Add(newCompany);
                await _companyRepository.AddAsync(newCompany, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var newCompany in newCompanies)
            {
                companyIdByName[newCompany.Name] = newCompany.Id;
            }
        }

        return companyIdByName;
    }
}

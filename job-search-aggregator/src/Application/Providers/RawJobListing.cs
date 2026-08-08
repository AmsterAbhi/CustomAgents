using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Application.Providers;

/// <summary>
/// A provider's raw, source-shaped output for a single job posting - before
/// the orchestrator resolves/creates the <c>Company</c> row, computes the
/// dedup <c>UniqueHash</c>, and maps it into a persisted <c>Job</c> entity.
/// </summary>
public sealed class RawJobListing
{
    public required string CompanyName { get; init; }

    public string? CompanyCareerUrl { get; init; }

    public required string Title { get; init; }

    public required string Location { get; init; }

    public WorkMode WorkMode { get; init; } = WorkMode.Unspecified;

    public decimal? SalaryMin { get; init; }

    public decimal? SalaryMax { get; init; }

    public string? SalaryCurrency { get; init; }

    public int? ExperienceMinYears { get; init; }

    public int? ExperienceMaxYears { get; init; }

    public EmploymentType EmploymentType { get; init; } = EmploymentType.FullTime;

    public string? Department { get; init; }

    public List<string> RequiredSkills { get; init; } = new();

    public List<string> PreferredSkills { get; init; } = new();

    public required string Description { get; init; }

    public List<string> Responsibilities { get; init; } = new();

    public List<string> Benefits { get; init; } = new();

    public required string ApplyUrl { get; init; }

    public required string ExternalId { get; init; }

    public DateTime PostedAtUtc { get; init; }
}

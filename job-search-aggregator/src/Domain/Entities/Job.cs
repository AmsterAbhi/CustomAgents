using JobSearchAggregator.Domain.Common;
using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// A single job posting collected from a free, publicly accessible source.
/// Populated by provider ingestion (Phase 3); scored by the matching engine
/// (Phase 5/6).
/// </summary>
public class Job : BaseEntity
{
    public required Guid CompanyId { get; set; }

    public Company? Company { get; set; }

    public required string Title { get; set; }

    public required string Location { get; set; }

    public WorkMode WorkMode { get; set; } = WorkMode.Unspecified;

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    public string? SalaryCurrency { get; set; }

    public int? ExperienceMinYears { get; set; }

    public int? ExperienceMaxYears { get; set; }

    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;

    public string? Department { get; set; }

    public List<string> RequiredSkills { get; set; } = new();

    public List<string> PreferredSkills { get; set; } = new();

    public required string Description { get; set; }

    public List<string> Responsibilities { get; set; } = new();

    public List<string> Benefits { get; set; } = new();

    public required string ApplyUrl { get; set; }

    public string? CompanyCareerUrl { get; set; }

    public JobSourceType Source { get; set; }

    public required string SourceName { get; set; }

    public required string ExternalId { get; set; }

    public DateTime PostedAtUtc { get; set; }

    public DateTime ScrapedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Deterministic hash of Company + Title + Location + Source + ApplyUrl
    /// used to detect and reject duplicate postings (Phase 3).
    /// </summary>
    public required string UniqueHash { get; set; }

    public JobStatus Status { get; set; } = JobStatus.New;

    // Populated by the matching engine (Phase 5 - deterministic, Phase 6 - LLM assisted).
    public decimal? DeterministicMatchScore { get; set; }

    public decimal? LlmMatchScore { get; set; }

    public decimal? OverallMatchScore { get; set; }

    public decimal? MatchConfidence { get; set; }

    public string? AiReasoning { get; set; }

    public List<string> MissingSkills { get; set; } = new();

    public List<string> RecommendedSkills { get; set; } = new();

    public ICollection<SavedJob> SavedByUser { get; set; } = new List<SavedJob>();

    public ICollection<AppliedJob> AppliedByUser { get; set; } = new List<AppliedJob>();

    public ICollection<IgnoredJob> IgnoredByUser { get; set; } = new List<IgnoredJob>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

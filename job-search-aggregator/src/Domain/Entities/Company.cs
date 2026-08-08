using JobSearchAggregator.Domain.Common;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// A company that job postings belong to. Resolved/created by provider
/// ingestion (Phase 3) as postings are collected.
/// </summary>
public class Company : BaseEntity
{
    public required string Name { get; set; }

    public string? LogoUrl { get; set; }

    public string? CareerPageUrl { get; set; }

    public string? Website { get; set; }

    public string? Industry { get; set; }

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}

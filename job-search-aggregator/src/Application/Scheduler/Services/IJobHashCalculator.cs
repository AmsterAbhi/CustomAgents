using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Application.Scheduler.Services;

/// <summary>
/// Computes <c>Job.UniqueHash</c> - a deterministic hash of Company + Title +
/// Location + Source + ApplyUrl used to detect and reject duplicate
/// postings. A pure function: same five inputs always produce the same hash.
/// </summary>
public interface IJobHashCalculator
{
    string ComputeHash(string companyName, string title, string location, JobSourceType source, string applyUrl);
}

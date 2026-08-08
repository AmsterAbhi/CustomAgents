using JobSearchAggregator.Domain.Common;
using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// A single skill in the user's static profile, used by the matching engine
/// (Phase 5) to compare against a job's required/preferred skills.
/// </summary>
public class UserSkill : BaseEntity
{
    public required string Name { get; set; }

    public SkillCategory Category { get; set; }

    public bool IsCore { get; set; } = true;
}

namespace JobSearchAggregator.Domain.Enums;

/// <summary>
/// The origin type of a job posting. Every <see cref="JobSearchAggregator.Domain.Entities.Job"/>
/// must originate from a free, publicly accessible source.
/// </summary>
public enum JobSourceType
{
    CompanyCareerPage = 0,
    Greenhouse = 1,
    Lever = 2,
    Ashby = 3,
    SmartRecruiters = 4,
    Workday = 5,
    SuccessFactors = 6,
    ICIMS = 7,
    RssFeed = 8,
    PublicApi = 9
}

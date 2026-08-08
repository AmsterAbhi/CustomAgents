using JobSearchAggregator.Application.Scheduler.Services;
using JobSearchAggregator.Domain.Enums;
using Xunit;

namespace JobSearchAggregator.Application.Tests.Scheduler;

public class JobHashCalculatorTests
{
    private readonly JobHashCalculator _sut = new();

    [Fact]
    public void ComputeHash_IsCaseInsensitive()
    {
        var hash1 = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Greenhouse, "https://acme.com/apply/1");
        var hash2 = _sut.ComputeHash("ACME CORP", "SOFTWARE ENGINEER", "REMOTE", JobSourceType.Greenhouse, "HTTPS://ACME.COM/APPLY/1");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IgnoresLeadingTrailingAndInternalWhitespaceDifferences()
    {
        var hash1 = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Greenhouse, "https://acme.com/apply/1");
        var hash2 = _sut.ComputeHash("  Acme   Corp  ", "Software    Engineer", " Remote ", JobSourceType.Greenhouse, "https://acme.com/apply/1");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IgnoresTrailingSlashOnApplyUrl()
    {
        var hash1 = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Greenhouse, "https://acme.com/apply/1");
        var hash2 = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Greenhouse, "https://acme.com/apply/1/");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IgnoresQueryStringOnApplyUrl()
    {
        var hash1 = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Greenhouse, "https://acme.com/apply/1");
        var hash2 = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Greenhouse, "https://acme.com/apply/1?utm_source=linkedin&ref=abc");

        Assert.Equal(hash1, hash2);
    }

    [Theory]
    [InlineData("Different Corp", "Software Engineer", "Remote", "https://acme.com/apply/1")]
    [InlineData("Acme Corp", "Different Title", "Remote", "https://acme.com/apply/1")]
    [InlineData("Acme Corp", "Software Engineer", "Different Location", "https://acme.com/apply/1")]
    [InlineData("Acme Corp", "Software Engineer", "Remote", "https://acme.com/apply/2")]
    public void ComputeHash_DiffersWhenAnyFieldDiffers(string companyName, string title, string location, string applyUrl)
    {
        var baseline = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Greenhouse, "https://acme.com/apply/1");
        var variant = _sut.ComputeHash(companyName, title, location, JobSourceType.Greenhouse, applyUrl);

        Assert.NotEqual(baseline, variant);
    }

    [Fact]
    public void ComputeHash_DiffersWhenSourceDiffers()
    {
        var hash1 = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Greenhouse, "https://acme.com/apply/1");
        var hash2 = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Lever, "https://acme.com/apply/1");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ProducesA64CharacterLowercaseHexString()
    {
        var hash = _sut.ComputeHash("Acme Corp", "Software Engineer", "Remote", JobSourceType.Greenhouse, "https://acme.com/apply/1");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }
}

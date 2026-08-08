using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using JobSearchAggregator.Domain.Enums;

namespace JobSearchAggregator.Application.Scheduler.Services;

/// <summary>
/// Pure-function implementation of <see cref="IJobHashCalculator"/>. Per
/// architecture doc §6.1: normalize each of the five fields (trim, collapse
/// internal whitespace, lowercase; for <c>ApplyUrl</c> also strip a trailing
/// slash and any query string), join with a delimiter that cannot appear in
/// any of them (ASCII unit-separator, \u001F), then SHA256-hash and
/// hex-encode the result.
/// </summary>
public class JobHashCalculator : IJobHashCalculator
{
    private const char FieldDelimiter = '\u001F';
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    public string ComputeHash(string companyName, string title, string location, JobSourceType source, string applyUrl)
    {
        var normalizedCompanyName = NormalizeField(companyName);
        var normalizedTitle = NormalizeField(title);
        var normalizedLocation = NormalizeField(location);
        var normalizedSource = source.ToString().ToLowerInvariant();
        var normalizedApplyUrl = NormalizeApplyUrl(applyUrl);

        var composite = string.Join(
            FieldDelimiter,
            normalizedCompanyName,
            normalizedTitle,
            normalizedLocation,
            normalizedSource,
            normalizedApplyUrl);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(composite));
        return Convert.ToHexStringLower(hashBytes);
    }

    private static string NormalizeField(string value)
    {
        var trimmed = value.Trim();
        return WhitespaceRun.Replace(trimmed, " ").ToLowerInvariant();
    }

    private static string NormalizeApplyUrl(string applyUrl)
    {
        var normalized = NormalizeField(applyUrl);

        var queryIndex = normalized.IndexOf('?');
        if (queryIndex >= 0)
        {
            normalized = normalized[..queryIndex];
        }

        return normalized.TrimEnd('/');
    }
}

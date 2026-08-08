using JobSearchAggregator.Domain.Common;

namespace JobSearchAggregator.Domain.Entities;

/// <summary>
/// Structured application log entry, mirrored from Serilog for durable
/// querying from the Admin Dashboard.
/// </summary>
public class SystemLogEntry : BaseEntity
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public required string Level { get; set; }

    public required string Message { get; set; }

    public string? Exception { get; set; }

    public string? SourceContext { get; set; }
}

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobSearchAggregator.Infrastructure.Persistence.Extensions;

/// <summary>
/// Shared EF Core configuration helpers for storing a <see cref="List{T}"/> of
/// strings as a native PostgreSQL <c>jsonb</c> column (used for skill lists,
/// responsibilities, benefits, preferences, etc.).
/// </summary>
public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<List<string>> HasStringListJsonConversion(this PropertyBuilder<List<string>> builder)
    {
        var comparer = new ValueComparer<List<string>>(
            (left, right) => (left ?? new List<string>()).SequenceEqual(right ?? new List<string>()),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            value => value.ToList());

        builder
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb")
            .Metadata.SetValueComparer(comparer);

        return builder;
    }
}

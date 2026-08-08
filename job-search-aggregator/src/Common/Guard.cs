namespace JobSearchAggregator.Common;

/// <summary>
/// Guard clauses for validating method arguments at the boundary of a call,
/// failing fast with a clear exception instead of a confusing downstream error.
/// </summary>
public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"'{parameterName}' cannot be null or whitespace.", parameterName);
        }

        return value;
    }

    public static T AgainstNull<T>(T? value, string parameterName) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return value;
    }

    public static Guid AgainstEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"'{parameterName}' cannot be an empty Guid.", parameterName);
        }

        return value;
    }

    public static int AgainstNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"'{parameterName}' cannot be negative.");
        }

        return value;
    }
}

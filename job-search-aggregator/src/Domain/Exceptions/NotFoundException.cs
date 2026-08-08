namespace JobSearchAggregator.Domain.Exceptions;

/// <summary>
/// Thrown when an entity requested by its identifier cannot be found.
/// Translated by the Api's exception middleware into an HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" ({key}) was not found.")
    {
    }
}

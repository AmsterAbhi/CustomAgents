using JobSearchAggregator.Domain.Exceptions;

namespace JobSearchAggregator.Domain.Tests.Exceptions;

public class NotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithEntityNameAndGuidKey_FormatsMessageCorrectly()
    {
        var key = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var exception = new NotFoundException("Job", key);

        Assert.Equal($"Entity \"Job\" ({key}) was not found.", exception.Message);
    }

    [Fact]
    public void Constructor_WithEntityNameAndIntKey_FormatsMessageCorrectly()
    {
        var exception = new NotFoundException("Company", 42);

        Assert.Equal("Entity \"Company\" (42) was not found.", exception.Message);
    }

    [Fact]
    public void Constructor_IsAssignableToException()
    {
        var exception = new NotFoundException("UserSettings", Guid.NewGuid());

        Assert.IsAssignableFrom<Exception>(exception);
    }
}

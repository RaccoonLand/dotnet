namespace RaccoonLand.Modules.Persistence.SqlServer.Queries.Tests.Pagination;

public sealed class PageRequestTests
{
    [Fact]
    public void Constructor_AssignsAllFields()
    {
        var request = new PageRequest(2, 10, true);

        Assert.Equal(2, request.Page);
        Assert.Equal(10, request.PageSize);
        Assert.True(request.IncludeTotalCount);
    }

    [Fact]
    public void RecordEquality_UsesValueSemantics()
    {
        var a = new PageRequest(2, 10, true);
        var b = new PageRequest(2, 10, true);
        var c = new PageRequest(2, 20, true);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}

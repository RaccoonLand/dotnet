using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Cqrs;

public sealed class PaginationTests
{
    [Fact]
    public void PagedQuery_Defaults_PageAndPageSize()
    {
        var query = new SamplePagedQuery();

        Assert.Equal(1, query.Page);
        Assert.Equal(20, query.PageSize);
        Assert.False(query.IncludeTotalCount);
    }

    [Fact]
    public void PageResponse_Items_DefaultsToEmptyNonNullCollection()
    {
        var response = new SamplePageResponse();

        Assert.NotNull(response.Items);
        Assert.Empty(response.Items);
    }

    [Fact]
    public void PageResponse_TotalPages_IsZero_WhenTotalCountMissingOrPageSizeInvalid()
    {
        Assert.Equal(0, new SamplePageResponse { TotalCount = null, PageSize = 10 }.TotalPages);
        Assert.Equal(0, new SamplePageResponse { TotalCount = 100, PageSize = 0 }.TotalPages);
        Assert.Equal(0, new SamplePageResponse { TotalCount = 100, PageSize = -1 }.TotalPages);
    }

    [Fact]
    public void PageResponse_TotalPages_UsesCeilingDivision()
    {
        var response = new SamplePageResponse { TotalCount = 21, PageSize = 10 };

        Assert.Equal(3, response.TotalPages);
    }

    private sealed class SamplePagedQuery : PagedQuery<SamplePageResponse>;

    private sealed record SamplePageResponse : PageResponse<string>;
}

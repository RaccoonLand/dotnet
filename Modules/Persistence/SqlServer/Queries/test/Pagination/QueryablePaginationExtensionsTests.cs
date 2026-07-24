using Microsoft.EntityFrameworkCore;

namespace RaccoonLand.Modules.Persistence.SqlServer.Queries.Tests.Pagination;

public sealed class QueryablePaginationExtensionsTests
{
    // ---- Normalize -----------------------------------------------------------------------------

    [Theory]
    [InlineData(-5, 10, 1, 10)]
    [InlineData(0, 10, 1, 10)]
    [InlineData(1, 10, 1, 10)]
    [InlineData(3, 10, 3, 10)]
    public void Normalize_ClampsPageToAtLeastOne(int inputPage, int inputSize, int expectedPage, int expectedSize)
    {
        var result = QueryablePaginationExtensions.Normalize(inputPage, inputSize, includeTotalCount: false);

        Assert.Equal(expectedPage, result.Page);
        Assert.Equal(expectedSize, result.PageSize);
    }

    [Theory]
    [InlineData(1, 0, 100, 1)]
    [InlineData(1, -3, 100, 1)]
    [InlineData(1, 50, 100, 50)]
    [InlineData(1, 500, 100, 100)]
    [InlineData(1, 25, 20, 20)]
    public void Normalize_ClampsPageSizeIntoBounds(int page, int size, int max, int expectedSize)
    {
        var result = QueryablePaginationExtensions.Normalize(page, size, includeTotalCount: false, maxPageSize: max);

        Assert.Equal(expectedSize, result.PageSize);
    }

    [Fact]
    public void Normalize_PropagatesIncludeTotalCount()
    {
        var yes = QueryablePaginationExtensions.Normalize(1, 10, includeTotalCount: true);
        var no = QueryablePaginationExtensions.Normalize(1, 10, includeTotalCount: false);

        Assert.True(yes.IncludeTotalCount);
        Assert.False(no.IncludeTotalCount);
    }

    [Fact]
    public void Normalize_WhenMaxPageSizeBelowOne_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => QueryablePaginationExtensions.Normalize(1, 10, false, maxPageSize: 0));
    }

    // ---- ApplyPaging ---------------------------------------------------------------------------

    [Fact]
    public void ApplyPaging_WhenNullQuery_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => QueryablePaginationExtensions.ApplyPaging<int>(null!, new PageRequest(1, 10, false)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ApplyPaging_WhenPageBelowOne_Throws(int page)
    {
        var query = Enumerable.Range(1, 100).AsQueryable();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => query.ApplyPaging(new PageRequest(page, 10, false)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ApplyPaging_WhenPageSizeBelowOne_Throws(int size)
    {
        var query = Enumerable.Range(1, 100).AsQueryable();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => query.ApplyPaging(new PageRequest(1, size, false)));
    }

    [Fact]
    public void ApplyPaging_SkipsAndTakesTheRightSlice()
    {
        var query = Enumerable.Range(1, 25).AsQueryable();

        var page2 = query.ApplyPaging(new PageRequest(2, 10, false)).ToList();

        Assert.Equal(Enumerable.Range(11, 10), page2);
    }

    [Fact]
    public void ApplyPaging_LastPartialPage_ReturnsRemainingItems()
    {
        var query = Enumerable.Range(1, 25).AsQueryable();

        var page3 = query.ApplyPaging(new PageRequest(3, 10, false)).ToList();

        Assert.Equal(Enumerable.Range(21, 5), page3);
    }

    [Fact]
    public void ApplyPaging_WhenPageTimesSizeOverflowsInt32_Throws()
    {
        var query = Enumerable.Range(1, 10).AsQueryable();
        // Skip = (Page-1) * PageSize = 2_000_000_000 * 2 overflows int.
        var request = new PageRequest(2_000_000_001, 2, false);

        Assert.Throws<ArgumentOutOfRangeException>(() => query.ApplyPaging(request));
    }

    // ---- ToPagedListAsync ----------------------------------------------------------------------

    [Fact]
    public async Task ToPagedListAsync_WhenNullQuery_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => QueryablePaginationExtensions.ToPagedListAsync<int>(null!, 1, 10));
    }

    [Fact]
    public async Task ToPagedListAsync_ReturnsRequestedSlice_WithoutTotalWhenNotAsked()
    {
        await using var context = CreatePagingContext(Enumerable.Range(1, 25).Select(i => new Item { Id = i }));

        var page = await context.Items.OrderBy(i => i.Id).ToPagedListAsync(2, 10);

        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Null(page.TotalCount);
        Assert.Equal(Enumerable.Range(11, 10), page.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ToPagedListAsync_IncludesTotalCountWhenRequested()
    {
        await using var context = CreatePagingContext(Enumerable.Range(1, 25).Select(i => new Item { Id = i }));

        var page = await context.Items.OrderBy(i => i.Id).ToPagedListAsync(1, 10, includeTotalCount: true);

        Assert.Equal(25, page.TotalCount);
        Assert.Equal(10, page.Items.Count);
    }

    [Fact]
    public async Task ToPagedListAsync_NormalizesInvalidInput()
    {
        await using var context = CreatePagingContext(Enumerable.Range(1, 25).Select(i => new Item { Id = i }));

        // Page = -3 -> 1, PageSize = 500 clamped to maxPageSize 20.
        var page = await context.Items.OrderBy(i => i.Id).ToPagedListAsync(-3, 500, maxPageSize: 20);

        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(20, page.Items.Count);
    }

    // ---- support -------------------------------------------------------------------------------

    private sealed class PagingContext(DbContextOptions<PagingContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
    }

    private sealed class Item
    {
        public int Id { get; set; }
    }

    private static PagingContext CreatePagingContext(IEnumerable<Item> seed)
    {
        var options = new DbContextOptionsBuilder<PagingContext>()
            .UseInMemoryDatabase($"paging-{Guid.NewGuid():N}")
            .Options;
        var context = new PagingContext(options);
        context.Items.AddRange(seed);
        context.SaveChanges();
        return context;
    }
}

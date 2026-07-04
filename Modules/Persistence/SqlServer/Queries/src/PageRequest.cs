namespace RaccoonLand.Modules.Persistence.SqlServer.Queries;

/// <summary>
/// Effective pagination parameters after normalization.
/// </summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="IncludeTotalCount">When <see langword="true"/>, the total count should be computed.</param>
public readonly record struct PageRequest(int Page, int PageSize, bool IncludeTotalCount);

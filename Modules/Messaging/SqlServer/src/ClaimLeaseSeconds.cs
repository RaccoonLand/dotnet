namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// Converts a claim <see cref="TimeSpan"/> to whole seconds for SQL <c>DATEADD(second, …)</c>.
/// </summary>
internal static class ClaimLeaseSeconds
{
    /// <summary>
    /// Minimum lease accepted by the SQL stores. Sub-second values would otherwise be rounded up by
    /// <see cref="Math.Ceiling(double)"/> and would not match the configured duration.
    /// </summary>
    public static readonly TimeSpan Minimum = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum lease that fits in a SQL <c>int</c> second count passed to <c>DATEADD</c>.
    /// </summary>
    public static readonly TimeSpan Maximum = TimeSpan.FromSeconds(int.MaxValue);

    /// <summary>
    /// Validates <paramref name="claimLease"/> and returns whole seconds
    /// (<c>Ceiling(TotalSeconds)</c>) suitable for <c>DATEADD(second, @LeaseSeconds, …)</c>.
    /// </summary>
    public static int ToSqlSeconds(TimeSpan claimLease, string paramName = "claimLease")
    {
        if (claimLease < Minimum || claimLease > Maximum)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                claimLease,
                $"Claim lease must be between {Minimum} and {Maximum} (inclusive). " +
                "SQL lease checks use whole seconds via DATEADD(second, …).");
        }

        var leaseSeconds = (int)Math.Ceiling(claimLease.TotalSeconds);
        if (leaseSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                claimLease,
                "Claim lease must resolve to at least one second.");
        }

        return leaseSeconds;
    }
}

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Support;

/// <summary>
/// Serializes the hosting tests. <c>UseSerilog</c> assigns the process-wide static <see cref="global::Serilog.Log.Logger"/>,
/// so building hosts in parallel would let tests clobber one another's logger.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SerilogHostCollection
{
    public const string Name = "Serilog host";
}

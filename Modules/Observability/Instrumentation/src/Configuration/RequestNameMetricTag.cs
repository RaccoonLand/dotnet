namespace RaccoonLand.Modules.Observability.Instrumentation.Configuration;

/// <summary>
/// How request count/duration metrics attach <c>raccoonland.request.name</c>. Spans always use
/// <see cref="Type.FullName"/> regardless of this setting.
/// </summary>
public enum RequestNameMetricTag
{
    /// <summary>Omit the request-name tag (lowest metric cardinality).</summary>
    None = 0,

    /// <summary>
    /// Tag with the request type's unqualified name (<c>Type.Name</c>). Lower cardinality than
    /// <see cref="FullName"/>, but distinct types that share a short name collide in metrics.
    /// </summary>
    Name = 1,

    /// <summary>
    /// Tag with the request type's full name (<c>Type.FullName</c>). Default; avoids collisions across
    /// namespaces at the cost of higher cardinality.
    /// </summary>
    FullName = 2,
}

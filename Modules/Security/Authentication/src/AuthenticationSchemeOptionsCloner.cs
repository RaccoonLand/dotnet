using System.Reflection;

namespace RaccoonLand.Modules.Security.Authentication;

/// <summary>
/// Copies public instance property values from a bound scheme-options instance onto the handler options
/// created by ASP.NET Core. Copy is shallow; nested objects share references.
/// </summary>
internal static class AuthenticationSchemeOptionsCloner
{
    public static void Populate<TSource, TTarget>(TSource source, TTarget target)
        where TSource : class
        where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var targetType = target.GetType();
        var sourceType = source.GetType();

        foreach (var targetProperty in EnumerateWritableInstanceProperties(targetType))
        {
            var sourceProperty = FindReadableInstanceProperty(sourceType, targetProperty.Name);
            if (sourceProperty is null)
            {
                continue;
            }

            var value = sourceProperty.GetValue(source);

            if (value is null)
            {
                if (IsNonNullableValueType(targetProperty.PropertyType))
                {
                    continue;
                }

                targetProperty.SetValue(target, null);
                continue;
            }

            if (!targetProperty.PropertyType.IsInstanceOfType(value)
                && !targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
            {
                throw new InvalidOperationException(
                    $"Cannot copy authentication scheme option '{targetProperty.Name}': " +
                    $"source type '{sourceProperty.PropertyType.FullName}' is not assignable to " +
                    $"target type '{targetProperty.PropertyType.FullName}'.");
            }

            targetProperty.SetValue(target, value);
        }
    }

    /// <summary>
    /// Walks from the most-derived type toward <see cref="object"/> with
    /// <see cref="BindingFlags.DeclaredOnly"/> so shadowed properties such as
    /// <c>JwtBearerOptions.Events</c> vs <c>AuthenticationSchemeOptions.Events</c>
    /// do not throw <see cref="AmbiguousMatchException"/>.
    /// </summary>
    private static IEnumerable<PropertyInfo> EnumerateWritableInstanceProperties(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var current = type; current is not null && current != typeof(object); current = current.BaseType!)
        {
            foreach (var property in current.GetProperties(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!property.CanWrite
                    || property.GetIndexParameters().Length > 0
                    || !seen.Add(property.Name))
                {
                    continue;
                }

                yield return property;
            }
        }
    }

    private static PropertyInfo? FindReadableInstanceProperty(Type type, string name)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType!)
        {
            var property = current.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (property?.CanRead == true && property.GetIndexParameters().Length == 0)
            {
                return property;
            }
        }

        return null;
    }

    private static bool IsNonNullableValueType(Type type)
        => type.IsValueType && Nullable.GetUnderlyingType(type) is null;
}

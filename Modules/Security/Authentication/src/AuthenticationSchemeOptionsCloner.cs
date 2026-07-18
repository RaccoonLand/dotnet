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

        foreach (var targetProperty in targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!targetProperty.CanWrite || targetProperty.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var sourceProperty = sourceType.GetProperty(
                targetProperty.Name,
                BindingFlags.Public | BindingFlags.Instance);

            if (sourceProperty?.CanRead != true || sourceProperty.GetIndexParameters().Length > 0)
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

    private static bool IsNonNullableValueType(Type type)
        => type.IsValueType && Nullable.GetUnderlyingType(type) is null;
}

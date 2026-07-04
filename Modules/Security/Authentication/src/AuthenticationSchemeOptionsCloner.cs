using System.Reflection;

namespace RaccoonLand.Modules.Security.Authentication;

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
            if (!targetProperty.CanWrite)
            {
                continue;
            }

            var sourceProperty = sourceType.GetProperty(
                targetProperty.Name,
                BindingFlags.Public | BindingFlags.Instance);

            if (sourceProperty?.CanRead != true)
            {
                continue;
            }

            targetProperty.SetValue(target, sourceProperty.GetValue(source));
        }
    }
}

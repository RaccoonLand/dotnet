using System.Reflection;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;

namespace RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;

/// <summary>
/// <see cref="ModelBuilder"/> extensions shared across command and query persistence layers.
/// </summary>
public static class ModelBuilderConfigurationExtensions
{
    private static readonly MethodInfo ApplyConfigurationOpenGeneric = typeof(ModelBuilder)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(method =>
            method.Name == nameof(ModelBuilder.ApplyConfiguration)
            && method.IsGenericMethodDefinition
            && method.GetGenericArguments().Length == 1
            && method.GetParameters() is [{ ParameterType: var parameterType }]
            && parameterType.IsGenericType
            && parameterType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

    /// <summary>
    /// Scans <paramref name="assembly"/> for concrete, public
    /// <see cref="IEntityTypeConfiguration{TEntity}"/> types that also implement
    /// <typeparamref name="TMarker"/>, instantiates each one, and applies it to the model.
    /// </summary>
    /// <typeparam name="TMarker">
    /// The marker that tags which configurations to apply — typically
    /// <see cref="ICommandEntityConfiguration"/> or <see cref="IQueryEntityConfiguration"/>.
    /// </typeparam>
    /// <param name="modelBuilder">The model builder being configured.</param>
    /// <param name="assembly">The assembly to scan for configuration types.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="modelBuilder"/> or <paramref name="assembly"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A matching configuration type does not expose a parameterless constructor.
    /// </exception>
    public static void ApplyConfigurationsFromAssembly<TMarker>(
        this ModelBuilder modelBuilder,
        Assembly assembly)
        where TMarker : class
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !type.IsPublic)
            {
                continue;
            }

            if (!typeof(TMarker).IsAssignableFrom(type))
            {
                continue;
            }

            var configurationType = Array.Find(
                type.GetInterfaces(),
                interfaceType =>
                    interfaceType.IsGenericType
                    && interfaceType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

            if (configurationType is null)
            {
                continue;
            }

            var entityType = configurationType.GetGenericArguments()[0];
            var instance = Activator.CreateInstance(type)
                ?? throw new InvalidOperationException(
                    $"Could not create an instance of '{type.FullName}'. " +
                    "Entity type configuration classes must expose a public parameterless constructor.");

            var applyConfiguration = ApplyConfigurationOpenGeneric.MakeGenericMethod(entityType);
            applyConfiguration.Invoke(modelBuilder, [instance]);
        }
    }
}

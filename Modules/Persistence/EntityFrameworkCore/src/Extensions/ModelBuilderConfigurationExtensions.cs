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
    /// A matching type implements both CQRS markers, does not implement exactly one
    /// <see cref="IEntityTypeConfiguration{TEntity}"/>, has no public parameterless constructor,
    /// or its <c>Configure</c> method threw.
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

            EnsureExclusiveCqrsMarker(type);

            var configurationInterfaces = type.GetInterfaces()
                .Where(interfaceType =>
                    interfaceType.IsGenericType
                    && interfaceType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                .ToArray();

            if (configurationInterfaces.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Configuration type '{type.FullName}' implements {typeof(TMarker).Name} but does not " +
                    "implement any IEntityTypeConfiguration<>. Marker-only types are not allowed; " +
                    "implement exactly one closed IEntityTypeConfiguration<> or remove the marker.");
            }

            if (configurationInterfaces.Length > 1)
            {
                var entities = string.Join(
                    ", ",
                    configurationInterfaces.Select(i => i.GetGenericArguments()[0].FullName));
                throw new InvalidOperationException(
                    $"Configuration type '{type.FullName}' implements multiple " +
                    $"IEntityTypeConfiguration<> interfaces ({entities}). " +
                    "A configuration class may implement only one closed IEntityTypeConfiguration<>. " +
                    "Split mappings into separate classes.");
            }

            var configurationType = configurationInterfaces[0];
            var entityType = configurationType.GetGenericArguments()[0];
            var instance = CreateConfigurationInstance(type);

            var applyConfiguration = ApplyConfigurationOpenGeneric.MakeGenericMethod(entityType);
            try
            {
                applyConfiguration.Invoke(modelBuilder, [instance]);
            }
            catch (TargetInvocationException exception)
            {
                throw new InvalidOperationException(
                    $"Failed to apply configuration '{type.FullName}' for entity '{entityType.FullName}'. " +
                    "See the inner exception for details.",
                    exception.InnerException ?? exception);
            }
        }
    }

    private static void EnsureExclusiveCqrsMarker(Type type)
    {
        var isCommand = typeof(ICommandEntityConfiguration).IsAssignableFrom(type);
        var isQuery = typeof(IQueryEntityConfiguration).IsAssignableFrom(type);
        if (isCommand && isQuery)
        {
            throw new InvalidOperationException(
                $"Configuration type '{type.FullName}' implements both " +
                $"{nameof(ICommandEntityConfiguration)} and {nameof(IQueryEntityConfiguration)}. " +
                "A configuration class must belong to exactly one CQRS side; dual markers are not allowed.");
        }
    }

    private static object CreateConfigurationInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type)
                ?? throw new InvalidOperationException(FormatConstructorRequirement(type));
        }
        catch (MissingMethodException exception)
        {
            throw new InvalidOperationException(FormatConstructorRequirement(type), exception);
        }
        catch (MemberAccessException exception)
        {
            throw new InvalidOperationException(FormatConstructorRequirement(type), exception);
        }
        catch (TargetInvocationException exception)
        {
            throw new InvalidOperationException(
                $"The parameterless constructor of configuration type '{type.FullName}' threw. " +
                "See the inner exception for details. Constructor injection is not supported; " +
                "configurations must be creatable with a public parameterless constructor.",
                exception.InnerException ?? exception);
        }
    }

    private static string FormatConstructorRequirement(Type type)
        => $"Could not create an instance of configuration type '{type.FullName}'. " +
           "Entity type configuration classes must expose a public parameterless constructor. " +
           "Constructor injection / DI is not supported by ApplyConfigurationsFromAssembly.";
}

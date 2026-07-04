namespace RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
/// Marker interface for <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/>
/// classes that belong on the <strong>command</strong> (write) <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
/// </summary>
/// <remarks>
/// Implement this marker together with <c>IEntityTypeConfiguration&lt;TEntity&gt;</c> on configuration classes
/// under the command persistence layer, then apply them with
/// <see cref="Extensions.ModelBuilderConfigurationExtensions.ApplyConfigurationsFromAssembly{TMarker}"/>.
/// Configurations without this marker are ignored when the command marker is used.
/// </remarks>
public interface ICommandEntityConfiguration;

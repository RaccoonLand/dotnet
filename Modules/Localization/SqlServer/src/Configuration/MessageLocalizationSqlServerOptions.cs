using System.ComponentModel.DataAnnotations;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Configuration;

/// <summary>
/// Settings for the SQL Server message-localization implementation, typically bound from a configuration
/// section such as <c>appsettings.json</c>.
/// </summary>
/// <example>
/// <code>
/// "MessageLocalization": {
///   "ConnectionString": "Server=.;Database=SharedLocalization;Trusted_Connection=True;TrustServerCertificate=True",
///   "ServiceName": "Ordering",
///   "ApplicationName": "Ordering.Api",
///   "DefaultCulture": "en-US",
///   "RefreshInterval": "00:05:00",
///   "AutoRegisterApplication": true,
///   "AutoInsertMissingKeys": true
/// }
/// </code>
/// </example>
public sealed class MessageLocalizationSqlServerOptions
{
    /// <summary>Default root configuration section name (<c>MessageLocalization</c>).</summary>
    public const string SectionName = "MessageLocalization";

    /// <summary>Connection string to the shared localization database.</summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Name of the current microservice (a row in <c>Localization.Services</c>).</summary>
    [Required]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Name of the current application within the service (a row in <c>Localization.Applications</c>).</summary>
    [Required]
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Culture used when neither the caller nor the registered <c>ICurrentCultureProvider</c> specifies one.
    /// Defaults to <c>en-US</c>.
    /// </summary>
    public string DefaultCulture { get; set; } = "en-US";

    /// <summary>Database schema that contains the localization tables. Defaults to <c>Localization</c>.</summary>
    public string SchemaName { get; set; } = "Localization";

    /// <summary>Name of the services table. Defaults to <c>Services</c>.</summary>
    public string ServicesTableName { get; set; } = "Services";

    /// <summary>Name of the applications table. Defaults to <c>Applications</c>.</summary>
    public string ApplicationsTableName { get; set; } = "Applications";

    /// <summary>Name of the message-localizations table. Defaults to <c>MessageLocalizations</c>.</summary>
    public string MessageLocalizationsTableName { get; set; } = "MessageLocalizations";

    /// <summary>
    /// When <see langword="true"/> the schema and tables are created at startup if they do not exist.
    /// Requires the connection's account to have DDL permissions; defaults to <see langword="false"/>.
    /// </summary>
    public bool AutoCreateTables { get; set; }

    /// <summary>
    /// How often the in-memory translations are reloaded from the database. Defaults to 5 minutes.
    /// </summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When <see langword="true"/> the service/application rows are created on startup if they do not exist.
    /// </summary>
    public bool AutoRegisterApplication { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/> a missing key is persisted (with <c>Value = Key</c> and
    /// <c>RequiresTranslation = 1</c>) so an admin can later provide the real translation.
    /// </summary>
    public bool AutoInsertMissingKeys { get; set; } = true;
}

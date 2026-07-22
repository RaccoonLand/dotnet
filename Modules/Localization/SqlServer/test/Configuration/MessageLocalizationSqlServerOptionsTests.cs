using RaccoonLand.Modules.MessageLocalization.SQLServer.Configuration;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Configuration;

public sealed class MessageLocalizationSqlServerOptionsTests
{
    [Fact]
    public void Defaults_MatchDocumentedContract()
    {
        var options = new MessageLocalizationSqlServerOptions();

        Assert.Equal(string.Empty, options.ConnectionString);
        Assert.Equal(string.Empty, options.ServiceName);
        Assert.Equal(string.Empty, options.ApplicationName);
        Assert.Equal("en-US", options.DefaultCulture);
        Assert.Equal("Localization", options.SchemaName);
        Assert.Equal("Services", options.ServicesTableName);
        Assert.Equal("Applications", options.ApplicationsTableName);
        Assert.Equal("MessageLocalizations", options.MessageLocalizationsTableName);
        Assert.False(options.AutoCreateTables);
        Assert.Equal(TimeSpan.FromMinutes(5), options.RefreshInterval);
        Assert.True(options.AutoRegisterApplication);
        Assert.True(options.AutoInsertMissingKeys);
        Assert.Equal("MessageLocalization", MessageLocalizationSqlServerOptions.SectionName);
    }
}

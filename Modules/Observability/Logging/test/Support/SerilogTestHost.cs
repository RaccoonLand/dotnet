using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Support;

/// <summary>
/// Builds a real generic host, runs <c>UseRaccoonLandSerilog</c> through <c>Build()</c> (which invokes the
/// deferred logger callback), emits a single probe log, and returns the captured (fully enriched) event.
/// <para>
/// These tests touch the process-wide static <see cref="Log.Logger"/> that <c>UseSerilog</c> assigns, so the
/// hosting test classes are serialized via <see cref="SerilogHostCollection"/>.
/// </para>
/// </summary>
internal static class SerilogTestHost
{
    public const string ProbeMessage = "probe";

    /// <summary>Builds the host, logs one Information event and returns the enriched <see cref="LogEvent"/>.</summary>
    public static LogEvent CaptureEvent(Action<IHostBuilder> configureBuilder)
    {
        var sink = new InMemorySink();

        var builder = new HostBuilder();
        builder.ConfigureServices(services => services.AddSingleton<ILogEventSink>(sink));
        configureBuilder(builder);

        try
        {
            using var host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<InMemorySink>>();
            logger.LogInformation(ProbeMessage);

            return sink.Events.Single(e => e.MessageTemplate.Text == ProbeMessage);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>Adds keys under the given section to an explicit <see cref="IConfiguration"/>.</summary>
    public static IConfiguration Configuration(params (string Key, string Value)[] pairs)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.ToDictionary(p => p.Key, p => (string?)p.Value))
            .Build();
}

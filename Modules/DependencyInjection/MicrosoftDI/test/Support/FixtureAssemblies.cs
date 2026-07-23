using System.Reflection;
using RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.AmbiguousMatchingInterface.Impl;
using RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.ConflictingServices;
using RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.InvalidMatchingInterface;
using RaccoonLand.Modules.DependencyInjection.Tests.Fixtures.Valid;

namespace RaccoonLand.Modules.DependencyInjection.MicrosoftDI.Tests.Support;

/// <summary>
/// Scrutor scans whole assemblies, so each scenario lives in its own isolated fixture assembly to avoid one
/// scenario's types leaking into another scan.
/// </summary>
internal static class FixtureAssemblies
{
    public static Assembly Valid => typeof(IncludedPublicService).Assembly;

    public static Assembly InvalidMatchingInterface => typeof(NoMatchingInterfaceService).Assembly;

    public static Assembly AmbiguousMatchingInterface => typeof(AmbiguousService).Assembly;

    public static Assembly ConflictingServices => typeof(IConflictContract).Assembly;
}

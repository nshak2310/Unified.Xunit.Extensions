using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Unified.Xunit.Extensions;

/// <summary>
/// Holds the assembly fixture instances that <see cref="UnifiedTestAssemblyRunner"/> discovers and
/// instantiates at test-assembly startup. Test classes retrieve their fixture from here instead of
/// receiving it via constructor injection (see <see cref="IAssemblyFixture{TFixture}"/> for why).
/// </summary>
public static class AssemblyFixtureRegistry
{
    private static readonly ConcurrentDictionary<Type, object> Fixtures = new();

    /// <summary>
    /// Retrieves the assembly fixture of type <typeparamref name="TFixture"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No fixture of this type was registered, either because no test class in the assembly implements
    /// <see cref="IAssemblyFixture{TFixture}"/> for this type, or because the unified test framework
    /// is not active for this assembly (check the <c>[assembly: Xunit.TestFramework]</c> attribute).
    /// </exception>
    public static TFixture Get<TFixture>() where TFixture : class, new()
    {
        if (TryGet<TFixture>(out var fixture))
            return fixture;

        throw new InvalidOperationException(
            $"No assembly fixture of type '{typeof(TFixture).FullName}' is registered. " +
            $"Ensure some test class in the assembly implements IAssemblyFixture<{typeof(TFixture).Name}>, " +
            "and that UnifiedTestFramework is the active [assembly: Xunit.TestFramework] for this assembly.");
    }

    /// <summary>
    /// Attempts to retrieve the assembly fixture of type <typeparamref name="TFixture"/>.
    /// </summary>
    public static bool TryGet<TFixture>([NotNullWhen(true)] out TFixture? fixture) where TFixture : class, new()
    {
        if (Fixtures.TryGetValue(typeof(TFixture), out var value))
        {
            fixture = (TFixture)value;
            return true;
        }

        fixture = null;
        return false;
    }

    /// <summary>
    /// Registers a fixture instance. Called by <see cref="UnifiedTestAssemblyRunner"/> during
    /// assembly startup; not intended to be called directly by consumers.
    /// </summary>
    internal static void Register(Type fixtureType, object instance) => Fixtures[fixtureType] = instance;

    /// <summary>
    /// Snapshot of the currently registered fixture instances, used by
    /// <see cref="UnifiedTestAssemblyRunner"/> to dispose them during teardown.
    /// </summary>
    internal static IReadOnlyCollection<object> Instances => Fixtures.Values.ToArray();

    /// <summary>
    /// Clears all registrations. Called by <see cref="UnifiedTestAssemblyRunner"/> after disposing
    /// every fixture, so state never leaks across test runs within the same process.
    /// </summary>
    internal static void Clear() => Fixtures.Clear();
}

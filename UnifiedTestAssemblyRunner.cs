using Meziantou.Xunit;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Unified.Xunit.Extensions;

/// <summary>
/// Inherits Meziantou's parallel-class-execution engine (<see cref="ParallelTestAssemblyRunner"/>) and
/// layers assembly-fixture discovery, instantiation, and disposal on top of it, replicating
/// xunit.assemblyfixture's behaviour without taking a dependency on that package (which would
/// re-introduce the same duplicate-[assembly: TestFramework] conflict this library exists to solve).
/// </summary>
public class UnifiedTestAssemblyRunner : ParallelTestAssemblyRunner
{
    public UnifiedTestAssemblyRunner(
        ITestAssembly testAssembly,
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions)
        : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
    {
    }

    /// <summary>
    /// Runs just after the test assembly reports as starting, before any test collection runs.
    /// Scans the assembly for types implementing <see cref="IAssemblyFixture{TFixture}"/>, instantiates
    /// each distinct fixture type exactly once, and registers it in <see cref="AssemblyFixtureRegistry"/>.
    /// </summary>
    protected override async Task AfterTestAssemblyStartingAsync()
    {
        await base.AfterTestAssemblyStartingAsync().ConfigureAwait(false);

        await Aggregator.RunAsync(async () =>
        {
            foreach (var fixtureType in DiscoverAssemblyFixtureTypes())
            {
                var instance = Activator.CreateInstance(fixtureType)
                    ?? throw new InvalidOperationException(
                        $"Activator.CreateInstance returned null for assembly fixture type '{fixtureType.FullName}'.");

                if (instance is IAsyncLifetime asyncLifetime)
                {
                    await asyncLifetime.InitializeAsync().ConfigureAwait(false);
                }

                AssemblyFixtureRegistry.Register(fixtureType, instance);
            }

            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs after every test collection has finished, before the test assembly reports as finished.
    /// Disposes every registered fixture (preferring <see cref="IAsyncDisposable"/> over
    /// <see cref="IDisposable"/> when a fixture implements both), isolating each fixture's disposal
    /// so one failing Dispose doesn't prevent the others from running.
    /// </summary>
    protected override async Task BeforeTestAssemblyFinishedAsync()
    {
        foreach (var fixture in AssemblyFixtureRegistry.Instances)
        {
            await Aggregator.RunAsync(async () =>
            {
                switch (fixture)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }).ConfigureAwait(false);
        }

        AssemblyFixtureRegistry.Clear();

        await base.BeforeTestAssemblyFinishedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Finds every distinct closed fixture type <c>TFixture</c> referenced by an
    /// <c>IAssemblyFixture&lt;TFixture&gt;</c> implementation anywhere in the test assembly.
    /// Discovers fixtures registered via Assembly Attribute ([assembly: AssemblyFixture(typeof(...))])
    /// </summary>
    private IEnumerable<Type> DiscoverAssemblyFixtureTypes()
    {
        var assembly = ((IReflectionAssemblyInfo)TestAssembly.Assembly).Assembly;

        // 1. Discover fixtures registered via Assembly Attribute ([assembly: AssemblyFixture(typeof(...))])
        var attributeFixtureTypes = assembly.GetCustomAttributes(false)
            .Where(attr => attr.GetType().Name == "AssemblyFixtureAttribute")
            .Select(attr => (Type)attr.GetType().GetProperty("FixtureType")?.GetValue(attr)!)
            .Where(t => t != null);

        // 2. Discover fixtures registered via Class Interface (IAssemblyFixture<T>)
        var interfaceFixtureTypes = assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces())
            .Where(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IAssemblyFixture<>))
            .Select(iface => iface.GetGenericArguments()[0]);

        // 3. Combine both lists, filter out nulls, and ensure each type is registered only once
        return attributeFixtureTypes
            .Concat(interfaceFixtureTypes)
            .Distinct();
    }
}

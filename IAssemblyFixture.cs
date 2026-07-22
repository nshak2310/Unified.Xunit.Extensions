namespace Unified.Xunit.Extensions;

/// <summary>
/// Marker interface that flags <typeparamref name="TFixture"/> as assembly-wide shared state.
/// <see cref="UnifiedTestAssemblyRunner"/> instantiates one instance of <typeparamref name="TFixture"/>
/// per test assembly before any test runs, and disposes it after the last test completes.
/// </summary>
/// <remarks>
/// Unlike xunit.assemblyfixture, the instance is never constructor-injected into the test class,
/// because <see cref="UnifiedTestAssemblyRunner"/> delegates class execution to
/// Meziantou.Xunit.ParallelTestFramework's own class runner, which only understands xUnit's native
/// <c>IClassFixture&lt;T&gt;</c> / <c>ICollectionFixture&lt;T&gt;</c> constructor-injection wiring.
/// Instead, retrieve the instance from <see cref="AssemblyFixtureRegistry"/>.
/// </remarks>
/// <typeparam name="TFixture">The type of the shared fixture. Must be a public, concrete, parameterless-constructible class.</typeparam>
public interface IAssemblyFixture<TFixture> where TFixture : class, new()
{
}

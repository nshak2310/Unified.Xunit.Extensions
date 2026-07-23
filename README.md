# Unified.Xunit.Extensions

Claims xUnit v2's single `[assembly: Xunit.TestFramework]` entry point so a test project gets
**Meziantou.Xunit.ParallelTestFramework**'s parallel-class execution *and* per-assembly fixture state
(`IAssemblyFixture<T>`, in the spirit of `xunit.assemblyfixture`) at the same time — without the CS0579
duplicate-attribute error you get from installing both packages' own test frameworks side by side.

## 1. Install

```xml
<PackageReference Include="Unified.Xunit.Extensions" Version="x.y.z" />
```

You do **not** need to also install `Meziantou.Xunit.ParallelTestFramework` or any `xunit.assemblyfixture`
package. `Unified.Xunit.Extensions` already depends on Meziantou's package, and its types (e.g.
`[DisableParallelization]`, `[EnableParallelization]`) are available to your test project transitively. We never
depend on `xunit.assemblyfixture` at all — `IAssemblyFixture<T>` is reimplemented internally, specifically so
that package's own unconditional attribute injection can't reintroduce the conflict this library exists to solve.

## 2. Claim the xUnit entry point

Add this once, anywhere in your test project (e.g. in an `AssemblyInfo.cs`):

```csharp
[assembly: Xunit.TestFramework("Unified.Xunit.Extensions.UnifiedTestFramework", "Unified.Xunit.Extensions")]
```

## 3. Do you need to disable Meziantou's auto-registration?

**If your test project only references `Unified.Xunit.Extensions` — the normal case — no.** Meziantou ships its
auto-registration as a `build\*.props` file, and NuGet only applies a non-transitive `build` asset to the project
that *directly* references the package. Since `Unified.Xunit.Extensions` holds that direct reference (not your test
project), Meziantou's own `[assembly: TestFramework]` attribute is never injected into your compilation. Verified: a
test project that only `ProjectReference`/`PackageReference`s this library builds clean with no extra steps.

**Only if you *also* add a direct reference to `Meziantou.Xunit.ParallelTestFramework` yourself** (for example
because tooling suggested it, or you want its attributes without going through us) does the conflict come back —
in that case, add this to your `.csproj` to disable its auto-registration:

```xml
<PropertyGroup>
  <IncludeMeziantouXunitParallelTestFramework>false</IncludeMeziantouXunitParallelTestFramework>
</PropertyGroup>
```

This has been verified both ways: without it you get `CS0579: Duplicate 'Xunit.TestFramework' attribute`; with it,
in a plain `<PropertyGroup>` anywhere in your own project file, the build is clean. (MSBuild fully resolves all
properties before it evaluates the conditional `AssemblyAttribute` item that Meziantou's props file adds, so where
in the file you set it doesn't matter.)

## 4. Write a fixture and a test class

```csharp
public sealed class DatabaseFixture : IAsyncDisposable
{
    public DatabaseFixture() { /* expensive one-time setup */ }

    public ValueTask DisposeAsync()
    {
        /* expensive one-time teardown */
        return ValueTask.CompletedTask;
    }
}

public class OrderTests : IAssemblyFixture<DatabaseFixture>
{
    [Fact]
    public void CreatingAnOrder_Succeeds()
    {
        var db = AssemblyFixtureRegistry.Get<DatabaseFixture>();
        // ...
    }
}
```

- `IAssemblyFixture<TFixture>` is just a marker. Implementing it on at least one test class tells
  `UnifiedTestAssemblyRunner` to construct `TFixture` exactly once, before any test in the assembly runs, and
  dispose it after the last one finishes.
- Any number of classes can implement `IAssemblyFixture<TFixture>` for the same `TFixture` — they all share one
  instance.
- Alternative: Registering via Assembly Attribute (xUnit v3 Style)
Instead of implementing the marker interface on a test class, you can register your assembly-level fixture directly at the assembly level using the `[assembly: AssemblyFixture(...)]` attribute:

```csharp
[assembly: AssemblyFixture(typeof(MySharedFixture))]
- Unlike `xunit.assemblyfixture`, the fixture is **never constructor-injected** into your test class. Class
  construction is handled by Meziantou's own class runner, which only understands xUnit's native
  `IClassFixture<T>` / `ICollectionFixture<T>` wiring — so always fetch the instance from
  `AssemblyFixtureRegistry.Get<TFixture>()` (or `TryGet`).
- `TFixture` needs a public parameterless constructor (`where TFixture : class, new()`) — the same requirement
  xUnit's own `IClassFixture<T>` has.
- On teardown, a fixture implementing `IAsyncDisposable` is disposed via `DisposeAsync()`; otherwise
  `IDisposable.Dispose()` is used. One fixture failing to dispose doesn't stop the others from being disposed —
  all failures are aggregated and surfaced together.

## Notes

xUnit v2 (`xunit.extensibility.execution`) is in maintenance mode — new feature work has moved to xUnit v3, which
has native assembly-fixture support of its own plus a `Meziantou.Xunit.v3.ParallelTestFramework`. This library
targets v2 because that's the pipeline being unified here; if you're starting a brand-new test project rather than
extending an existing v2 suite, evaluate v3 first.

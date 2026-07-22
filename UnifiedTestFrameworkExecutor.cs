using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Unified.Xunit.Extensions;

/// <summary>
/// Standard xUnit v2 executor wiring: constructs <see cref="UnifiedTestAssemblyRunner"/> instead of
/// the stock <see cref="XunitTestAssemblyRunner"/> (or Meziantou's <c>ParallelTestAssemblyRunner</c>)
/// so every test run goes through the combined parallelisation + assembly-fixture pipeline.
/// </summary>
public class UnifiedTestFrameworkExecutor : XunitTestFrameworkExecutor
{
    public UnifiedTestFrameworkExecutor(
        AssemblyName assemblyName,
        ISourceInformationProvider sourceInformationProvider,
        IMessageSink diagnosticMessageSink)
        : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
    {
    }

    protected override async void RunTestCases(
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions)
    {
        using var assemblyRunner = new UnifiedTestAssemblyRunner(
            TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions);

        await assemblyRunner.RunAsync().ConfigureAwait(false);
    }
}

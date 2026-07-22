using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Unified.Xunit.Extensions;

/// <summary>
/// The single xUnit v2 test framework entry point for this assembly. Claim it with:
/// <c>[assembly: Xunit.TestFramework("Unified.Xunit.Extensions.UnifiedTestFramework", "Unified.Xunit.Extensions")]</c>
/// instead of letting Meziantou.Xunit.ParallelTestFramework and xunit.assemblyfixture each register
/// their own (which conflict with a CS0579 duplicate-attribute error).
/// </summary>
public class UnifiedTestFramework : XunitTestFramework
{
    public UnifiedTestFramework(IMessageSink messageSink) : base(messageSink)
    {
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        => new UnifiedTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}

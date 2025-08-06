using System.Threading.Tasks;
using BenchmarkDotNet.Reports;
using Soenneker.Benchmarking.Extensions.Summary;
using Soenneker.Tests.Benchmark;
using Xunit;

namespace Soenneker.Utils.SingletonDictionary.Tests.Benchmarks;

public sealed class BenchmarkRunner : BenchmarkTest
{
    public BenchmarkRunner(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    //[Fact]
    public async ValueTask Benchmark()
    {
        Summary summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmark>(DefaultConf);

        await summary.OutputSummaryToLog(OutputHelper, CancellationToken);
    }
}
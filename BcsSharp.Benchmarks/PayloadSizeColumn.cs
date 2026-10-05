using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BcsSharp.Benchmarks;

public interface IPayloadSize
{
    int BcsSize();

    int MessagePackSize();
}

/// <summary>
/// Shows the encoded size of the payload each row serializes.
/// Columns render in the host process, not the benchmark process, so the size is recomputed here from the seeded payload rather than recorded during the run.
/// </summary>
public sealed class PayloadSizeColumn : IColumn
{
    public string Id => nameof(PayloadSizeColumn);
    public string ColumnName => "Size";
    public string Legend => "Encoded payload size in bytes";
    public UnitType UnitType => UnitType.Dimensionless;
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 0;
    public bool IsNumeric => true;

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, summary.Style);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var payload = (IPayloadSize)Activator.CreateInstance(benchmarkCase.Descriptor.Type)!;
        var isBcs = benchmarkCase.Descriptor.WorkloadMethod.Name.StartsWith("Bcs", StringComparison.Ordinal);
        return $"{(isBcs ? payload.BcsSize() : payload.MessagePackSize()):N0} B";
    }
}

public sealed class PayloadSizeConfig : ManualConfig
{
    public PayloadSizeConfig() => AddColumn(new PayloadSizeColumn());
}

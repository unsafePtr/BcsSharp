using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BcsSharp.Benchmarks;

public class SizeColumn : IColumn
{
    public string Id => nameof(SizeColumn);
    public string ColumnName => "Size";
    public string Legend => "Serialized size in bytes";
    public UnitType UnitType => UnitType.Size;
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 0;
    public bool IsNumeric => true;
    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        // Get the method name
        var methodName = benchmarkCase.Descriptor.WorkloadMethod.Name;
        
        // Get cached size for this method
        var size = SizeCache.GetSize(methodName);
        
        if (size.HasValue)
        {
            return FormatSize(size.Value);
        }
        
        return "N/A";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        return GetValue(summary, benchmarkCase);
    }

    private static string FormatSize(int bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        else
            return $"{bytes / (1024.0 * 1024):F2} MB";
    }
}

public static class SizeCache
{
    private static readonly Dictionary<string, int> _sizes = new();

    public static void SetSize(string methodName, int size)
    {
        _sizes[methodName] = size;
    }

    public static int? GetSize(string methodName)
    {
        return _sizes.TryGetValue(methodName, out var size) ? size : null;
    }

    public static void Clear() => _sizes.Clear();
}
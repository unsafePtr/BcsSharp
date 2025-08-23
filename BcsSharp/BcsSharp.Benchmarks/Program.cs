using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;

namespace BcsSharp.Benchmarks;

public class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 BcsSharp vs MessagePack Benchmarks");
        Console.WriteLine("=====================================");

        //var config = ManualConfig.Create(DefaultConfig.Instance)
        //    .AddExporter(CsvExporter.Default)
        //    .AddExporter(MarkdownExporter.GitHub)
        //    .AddExporter(HtmlExporter.Default);

        //if (args.Length == 0)
        //{
        //    //BenchmarkRunner.Run<SerializationBenchmarks>(config);
        //    //BenchmarkRunner.Run<PrimitiveBenchmarks>(config);
        //    //BenchmarkRunner.Run<SizeBenchmarks>(config);
        //    return;
        //}

        //switch (args[0].ToLower())
        //{
        //    case "serialization":
        //        BenchmarkRunner.Run<SerializationBenchmarks>(config);
        //        break;

        //    case "primitives":
        //        BenchmarkRunner.Run<PrimitiveBenchmarks>(config);
        //        break;

        //    case "size":
        //        BenchmarkRunner.Run<SizeBenchmarks>(config);
        //        break;

        //    case "all":
        //        BenchmarkRunner.Run<SerializationBenchmarks>(config);
        //        BenchmarkRunner.Run<PrimitiveBenchmarks>(config);
        //        BenchmarkRunner.Run<SizeBenchmarks>(config);
        //        break;

        //    default:
        //        Console.WriteLine($"Unknown option: {args[0]}");
        //        Console.WriteLine("Use 'serialization', 'primitives', 'size', 'all'");
        //        break;
        //}

    }
}

using BenchmarkDotNet.Running;

namespace BcsSharp.Benchmarks;

public static class Program
{
    public static void Main(string[] args) => BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
}

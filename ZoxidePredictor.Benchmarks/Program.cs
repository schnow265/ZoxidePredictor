using BenchmarkDotNet.Running;

namespace ZoxidePredictor.Benchmarks;

class Program
{
    static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}
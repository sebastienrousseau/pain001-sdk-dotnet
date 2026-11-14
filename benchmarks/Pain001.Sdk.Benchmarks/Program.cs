// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// Runs the client-side benchmarks, and the loopback ones when
// PAIN001_BASE_URL names a running `pain001 serve`. Arguments go to
// BenchmarkDotNet; `--job Dry` is the CI smoke run. With no --filter,
// every benchmark runs.
//
//   dotnet run -c Release --project benchmarks/Pain001.Sdk.Benchmarks
//   dotnet run -c Release --project benchmarks/Pain001.Sdk.Benchmarks -- --job Dry

using BenchmarkDotNet.Running;
using Pain001.Sdk.Benchmarks;

var types = new List<Type> { typeof(ClientSideBenchmarks) };
if (LoopbackBenchmarks.BaseUrl is null)
{
    Console.WriteLine("PAIN001_BASE_URL is unset: skipping the loopback benchmarks (start pain001 serve and set it to run them).");
}
else
{
    types.Add(typeof(LoopbackBenchmarks));
}

// In process (--inProcess): no generated child project to restore,
// which locked-mode restore in CI would refuse.
List<string> arguments = [.. args];
if (!arguments.Contains("--filter"))
{
    arguments.AddRange(["--filter", "*"]);
}

if (!arguments.Contains("--inProcess"))
{
    arguments.Add("--inProcess");
}

BenchmarkSwitcher.FromTypes([.. types]).Run([.. arguments]);

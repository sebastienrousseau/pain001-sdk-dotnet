// SPDX-License-Identifier: Apache-2.0 OR MIT

using BenchmarkDotNet.Attributes;

namespace Pain001.Sdk.Benchmarks;

/// <summary>
/// One round trip per call to a real <c>pain001 serve</c> on loopback at
/// <c>PAIN001_BASE_URL</c>, with the kit's fixtures: the client, the
/// loopback stack and the server's own work together.
/// </summary>
[MemoryDiagnoser]
public class LoopbackBenchmarks : IDisposable
{
    private Pain001Client? _client;

    /// <summary>The server address, or <see langword="null"/> to skip these benchmarks.</summary>
    internal static string? BaseUrl =>
        Environment.GetEnvironmentVariable("PAIN001_BASE_URL") is { Length: > 0 } url ? url : null;

    [GlobalSetup]
    public void Setup() =>
        _client = new Pain001Client(new Uri(BaseUrl ?? throw new InvalidOperationException("PAIN001_BASE_URL is unset.")));

    [Benchmark]
    public Task<HealthResponse> Health() => Client.HealthAsync();

    [Benchmark]
    public Task<ValidationResponse> Validate() =>
        Client.ValidateAsync(Fixtures.Csv, "payments.csv", MessageTypes.Pain001V09);

    [Benchmark]
    public Task<UiGenerateResponse> Generate() =>
        Client.GenerateAsync(Fixtures.Csv, "payments.csv", MessageTypes.Pain001V09);

    [Benchmark]
    public Task<CheckResponse> Check() => Client.CheckAsync(Fixtures.Xml);

    [Benchmark]
    public Task<IReadOnlyList<FaceInfo>> Faces() => Client.FacesAsync();

    [Benchmark]
    public Task<FaceResponse> ProjectFace() => Client.ProjectFaceAsync("berlin_group", Fixtures.Xml);

    [Benchmark]
    public Task<Pain001RawResponse> RawRequest() => Client.RequestAsync("GET", "/api/v1/health");

    [GlobalCleanup]
    public void Dispose()
    {
        _client?.Dispose();
        _client = null;
        GC.SuppressFinalize(this);
    }

    private Pain001Client Client => _client ?? throw new InvalidOperationException("Setup has not run.");
}

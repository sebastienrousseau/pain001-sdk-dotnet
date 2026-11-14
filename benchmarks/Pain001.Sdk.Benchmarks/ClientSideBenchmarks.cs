// SPDX-License-Identifier: Apache-2.0 OR MIT

using BenchmarkDotNet.Attributes;

namespace Pain001.Sdk.Benchmarks;

/// <summary>
/// The client's own cost per call, no network: request building and JSON
/// serialization of the kit's fixtures, and decoding of the server's
/// recorded answer, through an in-memory handler.
/// </summary>
[MemoryDiagnoser]
public class ClientSideBenchmarks : IDisposable
{
    private readonly Dictionary<string, (HttpClient Http, Pain001Client Client)> _clients = [];

    [GlobalSetup]
    public void Setup()
    {
        foreach ((string name, string file) in new[]
        {
            ("health", "health.json"),
            ("validate", "validate.json"),
            ("generate", "generate.json"),
            ("check", "check.json"),
            ("faces", "faces.json"),
            ("project", "project-face.json"),
        })
        {
            var http = new HttpClient(new RecordedHandler(Fixtures.Response(file)));
            _clients[name] = (http, new Pain001Client(http, new Pain001ClientOptions { BaseAddress = new Uri("http://pain001.bench/") }));
        }
    }

    [Benchmark]
    public Task<HealthResponse> Health() => Client("health").HealthAsync();

    [Benchmark]
    public Task<ValidationResponse> Validate() =>
        Client("validate").ValidateAsync(Fixtures.Csv, "payments.csv", MessageTypes.Pain001V09);

    [Benchmark]
    public Task<UiGenerateResponse> Generate() =>
        Client("generate").GenerateAsync(Fixtures.Csv, "payments.csv", MessageTypes.Pain001V09);

    [Benchmark]
    public Task<CheckResponse> Check() => Client("check").CheckAsync(Fixtures.Xml);

    [Benchmark]
    public Task<IReadOnlyList<FaceInfo>> Faces() => Client("faces").FacesAsync();

    [Benchmark]
    public Task<FaceResponse> ProjectFace() => Client("project").ProjectFaceAsync("berlin_group", Fixtures.Xml);

    [Benchmark]
    public Task<Pain001RawResponse> RawRequest() => Client("health").RequestAsync("GET", "/api/v1/health");

    [GlobalCleanup]
    public void Dispose()
    {
        foreach ((HttpClient http, Pain001Client client) in _clients.Values)
        {
            client.Dispose();
            http.Dispose();
        }

        _clients.Clear();
        GC.SuppressFinalize(this);
    }

    private Pain001Client Client(string name) => _clients[name].Client;
}

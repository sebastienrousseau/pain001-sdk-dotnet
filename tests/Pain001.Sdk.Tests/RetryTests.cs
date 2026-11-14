// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Pain001.Sdk.Tests;

/// <summary>The opt-in <see cref="RetryPolicy"/>, against scripted answers.</summary>
public sealed class RetryTests
{
    private const string Healthy = """{"status":"healthy","version":"0.0.88","message":"ok"}""";
    private const string Generated = """{"success":true,"message":"done","filename":"p.xml","xml":"<Document/>"}""";

    private static readonly RetryPolicy Fast = new()
    {
        MaxAttempts = 3,
        InitialDelay = TimeSpan.FromMilliseconds(100),
        MaxDelay = TimeSpan.FromSeconds(1),
    };

    [Fact]
    public async Task WithoutAPolicyA503IsSentOnce()
    {
        using var script = new Script(Answer(503), Answer(200, Healthy));
        using Pain001Client client = script.Client(retry: null);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.HealthAsync());

        Assert.Equal(503, error.StatusCode);
        Assert.Equal(1, script.Calls);
    }

    [Fact]
    public async Task AGetIsRetriedAfter503And429WithBackoff()
    {
        using var script = new Script(Answer(503), Answer(429), Answer(200, Healthy));
        using Pain001Client client = script.Client(Fast);

        HealthResponse health = await client.HealthAsync();

        Assert.Equal("healthy", health.Status);
        Assert.Equal(3, script.Calls);
        Assert.Equal(2, script.Time.Delays.Count);
        Assert.InRange(script.Time.Delays[0], TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));
        Assert.InRange(script.Time.Delays[1], TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task FacesAndARawGetAreRetried()
    {
        using var faces = new Script(Answer(503), Answer(200, "[]"));
        using Pain001Client facesClient = faces.Client(Fast);
        using var raw = new Script(Answer(503), Answer(200, Healthy));
        using Pain001Client rawClient = raw.Client(Fast);

        Assert.Empty(await facesClient.FacesAsync());
        Pain001RawResponse response = await rawClient.RequestAsync("get", "/api/v1/health");

        Assert.Equal(2, faces.Calls);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(2, raw.Calls);
    }

    [Fact]
    public async Task APostIsNeverRetried()
    {
        using var generate = new Script(Answer(503), Answer(200, Generated));
        using Pain001Client generateClient = generate.Client(Fast);
        using var raw = new Script(Answer(503), Answer(200, "{}"));
        using Pain001Client rawClient = raw.Client(Fast);
        using var refused = new Script(Fail(), Answer(200, Generated));
        using Pain001Client refusedClient = refused.Client(Fast);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(
            () => generateClient.GenerateAsync("x", "p.csv", MessageTypes.Pain001V09));
        Pain001RawResponse response = await rawClient.RequestAsync("POST", "/api/v1/check", new JsonObject());
        await Assert.ThrowsAsync<HttpRequestException>(() => refusedClient.CheckAsync("<x/>"));

        Assert.Equal(503, error.StatusCode);
        Assert.Equal(503, response.StatusCode);
        Assert.Equal(1, generate.Calls);
        Assert.Equal(1, raw.Calls);
        Assert.Equal(1, refused.Calls);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(404)]
    public async Task OtherStatusesAreNotRetried(int status)
    {
        using var script = new Script(Answer(status), Answer(200, Healthy));
        using Pain001Client client = script.Client(Fast);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.HealthAsync());

        Assert.Equal(status, error.StatusCode);
        Assert.Equal(1, script.Calls);
    }

    [Fact]
    public async Task TheLastAnswerIsReturnedWhenTheAttemptsRunOut()
    {
        using var script = new Script(Answer(503), Answer(503), Answer(503), Answer(200, Healthy));
        using Pain001Client client = script.Client(Fast);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.HealthAsync());

        Assert.Equal(503, error.StatusCode);
        Assert.Equal(3, script.Calls);
    }

    [Fact]
    public async Task ARetryAfterInSecondsReplacesTheBackoff()
    {
        using var script = new Script(Answer(429, retryAfter: "1"), Answer(200, Healthy));
        using Pain001Client client = script.Client(Fast);

        await client.HealthAsync();

        Assert.Equal([TimeSpan.FromSeconds(1)], script.Time.Delays);
    }

    [Fact]
    public async Task ARetryAfterDateIsCountedFromNow()
    {
        using var script = new Script(Answer(503, retryAfter: "Sat, 14 Nov 2026 12:00:01 GMT"), Answer(200, Healthy));
        using Pain001Client client = script.Client(Fast);

        await client.HealthAsync();

        Assert.Equal([TimeSpan.FromSeconds(1)], script.Time.Delays);
    }

    [Fact]
    public async Task ARetryAfterDateInThePastRetriesAtOnce()
    {
        using var script = new Script(Answer(503, retryAfter: "Sat, 14 Nov 2026 11:00:00 GMT"), Answer(200, Healthy));
        using Pain001Client client = script.Client(Fast);

        await client.HealthAsync();

        Assert.Equal(2, script.Calls);
        Assert.Empty(script.Time.Delays);
    }

    [Fact]
    public async Task ARetryAfterLongerThanMaxDelayReturnsTheAnswer()
    {
        using var script = new Script(Answer(429, retryAfter: "120"), Answer(200, Healthy));
        using Pain001Client client = script.Client(Fast);

        var error = await Assert.ThrowsAsync<Pain001ApiException>(() => client.HealthAsync());

        Assert.Equal(429, error.StatusCode);
        Assert.Equal(1, script.Calls);
        Assert.Empty(script.Time.Delays);
    }

    [Fact]
    public async Task AConnectionErrorIsRetried()
    {
        using var script = new Script(Fail(), Answer(200, Healthy));
        using Pain001Client client = script.Client(Fast);

        HealthResponse health = await client.HealthAsync();

        Assert.Equal("healthy", health.Status);
        Assert.Equal(2, script.Calls);
        Assert.Single(script.Time.Delays);
    }

    [Fact]
    public async Task TheLastConnectionErrorEscapesWhenTheAttemptsRunOut()
    {
        using var script = new Script(Fail(), Fail(), Fail(), Answer(200, Healthy));
        using Pain001Client client = script.Client(Fast);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.HealthAsync());

        Assert.Equal(3, script.Calls);
    }

    [Fact]
    public async Task CancellingDuringTheWaitStopsTheRetries()
    {
        using var cancel = new CancellationTokenSource();
        using var script = new Script(Answer(503), Answer(200, Healthy)) { OnDelay = cancel.Cancel };
        using Pain001Client client = script.Client(Fast);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.HealthAsync(cancel.Token));

        Assert.Equal(1, script.Calls);
    }

    [Fact]
    public async Task TheBackoffDoublesUpToMaxDelay()
    {
        var policy = new RetryPolicy
        {
            MaxAttempts = 5,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(3),
        };

        for (int run = 0; run < 20; run++)
        {
            using var script = new Script(Answer(503), Answer(503), Answer(503), Answer(503), Answer(200, Healthy));
            using Pain001Client client = script.Client(policy);

            await client.HealthAsync();

            List<TimeSpan> delays = script.Time.Delays;
            Assert.Equal(4, delays.Count);
            Assert.InRange(delays[0], TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1));
            Assert.InRange(delays[1], TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            Assert.InRange(delays[2], TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(3));
            Assert.InRange(delays[3], TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(3));
        }
    }

    [Fact]
    public void TheDefaultsAndLimitsAreDocumented()
    {
        var policy = new RetryPolicy();

        Assert.Equal(3, policy.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(200), policy.InitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(5), policy.MaxDelay);
        Assert.Null(new Pain001ClientOptions().Retry);
        Assert.Same(TimeProvider.System, new Pain001ClientOptions().TimeProvider);
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy { MaxAttempts = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy { InitialDelay = TimeSpan.FromTicks(-1) });
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetryPolicy { MaxDelay = TimeSpan.FromTicks(-1) });
        Assert.Throws<ArgumentNullException>(() => new Pain001ClientOptions { TimeProvider = null! });
    }

    private static Func<HttpResponseMessage> Answer(int status, string body = "{}", string? retryAfter = null) => () =>
    {
        var response = new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        if (retryAfter is not null)
        {
            response.Headers.TryAddWithoutValidation("Retry-After", retryAfter);
        }

        return response;
    };

    private static Func<HttpResponseMessage> Fail() =>
        () => throw new HttpRequestException("connection refused");

    /// <summary>Answers each request with the next scripted answer.</summary>
    private sealed class Script(params Func<HttpResponseMessage>[] answers) : IDisposable
    {
        private readonly Queue<Func<HttpResponseMessage>> _answers = new(answers);
        private readonly List<HttpClient> _http = [];

        internal int Calls { get; private set; }

        internal InstantTimeProvider Time { get; } = new();

        internal Action? OnDelay
        {
            get => Time.OnDelay;
            init => Time.OnDelay = value;
        }

        internal Pain001Client Client(RetryPolicy? retry)
        {
            var http = new HttpClient(new StubHandler(_ =>
            {
                Calls++;
                return Task.FromResult(_answers.Dequeue()());
            }));
            _http.Add(http);
            return new Pain001Client(
                http,
                new Pain001ClientOptions
                {
                    BaseAddress = new Uri("http://pain001.test/"),
                    Retry = retry,
                    TimeProvider = Time,
                });
        }

        public void Dispose()
        {
            foreach (HttpClient http in _http)
            {
                http.Dispose();
            }
        }
    }
}

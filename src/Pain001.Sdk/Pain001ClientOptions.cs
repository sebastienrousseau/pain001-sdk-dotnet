// SPDX-License-Identifier: Apache-2.0 OR MIT

namespace Pain001.Sdk;

/// <summary>
/// Where the pain001 server is and how to reach it.
/// </summary>
public sealed class Pain001ClientOptions
{
    /// <summary>
    /// The server's absolute base address, for example
    /// <c>http://127.0.0.1:8000</c>. A path prefix is kept, so a server
    /// mounted under <c>/payments</c> works too.
    /// </summary>
    public Uri BaseAddress { get; set; } = new("http://127.0.0.1:8000");

    /// <summary>
    /// The API key, sent as <c>Authorization: Bearer &lt;key&gt;</c>.
    /// Leave it <see langword="null"/> when the server has no key set.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// How long one request may take, from sending to the last byte of
    /// the response. Defaults to 30 seconds;
    /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> disables it.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The retry policy for <c>GET</c> requests, or <see langword="null"/>
    /// (the default) to send every request once. A <c>POST</c> is never
    /// retried. See <see cref="RetryPolicy"/>.
    /// </summary>
    public RetryPolicy? Retry { get; set; }

    /// <summary>
    /// The clock the retry waits and <c>Retry-After</c> dates are measured
    /// with. Defaults to <see cref="TimeProvider.System"/>; a test can
    /// pass a fake one.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is
    /// <see langword="null"/>.</exception>
    public TimeProvider TimeProvider
    {
        get => _timeProvider;
        set => _timeProvider = value ?? throw new ArgumentNullException(nameof(value));
    }

    private TimeProvider _timeProvider = TimeProvider.System;
}

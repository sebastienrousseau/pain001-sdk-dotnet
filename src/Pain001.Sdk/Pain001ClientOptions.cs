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
}

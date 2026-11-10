// SPDX-License-Identifier: Apache-2.0 OR MIT

namespace Pain001.Sdk.Tests;

/// <summary>Where the live server is, from <c>PAIN001_BASE_URL</c>.</summary>
internal static class LiveServer
{
    /// <summary>Why a server test is skipped, or <see langword="null"/> to run it.</summary>
    internal static string? SkipReason =>
        BaseUrl is null
            ? "PAIN001_BASE_URL is unset: start `pain001 serve` and set it (for example http://127.0.0.1:8000) to run the server tests."
            : null;

    /// <summary>The server's base URL, or <see langword="null"/> when unset.</summary>
    internal static string? BaseUrl
    {
        get
        {
            string? value = Environment.GetEnvironmentVariable("PAIN001_BASE_URL");
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    /// <summary>A client for the live server.</summary>
    internal static Pain001Client CreateClient() =>
        new(new Uri(BaseUrl ?? throw new InvalidOperationException("PAIN001_BASE_URL is unset.")));
}

/// <summary>A fact that runs only against a live server.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ServerFactAttribute : FactAttribute
{
    /// <summary>Skips the test when <c>PAIN001_BASE_URL</c> is unset.</summary>
    public ServerFactAttribute()
    {
        string? reason = LiveServer.SkipReason;
        if (reason is not null)
        {
            Skip = reason;
        }
    }
}

/// <summary>A theory that runs only against a live server.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ServerTheoryAttribute : TheoryAttribute
{
    /// <summary>Skips the test when <c>PAIN001_BASE_URL</c> is unset.</summary>
    public ServerTheoryAttribute()
    {
        string? reason = LiveServer.SkipReason;
        if (reason is not null)
        {
            Skip = reason;
        }
    }
}

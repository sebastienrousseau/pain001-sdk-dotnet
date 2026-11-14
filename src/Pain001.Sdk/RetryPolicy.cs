// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Net.Http.Headers;

namespace Pain001.Sdk;

/// <summary>
/// An opt-in retry policy for the idempotent calls: <c>GET</c> requests
/// only (<see cref="Pain001Client.HealthAsync"/>,
/// <see cref="Pain001Client.FacesAsync"/>, and
/// <see cref="Pain001Client.RequestAsync"/> with <c>GET</c>).
/// </summary>
/// <remarks>
/// <para>
/// Set it on <see cref="Pain001ClientOptions.Retry"/>; without one the
/// client sends every request once. A <c>POST</c> (validate, generate,
/// check, a face projection) is never retried, whatever the policy: a
/// second generate is a second payment file.
/// </para>
/// <para>
/// A <c>GET</c> is sent again when the server answers HTTP 429 or 503,
/// or when the request fails to connect (<see cref="HttpRequestException"/>),
/// up to <see cref="MaxAttempts"/> attempts in all. Between attempts the
/// client waits an exponential backoff with jitter: for attempt
/// <c>n</c>, a random time between half and all of
/// <c>min(MaxDelay, InitialDelay × 2^(n-1))</c>. A <c>Retry-After</c>
/// header on a 429 or 503 replaces the backoff; when it asks for longer
/// than <see cref="MaxDelay"/> the client stops and returns that answer
/// instead of waiting. A timeout or a cancelled request is not retried.
/// </para>
/// </remarks>
public sealed class RetryPolicy
{
    private readonly int _maxAttempts = 3;
    private readonly TimeSpan _initialDelay = TimeSpan.FromMilliseconds(200);
    private readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The attempts in all, the first included. Defaults to 3; 1 means
    /// no retry.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 1.</exception>
    public int MaxAttempts
    {
        get => _maxAttempts;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            _maxAttempts = value;
        }
    }

    /// <summary>The backoff before the second attempt. Defaults to 200 ms.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public TimeSpan InitialDelay
    {
        get => _initialDelay;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            _initialDelay = value;
        }
    }

    /// <summary>
    /// The longest the client waits between two attempts, for the backoff
    /// and for a <c>Retry-After</c>. Defaults to 5 seconds.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public TimeSpan MaxDelay
    {
        get => _maxDelay;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, TimeSpan.Zero);
            _maxDelay = value;
        }
    }

    /// <summary>
    /// The wait before the attempt after <paramref name="attempt"/>, which
    /// answered <paramref name="statusCode"/>, or <see langword="null"/>
    /// to return that answer.
    /// </summary>
    internal TimeSpan? DelayAfter(int attempt, int statusCode, RetryConditionHeaderValue? retryAfter, DateTimeOffset now)
    {
        if (attempt >= MaxAttempts || statusCode is not (429 or 503))
        {
            return null;
        }

        TimeSpan? asked = RetryAfter(retryAfter, now);
        if (asked is null)
        {
            return Backoff(attempt);
        }

        return asked <= MaxDelay ? asked : null;
    }

    /// <summary>The backoff after the failed attempt number <paramref name="attempt"/>.</summary>
    internal TimeSpan Backoff(int attempt)
    {
        double ceiling = Math.Min(MaxDelay.Ticks, InitialDelay.Ticks * Math.Pow(2, attempt - 1));
        double jitter = Random.Shared.NextDouble();
        return TimeSpan.FromTicks((long)((ceiling / 2) + (ceiling / 2 * jitter)));
    }

    private static TimeSpan? RetryAfter(RetryConditionHeaderValue? header, DateTimeOffset now)
    {
        if (header?.Delta is TimeSpan delta)
        {
            return delta;
        }

        if (header?.Date is DateTimeOffset date)
        {
            return date > now ? date - now : TimeSpan.Zero;
        }

        return null;
    }
}

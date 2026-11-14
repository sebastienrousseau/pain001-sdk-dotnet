// SPDX-License-Identifier: Apache-2.0 OR MIT

using Pain001.Sdk;

namespace Pain001.Examples;

/// <summary>
/// What every example shares: the server address from
/// <c>PAIN001_BASE_URL</c> (default <c>http://127.0.0.1:8000</c>), the
/// optional key from <c>PAIN001_API_KEY</c>, and the conformance kit's
/// synthetic fixtures. Run the examples from the repository root.
/// </summary>
internal static class Example
{
    /// <summary>The server's base address.</summary>
    internal static Uri BaseAddress =>
        new(Environment.GetEnvironmentVariable("PAIN001_BASE_URL") ?? "http://127.0.0.1:8000");

    /// <summary>The API key, or <see langword="null"/> when unset.</summary>
    internal static string? ApiKey => Environment.GetEnvironmentVariable("PAIN001_API_KEY");

    /// <summary>A client for the configured server.</summary>
    internal static Pain001Client CreateClient() => new(BaseAddress, ApiKey);

    /// <summary>The text of a fixture in <c>conformance/fixtures/</c>.</summary>
    internal static string Fixture(string name) =>
        File.ReadAllText(Path.Join("conformance", "fixtures", name));

    /// <summary>Prints a failed expectation and returns the exit code 1.</summary>
    internal static int Fail(string message)
    {
        Console.Error.WriteLine("unexpected: " + message);
        return 1;
    }
}

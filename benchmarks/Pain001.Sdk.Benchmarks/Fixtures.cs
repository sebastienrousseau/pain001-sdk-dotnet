// SPDX-License-Identifier: Apache-2.0 OR MIT

namespace Pain001.Sdk.Benchmarks;

/// <summary>The kit's fixtures and the server's recorded answers to them.</summary>
internal static class Fixtures
{
    /// <summary>The kit's five-payment credit-transfer CSV.</summary>
    internal static readonly string Csv = Read("fixtures", "credit-transfer.pain.001.001.09.csv");

    /// <summary>The kit's credit-transfer pain.001.001.09 document.</summary>
    internal static readonly string Xml = Read("fixtures", "credit-transfer.pain.001.001.09.xml");

    /// <summary>A recorded answer, by file name in <c>responses/</c>.</summary>
    internal static string Response(string name) => Read("responses", name);

    private static string Read(string directory, string name) =>
        File.ReadAllText(Path.Join(AppContext.BaseDirectory, directory, name));
}

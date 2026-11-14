// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Text.Json;

namespace Pain001.Sdk.Tests;

/// <summary>
/// Every case in <c>conformance/cases.json</c>, sent through
/// <see cref="Pain001Client.RequestAsync"/> to the server at
/// <c>PAIN001_BASE_URL</c>. Skipped when that variable is unset.
/// </summary>
public sealed class ConformanceTests
{
    public static TheoryData<string> CaseIds()
    {
        var ids = new TheoryData<string>();
        foreach (JsonElement item in ConformanceRunner.LoadCases())
        {
            ids.Add(item.GetProperty("id").GetString()!);
        }

        return ids;
    }

    [ServerTheory]
    [MemberData(nameof(CaseIds))]
    public async Task EveryCaseHoldsAgainstTheServer(string id)
    {
        JsonElement testCase = ConformanceRunner.FindCase(id);
        using Pain001Client client = LiveServer.CreateClient();

        List<string> failures = await ConformanceRunner.RunCaseAsync(client, testCase);

        Assert.Empty(failures);
    }
}

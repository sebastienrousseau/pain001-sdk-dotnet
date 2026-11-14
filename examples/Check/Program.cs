// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// CheckAsync: run the validation ladder (XSD, MDR, scheme profiles,
// Schematron) on an existing pain.001 or pain.008 document.
//
//   dotnet run --project examples/Check

using Pain001.Examples;
using Pain001.Sdk;

using Pain001Client client = Example.CreateClient();

CheckResponse result = await client
    .CheckAsync(Example.Fixture("credit-transfer.pain.001.001.09.xml"))
    .ConfigureAwait(false);

Console.WriteLine($"{result.MessageType}: passed={result.Passed}, {result.Errors} errors, {result.Warnings} warnings");
Console.WriteLine($"rungs: {string.Join(", ", result.Rungs)}");
foreach (CheckFinding finding in result.Findings)
{
    Console.WriteLine($"  {finding.Severity} {finding.Rule} ({finding.Rung}): {finding.Message}");
}

return result.Passed ? 0 : Example.Fail("the fixture document failed its check");

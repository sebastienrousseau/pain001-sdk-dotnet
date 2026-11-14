// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// GenerateAsync: turn a payment file into the XML document. A returned
// result always carries the XML; a refusal raises instead (see
// examples/ErrorHandling).
//
//   dotnet run --project examples/Generate -- [output.xml]

using Pain001.Examples;
using Pain001.Sdk;

string output = args.Length > 0 ? args[0] : "credit-transfer.pain.001.001.09.xml";
using Pain001Client client = Example.CreateClient();

UiGenerateResponse result = await client
    .GenerateAsync(Example.Fixture("credit-transfer.pain.001.001.09.csv"), "payments.csv", MessageTypes.Pain001V09)
    .ConfigureAwait(false);

await File.WriteAllTextAsync(output, result.Xml).ConfigureAwait(false);
Console.WriteLine($"{result.Message}: wrote {result.Xml!.Length} characters to {output} (suggested name {result.Filename})");

return result.Xml.Contains("<NbOfTxs>5</NbOfTxs>", StringComparison.Ordinal) ? 0 : Example.Fail("expected 5 transactions");

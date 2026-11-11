// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// Generates a pain.001.001.09 file from the conformance kit's synthetic
// credit-transfer CSV, through a running `pain001 serve`.
//
//   dotnet run --project examples/Basic -- [input.csv] [output.xml]
//
// The server address comes from PAIN001_BASE_URL (default
// http://127.0.0.1:8000) and the optional key from PAIN001_API_KEY.

using Pain001.Sdk;

string baseUrl = Environment.GetEnvironmentVariable("PAIN001_BASE_URL") ?? "http://127.0.0.1:8000";
string input = args.Length > 0 ? args[0] : Path.Join("conformance", "fixtures", "credit-transfer.pain.001.001.09.csv");
string output = args.Length > 1 ? args[1] : "credit-transfer.pain.001.001.09.xml";

using var client = new Pain001Client(new Uri(baseUrl), Environment.GetEnvironmentVariable("PAIN001_API_KEY"));
string csv = await File.ReadAllTextAsync(input).ConfigureAwait(false);

UiGenerateResponse result = await client
    .GenerateAsync(csv, Path.GetFileName(input), MessageTypes.Pain001V09)
    .ConfigureAwait(false);

if (!result.Success || result.Xml is null)
{
    Console.Error.WriteLine($"Generation failed: {result.Message}");
    return 1;
}

await File.WriteAllTextAsync(output, result.Xml).ConfigureAwait(false);
Console.WriteLine($"Wrote {output}: {result.Message}");
return 0;

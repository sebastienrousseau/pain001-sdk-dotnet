// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// ValidateAsync: check a payment file's rows without generating XML.
// A clean file returns the row counts; a row the server rejects is
// HTTP 400; a scheme rulebook reports its violations in the answer.
//
//   dotnet run --project examples/Validate

using Pain001.Examples;
using Pain001.Sdk;

using Pain001Client client = Example.CreateClient();

ValidationResponse good = await client
    .ValidateAsync(Example.Fixture("credit-transfer.pain.001.001.09.csv"), "payments.csv", MessageTypes.Pain001V09)
    .ConfigureAwait(false);
Console.WriteLine($"credit-transfer: valid={good.IsValid}, {good.ValidRows} of {good.TotalRows} rows valid");

try
{
    await client
        .ValidateAsync(Example.Fixture("bad-iban.pain.001.001.09.csv"), "payments.csv", MessageTypes.Pain001V09)
        .ConfigureAwait(false);
    return Example.Fail("the bad IBAN was accepted");
}
catch (Pain001ApiException error) when (error.StatusCode == 400)
{
    // Detail can quote the input; this one is synthetic, so it is printed.
    Console.WriteLine($"bad-iban: HTTP {error.StatusCode}, {error.Detail.GetString()}");
}

ValidationResponse chaps = await client
    .ValidateAsync(
        Example.Fixture("chaps-no-purpose.pain.001.001.09.csv"),
        "payments.csv",
        MessageTypes.Pain001V09,
        scheme: "uk-chaps")
    .ConfigureAwait(false);
string rule = chaps.SchemeViolations is [var first, ..] ? first.GetProperty("rule").GetString()! : "none";
Console.WriteLine($"chaps-no-purpose with uk-chaps: valid={chaps.IsValid}, first violation {rule}");

return good.IsValid && !chaps.IsValid ? 0 : Example.Fail("unexpected validation result");

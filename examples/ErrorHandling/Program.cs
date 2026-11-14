// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// The three ways a typed call fails, and how to catch them:
//
// - Pain001GenerationRefusedException: generate refused the data
//   (HTTP 422, success false); Response holds the findings.
// - Pain001ApiException: any other status outside 2xx; Detail holds the
//   server's reason (an array of validation errors for a 422).
// - Pain001NoDocumentException: a 2xx generate answer without the XML,
//   which only a server older than pain001 0.0.87 sends. The last part
//   simulates one in process, since a current server never does.
//
// Catch the refusal before Pain001ApiException: it derives from it.
// Messages never quote the findings, which can hold payment data, so
// they are safe to log; Detail and Response are not.
//
//   dotnet run --project examples/ErrorHandling

using System.Net;
using System.Text;
using System.Text.Json;
using Pain001.Examples;
using Pain001.Sdk;

using Pain001Client client = Example.CreateClient();
int failures = 0;

try
{
    await client
        .GenerateAsync(Example.Fixture("bad-currency.pain.001.001.09.csv"), "payments.csv", MessageTypes.Pain001V09)
        .ConfigureAwait(false);
    failures += Example.Fail("the bad currency was accepted");
}
catch (Pain001GenerationRefusedException refused)
{
    Console.WriteLine($"refused: {refused.Message}");
    foreach (RowError row in refused.Response.ValidationErrors ?? [])
    {
        Console.WriteLine($"  row error in {row.Field}: {row.Message}");
    }
}

try
{
    await client
        .GenerateAsync(Example.Fixture("credit-transfer.pain.001.001.09.csv"), "payments.csv", "pain.999.001.01")
        .ConfigureAwait(false);
    failures += Example.Fail("the unknown message type was accepted");
}
catch (Pain001GenerationRefusedException)
{
    failures += Example.Fail("a malformed request is not a refusal");
}
catch (Pain001ApiException error)
{
    Console.WriteLine($"api error: {error.Message}");
    foreach (ValidationError item in error.Detail.Deserialize<List<ValidationError>>() ?? [])
    {
        Console.WriteLine($"  {string.Join('.', item.Location)}: {item.Message}");
    }
}

// A server older than pain001 0.0.87 answers a refusal with HTTP 200.
using var oldServer = new HttpClient(new OldServerHandler());
using var oldClient = new Pain001Client(oldServer, new Pain001ClientOptions { BaseAddress = Example.BaseAddress });
try
{
    await oldClient.GenerateAsync("x", "payments.csv", MessageTypes.Pain001V09).ConfigureAwait(false);
    failures += Example.Fail("an answer without XML was returned");
}
catch (Pain001NoDocumentException noDocument)
{
    Console.WriteLine($"no document: {noDocument.Message} (server said: {noDocument.Response?.Message})");
}

return failures;

/// <summary>Answers like pain001 0.0.86 refusing the data: HTTP 200, no XML.</summary>
internal sealed class OldServerHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"success":false,"message":"Validation failed: 1 row(s) with errors","xml":null}""",
                Encoding.UTF8,
                "application/json"),
        });
}

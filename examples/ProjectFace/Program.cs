// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// ProjectFaceAsync: project a pain.001 document into another format (a
// face) and read what the projection could not carry.
//
//   dotnet run --project examples/ProjectFace -- [face]

using System.Text.Json;
using Pain001.Examples;
using Pain001.Sdk;

string faceName = args.Length > 0 ? args[0] : "berlin_group";
using Pain001Client client = Example.CreateClient();

FaceResponse result = await client
    .ProjectFaceAsync(faceName, Example.Fixture("credit-transfer.pain.001.001.09.xml"))
    .ConfigureAwait(false);

LossReport loss = result.LossReport;
Console.WriteLine($"{result.Face} from {result.Version}: {loss.MappedCount} of {loss.TotalSourceElements} leaves carried, lossless={loss.IsLossless}");
foreach (string path in loss.UnmappedPaths.Take(5))
{
    Console.WriteLine($"  not carried: {path}");
}

Console.WriteLine(JsonSerializer.Serialize(result.Payload, new JsonSerializerOptions { WriteIndented = true }));
return result.Payload.ValueKind == JsonValueKind.Object ? 0 : Example.Fail("the payload is not a JSON object");

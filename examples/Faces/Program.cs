// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// FacesAsync: the formats a pain.001 can be projected into, and the
// side inputs (bindings) each needs.
//
//   dotnet run --project examples/Faces

using Pain001.Examples;
using Pain001.Sdk;

using Pain001Client client = Example.CreateClient();

IReadOnlyList<FaceInfo> faces = await client.FacesAsync().ConfigureAwait(false);
foreach (FaceInfo face in faces)
{
    string bindings = face.Bindings.Count == 0 ? "no bindings" : "bindings: " + string.Join(", ", face.Bindings);
    Console.WriteLine($"{face.Name}: {face.Description} ({bindings})");
}

return faces.Count > 0 ? 0 : Example.Fail("the server lists no faces");

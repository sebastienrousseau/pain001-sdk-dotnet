// SPDX-License-Identifier: Apache-2.0 OR MIT
//
// HealthAsync: is the server up, and which pain001 is it?
//
//   dotnet run --project examples/Health

using Pain001.Examples;
using Pain001.Sdk;

using Pain001Client client = Example.CreateClient();

HealthResponse health = await client.HealthAsync().ConfigureAwait(false);
Console.WriteLine($"{Example.BaseAddress}: {health.Status}, pain001 {health.Version} ({health.Message})");

return health.Status == "healthy" ? 0 : Example.Fail("the server is not healthy");

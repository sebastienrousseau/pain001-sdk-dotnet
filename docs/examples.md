<!-- SPDX-License-Identifier: Apache-2.0 OR MIT -->

# Examples

`examples/` holds one console project per method. Each checks its own
result and exits non-zero when it is wrong; CI runs every one against
`pain001 serve` with `scripts/run-examples.sh`. Run them from the
repository root, with `PAIN001_BASE_URL` pointing at a server (default
`http://127.0.0.1:8000`):

```sh
make build
PAIN001_BASE_URL=http://127.0.0.1:8000 make examples
dotnet run --project examples/Check
```

| Example | Shows |
| :--- | :--- |
| [`Basic`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/Basic/Program.cs) | Health, validation and generation in one go: the README demo |
| [`Health`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/Health/Program.cs) | `HealthAsync` |
| [`Validate`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/Validate/Program.cs) | `ValidateAsync`: counts, a rejected IBAN (HTTP 400), scheme violations |
| [`Generate`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/Generate/Program.cs) | `GenerateAsync`: write the XML |
| [`Check`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/Check/Program.cs) | `CheckAsync`: the validation ladder |
| [`Faces`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/Faces/Program.cs) | `FacesAsync` |
| [`ProjectFace`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/ProjectFace/Program.cs) | `ProjectFaceAsync` and the loss report |
| [`RawRequest`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/RawRequest/Program.cs) | `RequestAsync` |
| [`ErrorHandling`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/ErrorHandling/Program.cs) | Refusal, API error and no-document, in catch order |
| [`Timeouts`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/Timeouts/Program.cs) | `Timeout`, cancellation and `RetryPolicy` |
| [`ApiKey`](https://github.com/sebastienrousseau/pain001-sdk-dotnet/blob/main/examples/ApiKey/Program.cs) | `ApiKey`, and a wrong key refused with HTTP 401 |

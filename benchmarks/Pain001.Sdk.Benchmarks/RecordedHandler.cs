// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Net;
using System.Text;

namespace Pain001.Sdk.Benchmarks;

/// <summary>
/// Answers every request with one recorded server answer, in memory, so a
/// benchmark measures the client alone: building and serializing the
/// request, and reading and decoding the answer.
/// </summary>
internal sealed class RecordedHandler(string body) : HttpMessageHandler
{
    private readonly byte[] _body = Encoding.UTF8.GetBytes(body);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Read the request body as a server would, so its serialization
        // is part of the measured cost.
        if (request.Content is not null)
        {
            await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }

        var content = new ByteArrayContent(_body);
        content.Headers.ContentType = new("application/json");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }
}

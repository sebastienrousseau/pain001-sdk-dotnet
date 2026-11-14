// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Globalization;
using System.Text.Json;

namespace Pain001.Sdk;

/// <summary>
/// The pain001 server answered with a status outside 2xx.
/// </summary>
/// <remarks>
/// The message names the status only. The server's <c>detail</c> can
/// echo the request (an IBAN, a row of the uploaded file), and payment
/// data must not reach a log through an exception message, so it is kept
/// in <see cref="Detail"/> for the caller to inspect. When
/// <see cref="Pain001Client.GenerateAsync"/> is refused for the data, the
/// exception is a <see cref="Pain001GenerationRefusedException"/>, which
/// also carries the findings in typed form.
/// </remarks>
public class Pain001ApiException : Exception
{
    /// <summary>Creates an exception with a default message.</summary>
    public Pain001ApiException()
        : base("The pain001 API returned an error.")
    {
    }

    /// <summary>Creates an exception with the given message.</summary>
    /// <param name="message">What went wrong.</param>
    public Pain001ApiException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and its cause.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public Pain001ApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates an exception for an HTTP error response.</summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="detail">The response's <c>detail</c> field, or the
    /// whole body when it has none.</param>
    public Pain001ApiException(int statusCode, JsonElement detail)
        : this(
            string.Create(
                CultureInfo.InvariantCulture,
                $"The pain001 API returned HTTP {statusCode}; see Detail for the server's reason."),
            statusCode,
            detail)
    {
    }

    /// <summary>
    /// Creates an exception for an HTTP error response, with a message
    /// for a subclass that knows more about the error.
    /// </summary>
    /// <param name="message">What went wrong; never the detail itself.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="detail">The response's <c>detail</c> field, or the
    /// whole body when it has none.</param>
    protected Pain001ApiException(string message, int statusCode, JsonElement detail)
        : base(message)
    {
        StatusCode = statusCode;
        Detail = detail;
    }

    /// <summary>The HTTP status code, or 0 when there was no response.</summary>
    public int StatusCode { get; }

    /// <summary>
    /// The server's <c>detail</c>: a string for most errors, an array of
    /// validation errors for HTTP 422, or the whole body when it has no
    /// <c>detail</c> field (a refused generate call).
    /// <see cref="JsonValueKind.Undefined"/> when the response had no body.
    /// </summary>
    public JsonElement Detail { get; }
}

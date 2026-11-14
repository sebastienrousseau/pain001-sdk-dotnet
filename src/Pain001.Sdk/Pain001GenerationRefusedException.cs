// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Globalization;
using System.Text.Json;

namespace Pain001.Sdk;

/// <summary>
/// The server refused to generate the XML because rows failed validation
/// or a scheme rule: HTTP 422 with <c>success: false</c> (pain001 0.0.87
/// and later).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Pain001ApiException.StatusCode"/> is 422,
/// <see cref="Pain001ApiException.Detail"/> is the whole body (it has no
/// <c>detail</c> field), and <see cref="Response"/> holds the body
/// decoded: the blocking <see cref="UiGenerateResponse.ValidationErrors"/>
/// and <see cref="UiGenerateResponse.SchemeViolations"/>. A caller that
/// catches <see cref="Pain001ApiException"/> still catches this one.
/// </para>
/// <para>
/// A 422 for a malformed request (an unknown message type) has a
/// <c>detail</c> array instead, and stays a plain
/// <see cref="Pain001ApiException"/>. The message gives the status and how
/// many findings there are, never the findings themselves, which can
/// quote payment data.
/// </para>
/// </remarks>
public sealed class Pain001GenerationRefusedException : Pain001ApiException
{
    private const string DefaultMessage = "The pain001 API refused to generate the XML.";

    /// <summary>Creates an exception with a default message.</summary>
    public Pain001GenerationRefusedException()
        : this(DefaultMessage)
    {
    }

    /// <summary>Creates an exception with the given message.</summary>
    /// <param name="message">What went wrong.</param>
    public Pain001GenerationRefusedException(string message)
        : base(message)
    {
        Response = Refused(message);
    }

    /// <summary>Creates an exception with a message and its cause.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public Pain001GenerationRefusedException(string message, Exception innerException)
        : base(message, innerException)
    {
        Response = Refused(message);
    }

    /// <summary>Creates an exception for a refused generate call.</summary>
    /// <param name="statusCode">The HTTP status code, 422.</param>
    /// <param name="body">The whole response body.</param>
    /// <param name="response">The body, decoded.</param>
    public Pain001GenerationRefusedException(int statusCode, JsonElement body, UiGenerateResponse response)
        : base(Describe(statusCode, response), statusCode, body)
    {
        Response = response;
    }

    /// <summary>
    /// The decoded refusal: <see cref="UiGenerateResponse.Success"/> is
    /// <see langword="false"/>, <see cref="UiGenerateResponse.Xml"/> is
    /// <see langword="null"/>, and the findings are in
    /// <see cref="UiGenerateResponse.ValidationErrors"/> and
    /// <see cref="UiGenerateResponse.SchemeViolations"/>.
    /// </summary>
    public UiGenerateResponse Response { get; }

    private static UiGenerateResponse Refused(string message) =>
        new(false, message, null, null, null, null);

    private static string Describe(int statusCode, UiGenerateResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"The pain001 API refused to generate the XML (HTTP {statusCode}): "
            + $"{Count(response.ValidationErrors?.Count, "validation error")}, "
            + $"{Count(response.SchemeViolations?.Count, "scheme violation")}; see Response for the findings.");
    }

    private static string Count(int? count, string noun) =>
        count == 1 ? "1 " + noun : string.Create(CultureInfo.InvariantCulture, $"{count ?? 0} {noun}s");
}

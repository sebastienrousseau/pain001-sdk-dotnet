// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Globalization;

namespace Pain001.Sdk;

/// <summary>
/// A generate call answered with a 2xx status but no XML document:
/// <c>success</c> is not <see langword="true"/>, or <c>xml</c> is missing
/// or empty.
/// </summary>
/// <remarks>
/// pain001 0.0.87 and later answer a refusal with HTTP 422, which
/// <see cref="Pain001Client.GenerateAsync"/> raises as
/// <see cref="Pain001GenerationRefusedException"/>, so a server that
/// sends this is older or broken. It is raised instead of returning a
/// result without a document, so a returned
/// <see cref="UiGenerateResponse"/> always carries the XML. This matches
/// the Go SDK's <c>ErrNoDocument</c> and the C++ SDK's <c>NoDocument</c>.
/// The message names the status only; <see cref="Response"/> holds the
/// decoded body, whose findings can quote payment data.
/// </remarks>
public sealed class Pain001NoDocumentException : Exception
{
    private const string DefaultMessage = "The pain001 API answered generate without an XML document.";

    /// <summary>Creates an exception with a default message.</summary>
    public Pain001NoDocumentException()
        : this(DefaultMessage)
    {
    }

    /// <summary>Creates an exception with the given message.</summary>
    /// <param name="message">What went wrong.</param>
    public Pain001NoDocumentException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a message and its cause.</summary>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public Pain001NoDocumentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates an exception for a generate answer without XML.</summary>
    /// <param name="statusCode">The HTTP status code, in the 2xx range.</param>
    /// <param name="response">The decoded body.</param>
    public Pain001NoDocumentException(int statusCode, UiGenerateResponse response)
        : base(string.Create(
            CultureInfo.InvariantCulture,
            $"The pain001 API answered generate with HTTP {statusCode} but no XML document; see Response."))
    {
        ArgumentNullException.ThrowIfNull(response);
        StatusCode = statusCode;
        Response = response;
    }

    /// <summary>The HTTP status code, or 0 when there was no response.</summary>
    public int StatusCode { get; }

    /// <summary>
    /// The decoded body, or <see langword="null"/> when the exception was
    /// not built from a response.
    /// </summary>
    public UiGenerateResponse? Response { get; }
}

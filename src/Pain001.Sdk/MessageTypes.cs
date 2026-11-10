// SPDX-License-Identifier: Apache-2.0 OR MIT

namespace Pain001.Sdk;

/// <summary>
/// The message types the server's <c>MessageType</c> enum lists. Every
/// method takes the message type as a string, so a newer server's types
/// work before this list knows them.
/// </summary>
public static class MessageTypes
{
    /// <summary>Customer credit transfer initiation, version 03.</summary>
    public const string Pain001V03 = "pain.001.001.03";

    /// <summary>Customer credit transfer initiation, version 04.</summary>
    public const string Pain001V04 = "pain.001.001.04";

    /// <summary>Customer credit transfer initiation, version 05.</summary>
    public const string Pain001V05 = "pain.001.001.05";

    /// <summary>Customer credit transfer initiation, version 06.</summary>
    public const string Pain001V06 = "pain.001.001.06";

    /// <summary>Customer credit transfer initiation, version 07.</summary>
    public const string Pain001V07 = "pain.001.001.07";

    /// <summary>Customer credit transfer initiation, version 08.</summary>
    public const string Pain001V08 = "pain.001.001.08";

    /// <summary>Customer credit transfer initiation, version 09.</summary>
    public const string Pain001V09 = "pain.001.001.09";

    /// <summary>Customer credit transfer initiation, version 10.</summary>
    public const string Pain001V10 = "pain.001.001.10";

    /// <summary>Customer credit transfer initiation, version 11.</summary>
    public const string Pain001V11 = "pain.001.001.11";

    /// <summary>Customer credit transfer initiation, version 12.</summary>
    public const string Pain001V12 = "pain.001.001.12";

    /// <summary>Customer credit transfer initiation, version 13.</summary>
    public const string Pain001V13 = "pain.001.001.13";

    /// <summary>Customer direct debit initiation, version 02.</summary>
    public const string Pain008V02 = "pain.008.001.02";

    /// <summary>Customer direct debit initiation, version 08.</summary>
    public const string Pain008V08 = "pain.008.001.08";

    /// <summary>Every message type above, in the server's order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Pain001V03,
        Pain001V04,
        Pain001V05,
        Pain001V06,
        Pain001V07,
        Pain001V08,
        Pain001V09,
        Pain001V10,
        Pain001V11,
        Pain001V12,
        Pain001V13,
        Pain008V02,
        Pain008V08,
    ];
}

// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Pain001.Sdk.Tests;

/// <summary>The vendored kit is the one core ships, byte for byte.</summary>
public sealed class KitTests
{
    [Fact]
    public void EveryKitFileMatchesItsPinnedHash()
    {
        string pinned = File.ReadAllText(Path.Join(Kit.Root, "SHA256SUMS"));

        Assert.Equal(pinned, ComputeSums(Kit.Root));
    }

    [Fact]
    public void TheKitHasUniqueCaseIdsAndKnownMethods()
    {
        List<JsonElement> cases = ConformanceRunner.LoadCases();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        Assert.NotEmpty(cases);
        foreach (JsonElement item in cases)
        {
            Assert.True(ids.Add(item.GetProperty("id").GetString()!));
            Assert.True(item.GetProperty("method").GetString() is "GET" or "POST");
            Assert.StartsWith("/", item.GetProperty("path").GetString(), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The <c>sha256sum</c> listing of every kit file except SHA256SUMS
    /// itself: two spaces between hash and path, paths sorted ordinally.
    /// </summary>
    private static string ComputeSums(string root)
    {
        var paths = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative != "SHA256SUMS")
            {
                paths.Add(relative);
            }
        }

        paths.Sort(StringComparer.Ordinal);
        var listing = new System.Text.StringBuilder();
        foreach (string relative in paths)
        {
            byte[] hash = SHA256.HashData(File.ReadAllBytes(Path.Join(root, relative)));
            foreach (byte b in hash)
            {
                listing.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            listing.Append("  ").Append(relative).Append('\n');
        }

        return listing.ToString();
    }
}

// SPDX-License-Identifier: Apache-2.0 OR MIT

namespace Pain001.Sdk.Tests;

/// <summary>Finds the vendored conformance kit and reads its files.</summary>
internal static class Kit
{
    private static readonly Lazy<string> RootPath = new(FindRoot);

    /// <summary>The absolute path of the <c>conformance/</c> directory.</summary>
    internal static string Root => RootPath.Value;

    /// <summary>Reads a kit file as UTF-8 text, by its path relative to the kit.</summary>
    internal static string ReadText(string relativePath) =>
        File.ReadAllText(Path.Combine(Root, relativePath), System.Text.Encoding.UTF8);

    private static string FindRoot()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "conformance");
            if (File.Exists(Path.Combine(candidate, "cases.json")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("conformance/cases.json was not found above the test output directory.");
    }
}

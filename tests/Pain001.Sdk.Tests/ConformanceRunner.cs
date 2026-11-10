// SPDX-License-Identifier: Apache-2.0 OR MIT

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pain001.Sdk.Tests;

/// <summary>
/// A port of core's reference runner (<c>tests/test_conformance_kit.py</c>):
/// <c>$file</c> substitution, RFC 6901 JSON Pointer lookup and the
/// <c>status</c>, <c>equals</c>, <c>types</c> and <c>contains</c> checks,
/// with the same failure wording where the languages allow.
/// </summary>
internal static class ConformanceRunner
{
    private static readonly Dictionary<string, Func<JsonElement, bool>> Kinds = new(StringComparer.Ordinal)
    {
        ["string"] = value => value.ValueKind == JsonValueKind.String,
        ["number"] = value => value.ValueKind == JsonValueKind.Number,
        ["integer"] = IsInteger,
        ["boolean"] = value => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        ["array"] = value => value.ValueKind == JsonValueKind.Array,
        ["object"] = value => value.ValueKind == JsonValueKind.Object,
        ["null"] = value => value.ValueKind == JsonValueKind.Null,
    };

    /// <summary>Every case in <c>conformance/cases.json</c>, in file order.</summary>
    internal static List<JsonElement> LoadCases()
    {
        using JsonDocument document = JsonDocument.Parse(Kit.ReadText("cases.json"));
        var cases = new List<JsonElement>();
        foreach (JsonElement item in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            cases.Add(item.Clone());
        }

        return cases;
    }

    /// <summary>The case with this id.</summary>
    internal static JsonElement FindCase(string id)
    {
        foreach (JsonElement item in LoadCases())
        {
            if (item.GetProperty("id").GetString() == id)
            {
                return item;
            }
        }

        throw new KeyNotFoundException("No conformance case has the id " + id + ".");
    }

    /// <summary>Sends one case through <see cref="Pain001Client.RequestAsync"/> and checks it.</summary>
    /// <returns>The failed expectations; empty when the case passes.</returns>
    internal static async Task<List<string>> RunCaseAsync(Pain001Client client, JsonElement testCase)
    {
        string method = testCase.GetProperty("method").GetString()!;
        string path = testCase.GetProperty("path").GetString()!;
        JsonNode? body = testCase.TryGetProperty("body", out JsonElement raw)
            ? Resolve(JsonNode.Parse(raw.GetRawText()))
            : null;
        Pain001RawResponse response = await client.RequestAsync(method, path, body);
        return Evaluate(response, testCase.GetProperty("expect"));
    }

    /// <summary>Replaces every <c>{"$file": ...}</c> with that kit file's text, recursively.</summary>
    internal static JsonNode? Resolve(JsonNode? body)
    {
        switch (body)
        {
            case JsonObject reference when reference.Count == 1 && reference.TryGetPropertyValue("$file", out JsonNode? file):
                return JsonValue.Create(Kit.ReadText(file!.GetValue<string>()));
            case JsonObject obj:
                var copy = new JsonObject();
                foreach (KeyValuePair<string, JsonNode?> pair in obj)
                {
                    copy[pair.Key] = Resolve(pair.Value);
                }

                return copy;
            case JsonArray array:
                var items = new JsonArray();
                foreach (JsonNode? item in array)
                {
                    items.Add(Resolve(item));
                }

                return items;
            default:
                return body?.DeepClone();
        }
    }

    /// <summary>RFC 6901 JSON Pointer lookup; <see langword="false"/> when the target is absent.</summary>
    internal static bool TryPointer(JsonElement document, string pointer, out JsonElement value)
    {
        value = document;
        if (pointer.Length == 0)
        {
            return true;
        }

        string[] tokens = pointer.Split('/');
        for (int i = 1; i < tokens.Length; i++)
        {
            string token = tokens[i]
                .Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            if (!TryStep(value, token, out value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Checks a response against a case's <c>expect</c> object.</summary>
    /// <returns>The failed expectations; empty when the case passes.</returns>
    internal static List<string> Evaluate(Pain001RawResponse response, JsonElement expect)
    {
        var failures = new List<string>();
        int status = expect.GetProperty("status").GetInt32();
        if (response.StatusCode != status)
        {
            failures.Add(string.Create(CultureInfo.InvariantCulture, $"status {response.StatusCode} != {status}"));
            return failures;
        }

        CheckEquals(response.Body, expect, failures);
        CheckTypes(response.Body, expect, failures);
        CheckContains(response.Body, expect, failures);
        return failures;
    }

    /// <summary>Deep JSON equality; numbers compare by value, so <c>1</c> equals <c>1.0</c>.</summary>
    internal static bool JsonEquals(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        return left.ValueKind switch
        {
            JsonValueKind.Object => ObjectEquals(left, right),
            JsonValueKind.Array => ArrayEquals(left, right),
            JsonValueKind.String => left.GetString() == right.GetString(),
            JsonValueKind.Number => NumberEquals(left, right),
            _ => true,
        };
    }

    /// <summary>Whether a value is of one of the kit's type names.</summary>
    internal static bool IsKind(JsonElement value, string kind)
    {
        if (!Kinds.TryGetValue(kind, out Func<JsonElement, bool>? test))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "The kit names an unknown type.");
        }

        return test(value);
    }

    private static bool TryStep(JsonElement current, string token, out JsonElement next)
    {
        next = default;
        if (current.ValueKind == JsonValueKind.Array)
        {
            if (!IsIndex(token, out int index) || index >= current.GetArrayLength())
            {
                return false;
            }

            next = current[index];
            return true;
        }

        return current.ValueKind == JsonValueKind.Object && current.TryGetProperty(token, out next);
    }

    private static bool IsIndex(string token, out int index)
    {
        index = 0;
        foreach (char c in token)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return token.Length > 0 && int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out index);
    }

    private static bool IsInteger(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        // Python reads 5.0 and 5e0 as floats, not ints; so does the kit.
        foreach (char c in value.GetRawText())
        {
            if (c is '.' or 'e' or 'E')
            {
                return false;
            }
        }

        return true;
    }

    private static bool NumberEquals(JsonElement left, JsonElement right)
    {
        if (left.TryGetDecimal(out decimal a) && right.TryGetDecimal(out decimal b))
        {
            return a == b;
        }

        return left.GetDouble().Equals(right.GetDouble());
    }

    private static bool ObjectEquals(JsonElement left, JsonElement right)
    {
        if (left.EnumerateObject().Count() != right.EnumerateObject().Count())
        {
            return false;
        }

        foreach (JsonProperty property in left.EnumerateObject())
        {
            if (!right.TryGetProperty(property.Name, out JsonElement other) || !JsonEquals(property.Value, other))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ArrayEquals(JsonElement left, JsonElement right)
    {
        int length = left.GetArrayLength();
        if (length != right.GetArrayLength())
        {
            return false;
        }

        for (int i = 0; i < length; i++)
        {
            if (!JsonEquals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static void CheckEquals(JsonElement document, JsonElement expect, List<string> failures)
    {
        if (!expect.TryGetProperty("equals", out JsonElement equals))
        {
            return;
        }

        foreach (JsonProperty check in equals.EnumerateObject())
        {
            if (!TryPointer(document, check.Name, out JsonElement value))
            {
                failures.Add(check.Name + " missing");
            }
            else if (!JsonEquals(value, check.Value))
            {
                failures.Add(check.Name + " != " + check.Value.GetRawText());
            }
        }
    }

    private static void CheckTypes(JsonElement document, JsonElement expect, List<string> failures)
    {
        if (!expect.TryGetProperty("types", out JsonElement types))
        {
            return;
        }

        foreach (JsonProperty check in types.EnumerateObject())
        {
            string kind = check.Value.GetString()!;
            if (!TryPointer(document, check.Name, out JsonElement value))
            {
                failures.Add(check.Name + " missing");
            }
            else if (!IsKind(value, kind))
            {
                failures.Add(check.Name + " is not " + kind);
            }
        }
    }

    private static void CheckContains(JsonElement document, JsonElement expect, List<string> failures)
    {
        if (!expect.TryGetProperty("contains", out JsonElement contains))
        {
            return;
        }

        foreach (JsonProperty check in contains.EnumerateObject())
        {
            if (!TryPointer(document, check.Name, out JsonElement value))
            {
                failures.Add(check.Name + " missing");
                continue;
            }

            if (value.ValueKind != JsonValueKind.String)
            {
                failures.Add(check.Name + " is not a string");
                continue;
            }

            string text = value.GetString()!;
            foreach (JsonElement needle in check.Value.EnumerateArray())
            {
                string wanted = needle.GetString()!;
                if (!text.Contains(wanted, StringComparison.Ordinal))
                {
                    failures.Add(check.Name + " lacks " + needle.GetRawText());
                }
            }
        }
    }
}

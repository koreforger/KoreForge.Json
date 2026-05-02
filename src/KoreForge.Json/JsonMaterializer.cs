using Newtonsoft.Json.Linq;

namespace KoreForge.Json;

/// <summary>
/// Recursively walks a <see cref="JToken"/> tree and expands string values
/// that contain valid JSON into parsed objects/arrays. Repeats until stable
/// or <see cref="JsonMaterializerOptions.MaxDepth"/> is reached.
/// </summary>
public static class JsonMaterializer
{
    /// <summary>
    /// Expand escaped JSON strings in <paramref name="token"/>.
    /// Returns a new tree — the original token is not mutated.
    /// </summary>
    public static JToken Expand(JToken token, JsonMaterializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxDepth must be greater than zero.");

        var result = token.DeepClone();
        ExpandRecursive(result, options, currentDepth: 0);
        return result;
    }

    private static void ExpandRecursive(JToken token, JsonMaterializerOptions options, int currentDepth)
    {
        if (currentDepth >= options.MaxDepth)
            return;

        switch (token)
        {
            case JObject obj:
                ExpandObject(obj, options, currentDepth);
                break;

            case JArray arr:
                ExpandArray(arr, options, currentDepth);
                break;
        }
    }

    private static void ExpandObject(JObject obj, JsonMaterializerOptions options, int currentDepth)
    {
        var properties = obj.Properties().ToList();

        foreach (var prop in properties)
        {
            if (prop.Value.Type == JTokenType.String)
            {
                if (ShouldProbe(prop.Name, options))
                {
                    var expanded = TryParseAndExpand(prop.Value.Value<string>()!, options, currentDepth);
                    if (expanded is not null)
                    {
                        prop.Value = expanded;
                        continue;
                    }
                }
            }

            ExpandRecursive(prop.Value, options, currentDepth);
        }
    }

    private static void ExpandArray(JArray arr, JsonMaterializerOptions options, int currentDepth)
    {
        for (int i = 0; i < arr.Count; i++)
        {
            if (arr[i].Type == JTokenType.String)
            {
                var expanded = TryParseAndExpand(arr[i].Value<string>()!, options, currentDepth);
                if (expanded is not null)
                {
                    arr[i] = expanded;
                    continue;
                }
            }

            ExpandRecursive(arr[i], options, currentDepth);
        }
    }

    private static JToken? TryParseAndExpand(string value, JsonMaterializerOptions options, int currentDepth)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.AsSpan().Trim();
        if (trimmed.Length < 2)
            return null;

        // Quick check: must start with { or [ to be valid JSON object/array
        char first = trimmed[0];
        if (first != '{' && first != '[')
            return null;

        try
        {
            var parsed = JToken.Parse(value);

            // Recurse into the newly parsed token to expand any nested escaped JSON
            ExpandRecursive(parsed, options, currentDepth + 1);

            return parsed;
        }
        catch
        {
            return null;
        }
    }

    private static bool ShouldProbe(string propertyName, JsonMaterializerOptions options)
    {
        if (options.FieldHints is null || options.FieldHints.Count == 0)
            return true;

        foreach (var hint in options.FieldHints)
        {
            if (string.Equals(propertyName, hint, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

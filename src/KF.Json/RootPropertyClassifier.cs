using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KF.Json;

/// <summary>
/// High-performance classifier that inspects the root-level properties of a UTF-8 JSON
/// document and matches a property value against an ordered list of compiled regexes.
/// Build once at startup, call <see cref="Classify(byte[])"/> per message.
/// </summary>
public sealed class RootPropertyClassifier
{
    private static readonly JsonReaderOptions ReaderOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private readonly byte[][] _propertyNamesUtf8;
    private readonly List<KeyValuePair<int, Regex>> _matches;

    /// <summary>
    /// Construct a classifier. Encodes property names to UTF-8 once.
    /// </summary>
    /// <param name="propertyNames">Root-level JSON property names to look for (e.g. "Action", "MessageType").</param>
    /// <param name="matches">Ordered list of (key, regex) pairs. First match wins.</param>
    public RootPropertyClassifier(
        string[] propertyNames,
        List<KeyValuePair<int, Regex>> matches)
    {
        ArgumentNullException.ThrowIfNull(propertyNames);
        ArgumentNullException.ThrowIfNull(matches);

        _propertyNamesUtf8 = propertyNames
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .Select(Encoding.UTF8.GetBytes)
            .ToArray();

        _matches = matches;
    }

    /// <summary>
    /// Classify a single JSON message (UTF-8 bytes).
    /// </summary>
    /// <returns>
    ///  0  = no matching root property found in the document.
    /// -1  = root property found but no regex matched the value.
    /// -2  = invalid/empty input or processing error.
    ///  n  = first matching regex's int key.
    /// </returns>
    public int Classify(byte[] inputData)
    {
        if (inputData is null || inputData.Length == 0)
            return -2;

        if (_propertyNamesUtf8.Length == 0)
            return -2;

        try
        {
            var reader = new Utf8JsonReader(inputData, ReaderOptions);

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                return -2;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1)
                {
                    if (IsTargetProperty(ref reader))
                    {
                        if (!reader.Read())
                            return -2;

                        if (reader.TokenType != JsonTokenType.String)
                            return -2;

                        string? value = reader.GetString();
                        if (value is null)
                            return -2;

                        foreach (var kv in _matches)
                        {
                            if (kv.Value?.IsMatch(value) == true)
                                return kv.Key;
                        }

                        return -1;
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }

            return 0;
        }
        catch
        {
            return -2;
        }
    }

    /// <summary>
    /// One-shot convenience method. Less efficient than building a classifier and reusing it.
    /// </summary>
    public static int Classify(
        byte[] inputData,
        string[] propertyNames,
        List<KeyValuePair<int, Regex>> matches)
    {
        try
        {
            var classifier = new RootPropertyClassifier(propertyNames, matches);
            return classifier.Classify(inputData);
        }
        catch
        {
            return -2;
        }
    }

    private bool IsTargetProperty(ref Utf8JsonReader reader)
    {
        foreach (var nameUtf8 in _propertyNamesUtf8)
        {
            if (reader.ValueTextEquals(nameUtf8))
                return true;
        }

        return false;
    }
}

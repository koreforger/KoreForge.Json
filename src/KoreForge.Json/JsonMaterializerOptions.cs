namespace KoreForge.Json;

/// <summary>
/// Configuration options for <see cref="JsonMaterializer"/>.
/// </summary>
public sealed class JsonMaterializerOptions
{
    /// <summary>
    /// Maximum recursion depth for expanding nested escaped JSON strings.
    /// Required — prevents unbounded recursion from malformed or malicious feeds.
    /// Default is 10.
    /// </summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>
    /// Optional list of property names known to contain escaped JSON.
    /// When set, only these fields are probed — all other string values are left untouched.
    /// When null or empty, ALL string values in the document are probed.
    /// </summary>
    public IReadOnlyList<string>? FieldHints { get; set; }
}

using KoreForge.Json;
using KoreForge.Jex;
using Newtonsoft.Json.Linq;

namespace KoreForge.Json.Jex;

/// <summary>
/// Registers the <c>expandJson(path, maxDepth?)</c> function into the JEX function registry.
/// When invoked in a JEX script, it materialises escaped JSON at the resolved path.
/// </summary>
public sealed class ExpandJsonFunction : IJexFunction
{
    private static readonly JsonMaterializerOptions DefaultOptions = new() { MaxDepth = 10 };

    public JexValue Invoke(JexExecutionContext context, IReadOnlyList<JexValue> args)
    {
        if (args.Count == 0)
            return JexValue.Null;

        var path = args[0].AsString();
        if (string.IsNullOrWhiteSpace(path))
            return JexValue.Null;

        var target = context.Input.SelectToken(path);
        if (target is null)
            return JexValue.Null;

        int maxDepth = args.Count > 1 ? (int)args[1].AsNumber() : DefaultOptions.MaxDepth;

        JToken expanded;
        if (target.Type == JTokenType.String)
        {
            expanded = JsonMaterializer.Expand(target, new JsonMaterializerOptions { MaxDepth = maxDepth });
            return JexValue.FromJson(expanded);
        }

        // If already an object/array, still run materialiser in case nested fields are escaped
        expanded = JsonMaterializer.Expand(target, new JsonMaterializerOptions { MaxDepth = maxDepth });
        return JexValue.FromJson(expanded);
    }
}

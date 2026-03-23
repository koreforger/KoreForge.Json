using KF.Jex.Functions;
using KF.Json;
using Newtonsoft.Json.Linq;

namespace KF.Json.Jex;

/// <summary>
/// Registers the <c>expandJson(path, maxDepth?)</c> function into the JEX function registry.
/// When invoked in a JEX script, it materialises escaped JSON at the resolved path.
/// </summary>
public sealed class ExpandJsonFunction : IJexFunction
{
    private static readonly JsonMaterializerOptions DefaultOptions = new() { MaxDepth = 10 };

    public string Name => "expandJson";

    public JToken Invoke(JToken input, JToken[] args)
    {
        if (args.Length == 0)
            return JValue.CreateNull();

        var path = args[0].Value<string>();
        if (string.IsNullOrWhiteSpace(path))
            return JValue.CreateNull();

        var target = input.SelectToken(path);
        if (target is null)
            return JValue.CreateNull();

        int maxDepth = args.Length > 1 ? args[1].Value<int>() : DefaultOptions.MaxDepth;

        if (target.Type == JTokenType.String)
        {
            var expanded = JsonMaterializer.Expand(target, new JsonMaterializerOptions { MaxDepth = maxDepth });
            return expanded;
        }

        // If already an object/array, still run materialiser in case nested fields are escaped
        return JsonMaterializer.Expand(target, new JsonMaterializerOptions { MaxDepth = maxDepth });
    }
}

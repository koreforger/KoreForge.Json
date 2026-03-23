using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace KF.Json.Tests;

public class JsonMaterializerTests
{
    [Fact]
    public void Expands_escaped_json_object_in_string_property()
    {
        var input = JObject.Parse("""
        {
            "Id": 1,
            "Data": "{\"name\":\"test\",\"value\":42}"
        }
        """);

        var result = JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 10 });

        result["Data"]!.Type.Should().Be(JTokenType.Object);
        result["Data"]!["name"]!.Value<string>().Should().Be("test");
        result["Data"]!["value"]!.Value<int>().Should().Be(42);
    }

    [Fact]
    public void Expands_escaped_json_array_in_string_property()
    {
        var input = JObject.Parse("""
        {
            "Items": "[1,2,3]"
        }
        """);

        var result = JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 10 });

        result["Items"]!.Type.Should().Be(JTokenType.Array);
        result["Items"]!.Values<int>().Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Expands_nested_escaped_json_recursively()
    {
        // Build nested escaped JSON properly using JObject construction
        // Level 2 (deepest): {"key":"deep"}
        var level2 = new JObject { ["key"] = "deep" };
        // Level 1: {"Payload": "<escaped level2>"}
        var level1 = new JObject { ["Payload"] = level2.ToString(Newtonsoft.Json.Formatting.None) };
        // Root: {"Data": "<escaped level1>"}
        var input = new JObject { ["Data"] = level1.ToString(Newtonsoft.Json.Formatting.None) };

        var result = JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 10 });

        result["Data"]!.Type.Should().Be(JTokenType.Object);
        result["Data"]!["Payload"]!.Type.Should().Be(JTokenType.Object);
        result["Data"]!["Payload"]!["key"]!.Value<string>().Should().Be("deep");
    }

    [Fact]
    public void Stops_at_max_depth()
    {
        // Build 3-level nested escaped JSON using JObject construction
        // Level 3 (deepest): {"a":"b"}
        var level3 = new JObject { ["a"] = "b" };
        // Level 2: {"Inner": "<escaped level3>"}
        var level2 = new JObject { ["Inner"] = level3.ToString(Newtonsoft.Json.Formatting.None) };
        // Level 1: {"Middle": "<escaped level2>"}
        var level1 = new JObject { ["Middle"] = level2.ToString(Newtonsoft.Json.Formatting.None) };
        // Root: {"Data": "<escaped level1>"}
        var input = new JObject { ["Data"] = level1.ToString(Newtonsoft.Json.Formatting.None) };

        var result = JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 2 });

        // Depth 1: Data expanded. Depth 2: Middle expanded. Inner should remain a string.
        result["Data"]!.Type.Should().Be(JTokenType.Object);
        result["Data"]!["Middle"]!.Type.Should().Be(JTokenType.Object);
        result["Data"]!["Middle"]!["Inner"]!.Type.Should().Be(JTokenType.String);
    }

    [Fact]
    public void Respects_field_hints()
    {
        var input = JObject.Parse("""
        {
            "Data": "{\"x\":1}",
            "Other": "{\"y\":2}"
        }
        """);

        var options = new JsonMaterializerOptions
        {
            MaxDepth = 10,
            FieldHints = ["Data"]
        };

        var result = JsonMaterializer.Expand(input, options);

        result["Data"]!.Type.Should().Be(JTokenType.Object);
        result["Other"]!.Type.Should().Be(JTokenType.String, "Other is not in FieldHints");
    }

    [Fact]
    public void Does_not_mutate_original_token()
    {
        var input = JObject.Parse("""{"Data":"{\"x\":1}"}""");
        var original = input.ToString();

        JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 10 });

        input.ToString().Should().Be(original);
    }

    [Fact]
    public void Leaves_non_json_strings_untouched()
    {
        var input = JObject.Parse("""
        {
            "Name": "just a plain string",
            "Num": "12345",
            "Empty": ""
        }
        """);

        var result = JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 10 });

        result["Name"]!.Type.Should().Be(JTokenType.String);
        result["Num"]!.Type.Should().Be(JTokenType.String);
        result["Empty"]!.Type.Should().Be(JTokenType.String);
    }

    [Fact]
    public void Handles_array_with_escaped_json_elements()
    {
        var input = JArray.Parse("""
        ["{\"a\":1}", "plain", "{\"b\":2}"]
        """);

        var result = JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 10 });

        result[0]!.Type.Should().Be(JTokenType.Object);
        result[1]!.Type.Should().Be(JTokenType.String);
        result[2]!.Type.Should().Be(JTokenType.Object);
    }

    [Fact]
    public void Throws_when_max_depth_is_zero()
    {
        var input = JObject.Parse("{}");
        var act = () => JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 0 });
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Throws_when_token_is_null()
    {
        var act = () => JsonMaterializer.Expand(null!, new JsonMaterializerOptions { MaxDepth = 10 });
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Handles_empty_object()
    {
        var input = JObject.Parse("{}");
        var result = JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 10 });
        result.Type.Should().Be(JTokenType.Object);
    }

    [Fact]
    public void Handles_deeply_nested_objects_without_escaped_strings()
    {
        var input = JObject.Parse("""{"a":{"b":{"c":{"d":"value"}}}}""");
        var result = JsonMaterializer.Expand(input, new JsonMaterializerOptions { MaxDepth = 10 });
        result["a"]!["b"]!["c"]!["d"]!.Value<string>().Should().Be("value");
    }

    [Fact]
    public void Field_hints_are_case_insensitive()
    {
        var input = JObject.Parse("""{"data":"{\"x\":1}"}""");

        var result = JsonMaterializer.Expand(input, new JsonMaterializerOptions
        {
            MaxDepth = 10,
            FieldHints = ["Data"]
        });

        result["data"]!.Type.Should().Be(JTokenType.Object);
    }
}

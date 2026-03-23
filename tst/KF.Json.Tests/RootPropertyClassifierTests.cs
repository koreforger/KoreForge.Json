using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace KF.Json.Tests;

public class RootPropertyClassifierTests
{
    private static readonly RootPropertyClassifier Classifier = new(
        propertyNames: ["Action", "MessageType"],
        matches:
        [
            new(10, new Regex("^OrderCreated$", RegexOptions.Compiled)),
            new(20, new Regex("^OrderUpdated$", RegexOptions.Compiled)),
            new(30, new Regex("^Order.*Cancel", RegexOptions.Compiled)),
        ]);

    private static byte[] Json(string json) => Encoding.UTF8.GetBytes(json);

    [Fact]
    public void Returns_matching_key_when_property_and_regex_match()
    {
        var input = Json("""{"Action":"OrderCreated","Data":{"id":1}}""");
        Classifier.Classify(input).Should().Be(10);
    }

    [Fact]
    public void Returns_second_match_key_when_second_regex_matches()
    {
        var input = Json("""{"Action":"OrderUpdated"}""");
        Classifier.Classify(input).Should().Be(20);
    }

    [Fact]
    public void Returns_regex_key_for_partial_match()
    {
        var input = Json("""{"Action":"OrderBulkCancel"}""");
        Classifier.Classify(input).Should().Be(30);
    }

    [Fact]
    public void Returns_minus_one_when_property_found_but_no_regex_matches()
    {
        var input = Json("""{"Action":"PaymentReceived"}""");
        Classifier.Classify(input).Should().Be(-1);
    }

    [Fact]
    public void Returns_zero_when_no_target_property_found()
    {
        var input = Json("""{"EventType":"OrderCreated"}""");
        Classifier.Classify(input).Should().Be(0);
    }

    [Fact]
    public void Returns_minus_two_for_null_input()
    {
        Classifier.Classify(null!).Should().Be(-2);
    }

    [Fact]
    public void Returns_minus_two_for_empty_input()
    {
        Classifier.Classify([]).Should().Be(-2);
    }

    [Fact]
    public void Returns_minus_two_for_invalid_json()
    {
        var input = Json("not json at all");
        Classifier.Classify(input).Should().Be(-2);
    }

    [Fact]
    public void Returns_minus_two_when_property_value_is_not_string()
    {
        var input = Json("""{"Action":42}""");
        Classifier.Classify(input).Should().Be(-2);
    }

    [Fact]
    public void Skips_large_nested_data_efficiently()
    {
        var largePayload = new string('x', 100_000);
        var input = Json($$"""{"Data":"{{largePayload}}","Action":"OrderCreated"}""");
        Classifier.Classify(input).Should().Be(10);
    }

    [Fact]
    public void Uses_alternate_property_name()
    {
        var input = Json("""{"MessageType":"OrderUpdated"}""");
        Classifier.Classify(input).Should().Be(20);
    }

    [Fact]
    public void Static_classify_returns_correct_result()
    {
        var input = Json("""{"Action":"OrderCreated"}""");
        var result = RootPropertyClassifier.Classify(
            input,
            ["Action"],
            [new(99, new Regex("^OrderCreated$", RegexOptions.Compiled))]);
        result.Should().Be(99);
    }

    [Fact]
    public void Static_classify_returns_minus_two_on_error()
    {
        var result = RootPropertyClassifier.Classify(null!, null!, null!);
        result.Should().Be(-2);
    }

    [Fact]
    public void Handles_trailing_commas()
    {
        var input = Json("""{"Action":"OrderCreated",}""");
        Classifier.Classify(input).Should().Be(10);
    }

    [Fact]
    public void Returns_minus_two_for_json_array_root()
    {
        var input = Json("""[{"Action":"OrderCreated"}]""");
        Classifier.Classify(input).Should().Be(-2);
    }
}

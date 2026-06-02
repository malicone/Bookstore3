using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Bookstore3.WPF.AITools;

internal sealed class LenientNullableInt32JsonConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var intValue))
                    return intValue;
                if (reader.TryGetInt64(out var longValue) &&
                    longValue is >= int.MinValue and <= int.MaxValue)
                    return (int)longValue;
                if (reader.TryGetDouble(out var doubleValue))
                    return (int)doubleValue;
                return null;
            case JsonTokenType.String:
                return ParseFromString(reader.GetString());
            case JsonTokenType.True:
            case JsonTokenType.False:
                return null;
            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
                reader.Skip();
                return null;
            default:
                throw new JsonException($"Unexpected JSON token {reader.TokenType} when parsing a nullable integer.");
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    private static int? ParseFromString(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return value;

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var asDouble))
            return (int)asDouble;

        var yearMatch = Regex.Match(text, @"\b(19|20)\d{2}\b");
        if (yearMatch.Success &&
            int.TryParse(yearMatch.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return value;

        var digitsMatch = Regex.Match(text, @"\d+");
        if (digitsMatch.Success &&
            int.TryParse(digitsMatch.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return value;

        return null;
    }
}

internal sealed class LenientNullableDoubleJsonConverter : JsonConverter<double?>
{
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetDouble(out var value))
                    return value;
                return null;
            case JsonTokenType.String:
                return ParseFromString(reader.GetString());
            case JsonTokenType.True:
            case JsonTokenType.False:
                return null;
            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
                reader.Skip();
                return null;
            default:
                throw new JsonException($"Unexpected JSON token {reader.TokenType} when parsing a nullable number.");
        }
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }

    private static double? ParseFromString(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return value;

        var normalized = Regex.Replace(text, @"[^\d.,\-]", string.Empty);
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return value;

        return null;
    }
}

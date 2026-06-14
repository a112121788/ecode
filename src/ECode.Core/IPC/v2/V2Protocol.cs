using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECode.Core.IPC.V2;

public sealed record V2Request
{
    [JsonPropertyName("protocol")]
    public string Protocol { get; init; } = V2Protocol.ProtocolName;

    [JsonPropertyName("id")]
    public JsonElement? Id { get; init; }

    [JsonPropertyName("method")]
    public string Method { get; init; } = "";

    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

public sealed record V2Response
{
    [JsonPropertyName("protocol")]
    public string Protocol { get; init; } = V2Protocol.ProtocolName;

    [JsonPropertyName("id")]
    public JsonElement? Id { get; init; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public V2Error? Error { get; init; }

    public static V2Response FromResult(JsonElement? id, object? result) => new()
    {
        Id = id,
        Result = result,
    };

    public static V2Response FromError(JsonElement? id, string code, string message) => new()
    {
        Id = id,
        Error = new V2Error(code, message),
    };
}

public sealed record V2Error(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message);

public sealed record V2ParseResult(bool Success, V2Request? Request, V2Response? ErrorResponse);

public static class V2Protocol
{
    public const string ProtocolName = "ecode.v2";

    public static V2ParseResult ParseRequest(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return InvalidRequest(null, "Request body is empty.");

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return InvalidRequest(null, "Request must be a JSON object.");

            var id = root.TryGetProperty("id", out var idElement)
                ? idElement.Clone()
                : (JsonElement?)null;

            if (!root.TryGetProperty("protocol", out var protocolElement) ||
                protocolElement.ValueKind != JsonValueKind.String ||
                !string.Equals(protocolElement.GetString(), ProtocolName, StringComparison.Ordinal))
            {
                return InvalidRequest(id, $"Unsupported protocol. Expected {ProtocolName}.");
            }

            if (!root.TryGetProperty("method", out var methodElement) ||
                methodElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(methodElement.GetString()))
            {
                return InvalidRequest(id, "Request method is required.");
            }

            var request = new V2Request
            {
                Id = id,
                Method = methodElement.GetString()!,
                Params = root.TryGetProperty("params", out var paramsElement)
                    ? paramsElement.Clone()
                    : null,
            };

            return new V2ParseResult(true, request, null);
        }
        catch (JsonException ex)
        {
            return InvalidRequest(null, ex.Message);
        }
    }

    public static bool IsV2Request(string json)
    {
        return ParseRequest(json).Success;
    }

    private static V2ParseResult InvalidRequest(JsonElement? id, string message)
    {
        return new V2ParseResult(
            Success: false,
            Request: null,
            ErrorResponse: V2Response.FromError(id, "invalid_request", message));
    }
}

using System.Text.Json.Serialization;

namespace _03_Proxy;

record OperatorRequest(
    [property: JsonPropertyName("sessionID")] string SessionId,
    [property: JsonPropertyName("msg")] string Msg);

record OperatorResponse(
    [property: JsonPropertyName("msg")] string Msg);

record PackageApiRequest(
    [property: JsonPropertyName("apikey")] string Apikey,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("packageId")] string PackageId,
    [property: JsonPropertyName("destination")] string? Destination = null,
    [property: JsonPropertyName("code")] string? Code = null);

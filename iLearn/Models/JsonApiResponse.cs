using System.Text.Json;

namespace iLearn.Models;

internal static class JsonApiResponse
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<T> DeserializeDataList<T>(string json, string missingDataMessage)
    {
        using var doc = JsonDocument.Parse(json);
        var data = GetRequiredData(doc.RootElement, missingDataMessage);

        if (!data.TryGetProperty("dataList", out var dataList) || dataList.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            throw CreateFriendlyException(doc.RootElement, missingDataMessage);

        return JsonSerializer.Deserialize<List<T>>(dataList.GetRawText(), SerializerOptions) ?? [];
    }

    public static T DeserializeDataObject<T>(string json, string missingDataMessage) where T : new()
    {
        using var doc = JsonDocument.Parse(json);
        var data = GetRequiredData(doc.RootElement, missingDataMessage);

        return JsonSerializer.Deserialize<T>(data.GetRawText(), SerializerOptions) ?? new T();
    }

    private static JsonElement GetRequiredData(JsonElement root, string missingDataMessage)
    {
        if (!root.TryGetProperty("data", out var data) || data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            throw CreateFriendlyException(root, missingDataMessage);

        return data;
    }

    private static InvalidOperationException CreateFriendlyException(JsonElement root, string missingDataMessage)
    {
        var serverMessage = TryGetString(root, "message") ?? TryGetString(root, "msg");
        var suffix = string.IsNullOrWhiteSpace(serverMessage) ? "请重新登录后重试" : serverMessage;
        return new InvalidOperationException($"{missingDataMessage}，{suffix}");
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}

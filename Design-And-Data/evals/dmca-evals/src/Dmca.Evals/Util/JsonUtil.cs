using System.Text.Json;

namespace Dmca.Evals.Util;

public static class JsonUtil
{
    public static T LoadFromFile<T>(string path)
    {
        var json = File.ReadAllText(path);
        var obj = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return obj ?? throw new InvalidOperationException($"Failed to deserialize: {path}");
    }
}

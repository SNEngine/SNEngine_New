using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SNEngine.Data;

/// <summary>
/// Base class for all game data with correct polymorphic JSON support.
/// </summary>
[JsonDerivedType(typeof(CharacterData), typeDiscriminator: "CharacterData")]
public abstract class GameData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Serialize to JSON (with type information)
    /// </summary>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            // Включаем полиморфизм
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Serialize to binary
    /// </summary>
    public byte[] ToBinary()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this);
    }

    /// <summary>
    /// Deserialize from JSON
    /// </summary>
    public static T FromJson<T>(string json) where T : GameData
    {
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException("Failed to deserialize JSON");
    }

    public static T FromBinary<T>(byte[] data) where T : GameData
    {
        return JsonSerializer.Deserialize<T>(data)
            ?? throw new InvalidOperationException("Failed to deserialize binary");
    }
}
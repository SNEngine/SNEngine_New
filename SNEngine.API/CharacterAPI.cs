using SNEngine.Core;
using SNEngine.Core.Components;
using SNEngine.Core.Scenes;
using SNEngine.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace SNEngine.API;

/// <summary>
/// High-level API for managing characters.
/// </summary>
public static class CharacterAPI
{
    private static string _charactersRoot = "assets/characters";

    // Активные персонажи на сцене
    private static readonly Dictionary<string, CharacterObject> _activeCharacters = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Sets root folder for characters.
    /// </summary>
    public static void SetRoot(string rootPath)
    {
        _charactersRoot = rootPath;
        Directory.CreateDirectory(rootPath);
        Debug.Log($"[CharacterAPI] Root folder set to: {rootPath}");
    }

    /// <summary>
    /// Adds or updates a character (creates folder + .sncd file).
    /// </summary>
    public static void AddCharacter(CharacterData character)
    {
        if (character == null || string.IsNullOrEmpty(character.Name))
        {
            Debug.LogError("[CharacterAPI] Character or Name cannot be null");
            return;
        }

        string charFolder = Path.Combine(_charactersRoot, character.Name.ToLower());
        Directory.CreateDirectory(charFolder);

        // Сохраняем бинарные данные
        string sncdPath = Path.Combine(charFolder, $"{character.Name.ToLower()}.sncd");
        string json = character.ToJson();
        File.WriteAllText(sncdPath, json);

        Debug.Log($"[CharacterAPI] Character saved: {character.DisplayName} ({character.Name})");

        // Создаём заглушки для спрайтов
        foreach (var emotion in character.Emotions)
        {
            if (!string.IsNullOrEmpty(emotion.SpritePath))
            {
                string spriteFullPath = Path.Combine(_charactersRoot, emotion.SpritePath);
                string dir = Path.GetDirectoryName(spriteFullPath)!;
                Directory.CreateDirectory(dir);

                if (!File.Exists(spriteFullPath))
                {
                    File.WriteAllBytes(spriteFullPath, new byte[0]);
                    Debug.Log($"[CharacterAPI] Created sprite placeholder: {emotion.SpritePath}");
                }
            }
        }
    }

    /// <summary>
    /// Shows character on the scene.
    /// </summary>
    public static CharacterObject Show(string characterName, string emotion = "happy", float x = 640f, float y = 120f)
    {
        if (SNEngine.CurrentScene == null || SNEngine.Host?.AssetManager == null)
        {
            Debug.LogError("[CharacterAPI] Cannot show character: no active scene");
            return null!;
        }

        if (_activeCharacters.TryGetValue(characterName, out var existing))
        {
            existing.ChangeEmotion(emotion);
            existing.SetPosition(x, y);
            return existing;
        }

        var characterObj = new CharacterObject(SNEngine.Host.AssetManager);
        characterObj.Load(characterName, emotion);
        characterObj.SetPosition(x, y);

        var gameObject = new GameObject { Name = characterName };
        gameObject.AddComponent(characterObj);

        SNEngine.CurrentScene.AddGameObject(gameObject);
        _activeCharacters[characterName] = characterObj;

        Debug.Log($"[CharacterAPI] Showed {characterName} with emotion '{emotion}'");
        return characterObj;
    }

    /// <summary>
    /// Hides character from the scene.
    /// </summary>
    public static void Hide(string characterName)
    {
        if (_activeCharacters.TryGetValue(characterName, out var character))
        {
            character.Alpha = 0f; // можно сделать fade позже
            _activeCharacters.Remove(characterName);
            Debug.Log($"[CharacterAPI] Hidden character: {characterName}");
        }
    }

    /// <summary>
    /// Changes emotion of active character.
    /// </summary>
    public static void ChangeEmotion(string characterName, string emotion)
    {
        if (_activeCharacters.TryGetValue(characterName, out var character))
        {
            character.ChangeEmotion(emotion);
        }
    }

    /// <summary>
    /// Quick example
    /// </summary>
    public static void AddExampleYuki()
    {
        var yuki = new CharacterData
        {
            Name = "yuki",
            DisplayName = "Юки",
            Description = "Весёлая старшеклассница с добрым сердцем",
            DefaultEmotion = "happy",
            Emotions = new[]
            {
                new EmotionData { Name = "happy",     SpritePath = "yuki/happy.png",     Description = "Радостная" },
                new EmotionData { Name = "sad",       SpritePath = "yuki/sad.png",       Description = "Грустная" },
                new EmotionData { Name = "angry",     SpritePath = "yuki/angry.png",     Description = "Злая" },
                new EmotionData { Name = "blush",     SpritePath = "yuki/blush.png",     Description = "Смущённая" },
                new EmotionData { Name = "surprised", SpritePath = "yuki/surprised.png", Description = "Удивлённая" }
            }
        };

        AddCharacter(yuki);
    }
}
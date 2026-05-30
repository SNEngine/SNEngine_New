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
    [JSExclude]
    public static void SetRoot(string rootPath)
    {
        _charactersRoot = rootPath;
        Directory.CreateDirectory(rootPath);
        Debug.Log($"[CharacterAPI] Root folder set to: {rootPath}");
    }

    /// <summary>
    /// Adds or updates a character (creates folder + .sncd file).
    /// </summary>
    [JSExclude]
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
    /// Internal default bottom padding (in pixels) used for automatic grounded positioning
    /// when no explicit bottomPadding is provided.
    /// Hidden from public API.
    /// </summary>
    private const float InternalDefaultBottomPadding = 0f;

    /// <summary>
    /// Shows a character on the scene.
    /// 
    /// This is the main (and recommended) method for scripting.
    /// By default it automatically centers the character horizontally and places it at the bottom
    /// of the screen using the bounce value calculated from the raw image pixels.
    /// 
    /// The character will automatically adapt its position and scale when the window is resized or switched to fullscreen.
    /// </summary>
    /// <param name="characterName">Name of the character (must be loaded via packages or AddCharacter)</param>
    /// <param name="emotion">Emotion/pose to show</param>
    /// <param name="x">Absolute X position. If provided together with horizontalAnchor, horizontalAnchor takes priority.</param>
    /// <param name="y">Absolute Y position. If bottomPadding is also provided, bottomPadding takes priority for vertical placement.</param>
    /// <param name="bottomPadding">
    /// Distance from the bottom of the screen. 
    /// When set (or left null for default behavior), the character is automatically grounded using image bounce.
    /// Default behavior (when nothing vertical is specified) uses automatic bottom placement.
    /// </param>
    /// <param name="horizontalAnchor">
    /// Horizontal anchor from 0.0 (left) to 1.0 (right). 
    /// Common values: 0.5 = center (default), 0.25 = leftish, 0.75 = rightish.
    /// </param>
    /// <param name="horizontalOffset">Pixel offset from the horizontal anchor point.</param>
    /// 
    [JSExclude]

    public static CharacterObject Show(
        string characterName, 
        string emotion = "happy",
        float? x = null,
        float? y = null,
        float? bottomPadding = null,
        float? horizontalAnchor = null,
        float horizontalOffset = 0f)
    {
        if (SNEngine.CurrentScene == null || SNEngine.Host?.AssetManager == null)
        {
            Debug.LogError("[CharacterAPI] Cannot show character: no active scene");
            return null!;
        }

        // === Determine horizontal behavior ===
        bool useAutoHorizontal = horizontalAnchor.HasValue;
        float finalX = x ?? 0f;

        // Default horizontal behavior: center the character
        if (!useAutoHorizontal && !x.HasValue)
        {
            horizontalAnchor = 0.5f;
            horizontalOffset = 0f;
            useAutoHorizontal = true;
        }

        // === Determine vertical behavior ===
        bool useAutoBottom = false;
        float finalBottomPadding = 0f;

        if (bottomPadding.HasValue)
        {
            useAutoBottom = true;
            finalBottomPadding = bottomPadding.Value;
        }
        else if (!y.HasValue)
        {
            // No Y and no explicit bottomPadding → automatic bottom placement with internal default
            useAutoBottom = true;
            finalBottomPadding = InternalDefaultBottomPadding;
        }

        if (_activeCharacters.TryGetValue(characterName, out var existing))
        {
            existing.ChangeEmotion(emotion);

            if (useAutoBottom || useAutoHorizontal)
            {
                float paddingToUse = finalBottomPadding;

                if (useAutoHorizontal)
                {
                    existing.SetAutoPosition(horizontalAnchor!.Value, horizontalOffset, paddingToUse);
                }
                else
                {
                    existing.SetAutoGroundedPosition(finalX, paddingToUse);
                }
            }
            else
            {
                existing.SetPosition(finalX, y!.Value);
            }

            return existing;
        }

        var characterObj = new CharacterObject(SNEngine.Host.AssetManager);
        characterObj.Load(characterName, emotion);

        if (useAutoBottom || useAutoHorizontal)
        {
            if (useAutoHorizontal)
            {
                characterObj.SetAutoPosition(horizontalAnchor!.Value, horizontalOffset, finalBottomPadding);
            }
            else
            {
                characterObj.SetAutoGroundedPosition(finalX, finalBottomPadding);
            }
        }
        else
        {
            characterObj.SetPosition(finalX, y!.Value);
        }

        var gameObject = new GameObject { Name = characterName };
        gameObject.AddComponent(characterObj);

        SNEngine.CurrentScene.AddGameObject(gameObject);
        _activeCharacters[characterName] = characterObj;

        string positioningInfo;
        if (useAutoBottom || useAutoHorizontal)
        {
            string h = useAutoHorizontal ? $"anchor={horizontalAnchor}, offset={horizontalOffset}" : $"x={finalX}";
            string v = useAutoBottom ? $"bottomPadding={finalBottomPadding}" : $"y={y}";
            positioningInfo = $"auto ({h}, {v})";
        }
        else
        {
            positioningInfo = $"manual (x={finalX}, y={y})";
        }

        Debug.Log($"[CharacterAPI] Showed {characterName} ({emotion}) → {positioningInfo}");
        return characterObj;
    }

    /// <summary>
    /// Hides character from the scene.
    /// </summary>
    /// 
    [JSExclude]

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
    [JSExclude]
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
    [JSExclude]
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
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AudioSettingsData
{
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
}

public class PlayerAttackSpriteVersion
{
    public PlayerAttackSpriteVersion(string id, string displayName, string resourcePath)
    {
        Id = id;
        DisplayName = displayName;
        ResourcePath = resourcePath;
    }

    public string Id { get; private set; }
    public string DisplayName { get; private set; }
    public string ResourcePath { get; private set; }
}

public static class PlayerAttackSpriteVersions
{
    public const string DefaultVersionId = "v15";
    public const string DefaultResourcePath = "SpritsProtagoniista/PlayerAttackConsistent_v15/sheets/player_attack_sheet_576x576";

    private static readonly PlayerAttackSpriteVersion[] Versions =
    {
        new PlayerAttackSpriteVersion("v15", "Ataques v15 - suavizado sem corte", DefaultResourcePath)
    };

    public static IReadOnlyList<PlayerAttackSpriteVersion> All
    {
        get { return Versions; }
    }

    public static PlayerAttackSpriteVersion GetDefault()
    {
        PlayerAttackSpriteVersion version;
        return TryGet(DefaultVersionId, out version) ? version : Versions[Versions.Length - 1];
    }

    public static string NormalizeVersionId(string versionId)
    {
        PlayerAttackSpriteVersion version;
        return TryGet(versionId, out version) ? version.Id : DefaultVersionId;
    }

    public static bool IsValid(string versionId)
    {
        PlayerAttackSpriteVersion version;
        return TryGet(versionId, out version);
    }

    public static string GetResourcePath(string versionId)
    {
        PlayerAttackSpriteVersion version;
        return TryGet(versionId, out version) ? version.ResourcePath : DefaultResourcePath;
    }

    public static string GetDisplayName(string versionId)
    {
        PlayerAttackSpriteVersion version;
        return TryGet(versionId, out version) ? version.DisplayName : GetDefault().DisplayName;
    }

    public static bool TryGet(string versionId, out PlayerAttackSpriteVersion version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(versionId))
        {
            return false;
        }

        string normalized = versionId.Trim();
        for (int index = 0; index < Versions.Length; index++)
        {
            PlayerAttackSpriteVersion candidate = Versions[index];
            if (string.Equals(candidate.Id, normalized, StringComparison.OrdinalIgnoreCase))
            {
                version = candidate;
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public class ProgressData
{
    public string lastScene = "TelaInicial";
    public int lives = GameplayBalance.PlayerInitialHealth;
    public int coinsCollected = 0;
    public bool hasSave = false;
    public float playerPositionX = 0f;
    public float playerPositionY = 0f;
    public List<string> completedScenes = new List<string>();
}

[Serializable]
public class GameData
{
    public AudioSettingsData audio = new AudioSettingsData();
    public ProgressData progress = new ProgressData();
    public string selectedAttackSpriteVersion = PlayerAttackSpriteVersions.DefaultVersionId;
    public List<KeybindingEntry> keybindings = new List<KeybindingEntry>();
}

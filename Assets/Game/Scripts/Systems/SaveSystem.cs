using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string SaveFileName = "game-data.json";
    private const string BackupFileName = "game-data.backup.json";

    private static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, SaveFileName); }
    }

    private static string BackupPath
    {
        get { return Path.Combine(Application.persistentDataPath, BackupFileName); }
    }

    public static GameData Load()
    {
        var data = TryLoadFromPath(SavePath);
        if (data != null)
        {
            return data;
        }

        data = TryLoadFromPath(BackupPath);
        if (data != null)
        {
            Debug.LogWarning("Save principal corrompido. Backup restaurado.");
            Save(data);
            return data;
        }

        return CreateDefault();
    }

    public static void Save(GameData data)
    {
        try
        {
            data = Normalize(data);
            Directory.CreateDirectory(Application.persistentDataPath);

            string json = JsonUtility.ToJson(data, true);
            string tempPath = SavePath + ".tmp";

            File.WriteAllText(tempPath, json);

            if (File.Exists(SavePath))
            {
                File.Copy(SavePath, BackupPath, true);
            }

            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }

            File.Move(tempPath, SavePath);
            PlayerPrefs.SetString("last_save_path", SavePath);
            PlayerPrefs.Save();
        }
        catch (Exception exception)
        {
            Debug.LogError("Falha ao salvar dados do jogo: " + exception);
        }
    }

    public static GameData CreateDefault()
    {
        var data = new GameData();
        data.progress.lives = GameplayBalance.PlayerInitialHealth;
        data.keybindings = GameActionDefaults.CreateDefaultKeybindings();
        return data;
    }

    private static GameData TryLoadFromPath(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            var data = JsonUtility.FromJson<GameData>(json);
            return Normalize(data);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Falha ao carregar arquivo de save em " + path + ": " + exception.Message);
            return null;
        }
    }

    private static GameData Normalize(GameData data)
    {
        if (data == null)
        {
            return CreateDefault();
        }

        if (data.audio == null)
        {
            data.audio = new AudioSettingsData();
        }

        data.audio.masterVolume = Mathf.Clamp01(data.audio.masterVolume);
        data.audio.musicVolume = Mathf.Clamp01(data.audio.musicVolume);
        data.audio.sfxVolume = Mathf.Clamp01(data.audio.sfxVolume);

        if (data.progress == null)
        {
            data.progress = new ProgressData();
        }

        if (string.IsNullOrWhiteSpace(data.progress.lastScene))
        {
            data.progress.lastScene = "TelaInicial";
        }

        data.progress.lives = Mathf.Clamp(data.progress.lives, 0, GameplayBalance.PlayerMaxHealth);
        data.progress.coinsCollected = Mathf.Max(0, data.progress.coinsCollected);

        if (data.progress.completedScenes == null)
        {
            data.progress.completedScenes = new System.Collections.Generic.List<string>();
        }

        data.selectedAttackSpriteVersion = PlayerAttackSpriteVersions.NormalizeVersionId(data.selectedAttackSpriteVersion);

        if (data.keybindings == null)
        {
            data.keybindings = new System.Collections.Generic.List<KeybindingEntry>();
        }

        var seenActions = new System.Collections.Generic.HashSet<GameAction>();
        var normalizedBindings = new System.Collections.Generic.List<KeybindingEntry>();
        foreach (KeybindingEntry entry in data.keybindings)
        {
            if (entry == null
                || !Enum.IsDefined(typeof(GameAction), entry.action)
                || entry.action == GameAction.Fire
                || !seenActions.Add(entry.action))
            {
                continue;
            }

            KeyCode keyCode;
            if (string.IsNullOrWhiteSpace(entry.keyCode) || !Enum.TryParse(entry.keyCode, out keyCode) || keyCode == KeyCode.None)
            {
                keyCode = GameActionDefaults.GetDefaultKey(entry.action);
            }
            else if (entry.action != GameAction.Jump && keyCode == KeyCode.Space)
            {
                keyCode = GameActionDefaults.GetDefaultKey(entry.action);
            }

            normalizedBindings.Add(new KeybindingEntry
            {
                action = entry.action,
                keyCode = keyCode.ToString()
            });
        }

        foreach (GameAction action in GameActionDefaults.RebindableActions)
        {
            if (seenActions.Contains(action))
            {
                continue;
            }

            normalizedBindings.Add(new KeybindingEntry
            {
                action = action,
                keyCode = GameActionDefaults.GetDefaultKey(action).ToString()
            });
        }

        data.keybindings = normalizedBindings;
        return data;
    }
}

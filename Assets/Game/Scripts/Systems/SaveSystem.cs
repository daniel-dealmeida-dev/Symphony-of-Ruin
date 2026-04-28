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
        data.keybindings.Add(new KeybindingEntry { action = GameAction.MoveLeft, keyCode = KeyCode.A.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.MoveRight, keyCode = KeyCode.D.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.Jump, keyCode = KeyCode.Space.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.Fire, keyCode = KeyCode.Mouse0.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.Interact, keyCode = KeyCode.E.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.Dash, keyCode = KeyCode.LeftShift.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.Pause, keyCode = KeyCode.Escape.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.Submit, keyCode = KeyCode.Return.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.Cancel, keyCode = KeyCode.Backspace.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.NavigateUp, keyCode = KeyCode.UpArrow.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.NavigateDown, keyCode = KeyCode.DownArrow.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.NavigateLeft, keyCode = KeyCode.LeftArrow.ToString() });
        data.keybindings.Add(new KeybindingEntry { action = GameAction.NavigateRight, keyCode = KeyCode.RightArrow.ToString() });
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
            return data ?? CreateDefault();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Falha ao carregar arquivo de save em " + path + ": " + exception.Message);
            return null;
        }
    }
}

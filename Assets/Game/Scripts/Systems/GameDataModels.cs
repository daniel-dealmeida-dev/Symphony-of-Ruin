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

[Serializable]
public class ProgressData
{
    public string lastScene = "TelaInicial";
    public int lives = 3;
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
    public List<KeybindingEntry> keybindings = new List<KeybindingEntry>();
}

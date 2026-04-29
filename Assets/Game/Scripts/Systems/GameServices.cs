using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class GameServices : MonoBehaviour
{
    private const string ServicesObjectName = "__GameServices";

    public static GameServices Instance { get; private set; }
    public static bool HasInstance { get { return Instance != null; } }

    public static GameServices EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var existing = FindFirstObjectByType<GameServices>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        var servicesObject = new GameObject(ServicesObjectName);
        Instance = servicesObject.AddComponent<GameServices>();
        return Instance;
    }

    public SettingsService Settings { get; private set; }
    public AudioService Audio { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Settings = new SettingsService();
        Audio = new AudioService();

        Settings.Load();
        Audio.Initialize(Settings);

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Audio.RefreshSceneAudioSources();
        ResponsiveCanvasUtility.ConfigureAllCanvases();
    }
}

public class SettingsService
{
    public event Action OnSettingsApplied;

    private GameData data;
    private readonly Dictionary<GameAction, KeyCode> bindings = new Dictionary<GameAction, KeyCode>();

    public GameData Data
    {
        get { return data; }
    }

    public void Load()
    {
        data = SaveSystem.Load();
        bindings.Clear();

        foreach (var entry in data.keybindings)
        {
            if (Enum.TryParse(entry.keyCode, out KeyCode parsedKey))
            {
                bindings[entry.action] = parsedKey;
            }
        }

        foreach (GameAction action in Enum.GetValues(typeof(GameAction)))
        {
            if (!bindings.ContainsKey(action))
            {
                bindings[action] = GetDefaultKey(action);
            }
        }

        PersistBindingsToData();
        Save();
    }

    public void Save()
    {
        if (data == null)
        {
            data = SaveSystem.CreateDefault();
        }

        PersistBindingsToData();
        SaveSystem.Save(data);
        OnSettingsApplied?.Invoke();
    }

    public bool GetButton(GameAction action)
    {
        return Input.GetKey(GetKey(action));
    }

    public bool GetButtonDown(GameAction action)
    {
        return Input.GetKeyDown(GetKey(action));
    }

    public bool GetButtonUp(GameAction action)
    {
        return Input.GetKeyUp(GetKey(action));
    }

    public float GetHorizontal()
    {
        float axis = 0f;
        if (GetButton(GameAction.MoveLeft))
        {
            axis -= 1f;
        }

        if (GetButton(GameAction.MoveRight))
        {
            axis += 1f;
        }

        return Mathf.Clamp(axis, -1f, 1f);
    }

    public KeyCode GetKey(GameAction action)
    {
        return bindings[action];
    }

    public bool TryRebind(GameAction action, KeyCode newKey, out string error)
    {
        foreach (var pair in bindings)
        {
            if (pair.Key != action && pair.Value == newKey)
            {
                error = "A tecla " + newKey + " ja esta em uso por " + pair.Key + ".";
                return false;
            }
        }

        bindings[action] = newKey;
        PersistBindingsToData();
        Save();
        error = string.Empty;
        return true;
    }

    public void SetVolumes(float master, float music, float sfx)
    {
        data.audio.masterVolume = Mathf.Clamp01(master);
        data.audio.musicVolume = Mathf.Clamp01(music);
        data.audio.sfxVolume = Mathf.Clamp01(sfx);
        Save();
    }

    public void SetLastScene(string sceneName)
    {
        data.progress.lastScene = sceneName;
        Save();
    }

    public void SetPlayerPosition(Vector2 position)
    {
        data.progress.playerPositionX = position.x;
        data.progress.playerPositionY = position.y;
        data.progress.hasSave = true;
        Save();
    }

    public Vector2 GetSavedPlayerPosition()
    {
        return new Vector2(data.progress.playerPositionX, data.progress.playerPositionY);
    }

    public bool HasSave()
    {
        return data != null && data.progress.hasSave;
    }

    public void ResetProgress()
    {
        data.progress.lastScene = "PrimeiraFase";
        data.progress.lives = GameplayBalance.PlayerInitialHealth;
        data.progress.coinsCollected = 0;
        data.progress.playerPositionX = 0f;
        data.progress.playerPositionY = 0f;
        data.progress.hasSave = true;
        data.progress.completedScenes.Clear();
        Save();
    }

    public void MarkSceneCompleted(string sceneName)
    {
        if (!data.progress.completedScenes.Contains(sceneName))
        {
            data.progress.completedScenes.Add(sceneName);
            Save();
        }
    }

    public void SetLives(int lives)
    {
        data.progress.lives = Mathf.Max(0, lives);
        Save();
    }

    public void SetCoins(int coins)
    {
        data.progress.coinsCollected = Mathf.Max(0, coins);
        Save();
    }

    private void PersistBindingsToData()
    {
        data.keybindings.Clear();
        foreach (var pair in bindings)
        {
            data.keybindings.Add(new KeybindingEntry
            {
                action = pair.Key,
                keyCode = pair.Value.ToString()
            });
        }
    }

    private static KeyCode GetDefaultKey(GameAction action)
    {
        switch (action)
        {
            case GameAction.MoveLeft: return KeyCode.A;
            case GameAction.MoveRight: return KeyCode.D;
            case GameAction.Jump: return KeyCode.Space;
            case GameAction.Fire: return KeyCode.Mouse0;
            case GameAction.Interact: return KeyCode.E;
            case GameAction.Dash: return KeyCode.LeftShift;
            case GameAction.Pause: return KeyCode.Escape;
            case GameAction.Submit: return KeyCode.Return;
            case GameAction.Cancel: return KeyCode.Backspace;
            case GameAction.NavigateUp: return KeyCode.UpArrow;
            case GameAction.NavigateDown: return KeyCode.DownArrow;
            case GameAction.NavigateLeft: return KeyCode.LeftArrow;
            case GameAction.NavigateRight: return KeyCode.RightArrow;
            case GameAction.RangedFire: return KeyCode.Mouse1;
            default: return KeyCode.None;
        }
    }
}

public class AudioService
{
    private readonly HashSet<ManagedAudioSource> sources = new HashSet<ManagedAudioSource>();
    private SettingsService settings;
    private AudioMixer audioMixer;
    private const string MasterParameter = "MasterVolume";
    private const string MusicParameter = "MusicVolume";
    private const string SfxParameter = "SfxVolume";

    public void Initialize(SettingsService settingsService)
    {
        settings = settingsService;
        settings.OnSettingsApplied += ApplyVolumes;
        RefreshSceneAudioSources();
        ApplyVolumes();
    }

    public void Register(ManagedAudioSource source)
    {
        if (source == null)
        {
            return;
        }

        sources.Add(source);
        ApplyToSource(source);
    }

    public void Unregister(ManagedAudioSource source)
    {
        if (source != null)
        {
            sources.Remove(source);
        }
    }

    public void SetAudioMixer(AudioMixer mixer)
    {
        audioMixer = mixer;
        ApplyVolumes();
    }

    public void RefreshSceneAudioSources()
    {
        var audioSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var source in audioSources)
        {
            var managed = source.GetComponent<ManagedAudioSource>();
            if (managed == null)
            {
                managed = source.gameObject.AddComponent<ManagedAudioSource>();
                managed.Bus = GuessBus(source);
            }

            Register(managed);
        }
    }

    public void ApplyVolumes()
    {
        if (settings == null || settings.Data == null)
        {
            return;
        }

        AudioListener.volume = settings.Data.audio.masterVolume;

        if (audioMixer != null)
        {
            audioMixer.SetFloat(MasterParameter, ToMixerDb(settings.Data.audio.masterVolume));
            audioMixer.SetFloat(MusicParameter, ToMixerDb(settings.Data.audio.musicVolume));
            audioMixer.SetFloat(SfxParameter, ToMixerDb(settings.Data.audio.sfxVolume));
        }

        foreach (var source in sources)
        {
            ApplyToSource(source);
        }
    }

    private void ApplyToSource(ManagedAudioSource source)
    {
        if (source == null || settings == null || settings.Data == null)
        {
            return;
        }

        float channelVolume = settings.Data.audio.sfxVolume;
        if (source.Bus == AudioBus.Music)
        {
            channelVolume = settings.Data.audio.musicVolume;
        }
        else if (source.Bus == AudioBus.Ui)
        {
            channelVolume = settings.Data.audio.sfxVolume;
        }

        source.ApplyVolume(settings.Data.audio.masterVolume, channelVolume);
    }

    private static float ToMixerDb(float normalizedValue)
    {
        return normalizedValue <= 0.0001f ? -80f : Mathf.Log10(normalizedValue) * 20f;
    }

    private static AudioBus GuessBus(AudioSource source)
    {
        string sourceName = source.gameObject.name.ToLowerInvariant();
        if (source.loop || sourceName.Contains("music") || sourceName.Contains("bgm"))
        {
            return AudioBus.Music;
        }

        if (sourceName.Contains("ui") || sourceName.Contains("menu"))
        {
            return AudioBus.Ui;
        }

        return AudioBus.Sfx;
    }
}

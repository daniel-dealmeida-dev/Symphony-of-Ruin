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
            if (Audio != null)
            {
                Audio.Dispose();
            }

            Instance = null;
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
            if (entry.action == GameAction.Fire)
            {
                continue;
            }

            if (Enum.TryParse(entry.keyCode, out KeyCode parsedKey))
            {
                bindings[entry.action] = parsedKey;
            }
        }

        MigrateLegacyDefaultBindings();

        foreach (GameAction action in GameActionDefaults.RebindableActions)
        {
            if (!bindings.ContainsKey(action))
            {
                bindings[action] = GameActionDefaults.GetDefaultKey(action);
            }
        }

        PersistBindingsToData();
        Save();
    }

    public void Save()
    {
        EnsureData();
        PersistBindingsToData();
        SaveSystem.Save(data);
        OnSettingsApplied?.Invoke();
    }

    public bool GetButton(GameAction action)
    {
        return ReadActionKey(action, Input.GetKey);
    }

    public bool GetButtonDown(GameAction action)
    {
        return ReadActionKey(action, Input.GetKeyDown);
    }

    public bool GetButtonUp(GameAction action)
    {
        return ReadActionKey(action, Input.GetKeyUp);
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

    public string SelectedAttackSpriteVersionId
    {
        get
        {
            EnsureData();
            EnsureSelectedAttackSpriteVersion();
            return data.selectedAttackSpriteVersion;
        }
    }

    public string GetSelectedAttackSpriteResourcePath()
    {
        EnsureData();
        EnsureSelectedAttackSpriteVersion();
        return PlayerAttackSpriteVersions.GetResourcePath(data.selectedAttackSpriteVersion);
    }

    public string GetSelectedAttackSpriteDisplayName()
    {
        EnsureData();
        EnsureSelectedAttackSpriteVersion();
        return PlayerAttackSpriteVersions.GetDisplayName(data.selectedAttackSpriteVersion);
    }

    public void SetSelectedAttackSpriteVersion(string versionId)
    {
        EnsureData();
        string normalizedVersionId = PlayerAttackSpriteVersions.NormalizeVersionId(versionId);
        if (data.selectedAttackSpriteVersion == normalizedVersionId)
        {
            return;
        }

        data.selectedAttackSpriteVersion = normalizedVersionId;
        Save();
    }

    public KeyCode GetKey(GameAction action)
    {
        KeyCode keyCode;
        if (bindings.TryGetValue(action, out keyCode))
        {
            return keyCode;
        }

        keyCode = GameActionDefaults.GetDefaultKey(action);
        bindings[action] = keyCode;
        return keyCode;
    }

    public bool TryRebind(GameAction action, KeyCode newKey, out string error)
    {
        if (newKey == KeyCode.None)
        {
            error = "Escolha uma tecla valida.";
            return false;
        }

        if (action != GameAction.Jump && newKey == KeyCode.Space)
        {
            error = "Espaco reservado para pular.";
            return false;
        }

        foreach (var pair in bindings)
        {
            if (pair.Key != action && pair.Value == newKey)
            {
                error = "A tecla " + newKey + " ja esta em uso por " + GameActionDefaults.GetDisplayName(pair.Key) + ".";
                return false;
            }
        }

        bindings[action] = newKey;
        PersistBindingsToData();
        Save();
        error = string.Empty;
        return true;
    }

    private void MigrateLegacyDefaultBindings()
    {
        MigrateBindingIfStillLegacyDefault(GameAction.MoveLeft, KeyCode.A);
        MigrateBindingIfStillLegacyDefault(GameAction.MoveRight, KeyCode.D);
        MigrateBindingIfStillLegacyDefault(GameAction.Fire, KeyCode.Mouse0);
    }

    private void MigrateBindingIfStillLegacyDefault(GameAction action, KeyCode legacyDefault)
    {
        KeyCode currentKey;
        if (bindings.TryGetValue(action, out currentKey) && currentKey == legacyDefault)
        {
            bindings[action] = GameActionDefaults.GetDefaultKey(action);
        }
    }

    private bool ReadActionKey(GameAction action, Func<KeyCode, bool> readKey)
    {
        KeyCode primaryKey = GetKey(action);
        if (readKey(primaryKey))
        {
            return true;
        }

        KeyCode controlCounterpart;
        return action == GameAction.Fire &&
               TryGetControlCounterpart(primaryKey, out controlCounterpart) &&
               readKey(controlCounterpart);
    }

    private static bool TryGetControlCounterpart(KeyCode keyCode, out KeyCode counterpart)
    {
        if (keyCode == KeyCode.LeftControl)
        {
            counterpart = KeyCode.RightControl;
            return true;
        }

        if (keyCode == KeyCode.RightControl)
        {
            counterpart = KeyCode.LeftControl;
            return true;
        }

        counterpart = KeyCode.None;
        return false;
    }

    public void SetVolumes(float master, float music, float sfx)
    {
        EnsureData();
        data.audio.masterVolume = Mathf.Clamp01(master);
        data.audio.musicVolume = Mathf.Clamp01(music);
        data.audio.sfxVolume = Mathf.Clamp01(sfx);
        Save();
    }

    public void SetLastScene(string sceneName)
    {
        EnsureData();
        data.progress.lastScene = sceneName;
        Save();
    }

    public void SetPlayerPosition(Vector2 position)
    {
        EnsureData();
        data.progress.playerPositionX = position.x;
        data.progress.playerPositionY = position.y;
        data.progress.hasSave = true;
        Save();
    }

    public Vector2 GetSavedPlayerPosition()
    {
        EnsureData();
        return new Vector2(data.progress.playerPositionX, data.progress.playerPositionY);
    }

    public bool HasSave()
    {
        return data != null && data.progress.hasSave;
    }

    public void ResetProgress()
    {
        EnsureData();
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
        EnsureData();
        if (!data.progress.completedScenes.Contains(sceneName))
        {
            data.progress.completedScenes.Add(sceneName);
            Save();
        }
    }

    public void SetLives(int lives)
    {
        EnsureData();
        data.progress.lives = Mathf.Max(0, lives);
        Save();
    }

    public void SetCoins(int coins)
    {
        EnsureData();
        data.progress.coinsCollected = Mathf.Max(0, coins);
        Save();
    }

    private void PersistBindingsToData()
    {
        EnsureData();
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

    private void EnsureData()
    {
        if (data == null)
        {
            data = SaveSystem.CreateDefault();
        }

        EnsureSelectedAttackSpriteVersion();
    }

    private void EnsureSelectedAttackSpriteVersion()
    {
        if (data != null && !PlayerAttackSpriteVersions.IsValid(data.selectedAttackSpriteVersion))
        {
            data.selectedAttackSpriteVersion = PlayerAttackSpriteVersions.DefaultVersionId;
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

    public void Dispose()
    {
        if (settings != null)
        {
            settings.OnSettingsApplied -= ApplyVolumes;
            settings = null;
        }

        sources.Clear();
        audioMixer = null;
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

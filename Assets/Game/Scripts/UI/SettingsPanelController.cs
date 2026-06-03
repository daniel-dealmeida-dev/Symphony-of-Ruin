using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelController : MonoBehaviour
{
    private static KeyCode[] cachedKeyCodes;

    private readonly Dictionary<GameAction, Text> keyLabels = new Dictionary<GameAction, Text>();
    private readonly HashSet<Button> wiredKeyButtons = new HashSet<Button>();
    private readonly HashSet<Button> wiredBackButtons = new HashSet<Button>();

    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Text masterValue;
    private Text musicValue;
    private Text sfxValue;
    private Text statusText;
    private GameAction? pendingRebindAction;
    private int listenStartFrame;
    private UnityAction closeAction;
    private bool initialized;

    public static SettingsPanelController CreateRuntimePanel(Transform parent, UnityAction onClose)
    {
        GameObject panel = CreateUiObject("RuntimeSettingsPanel", parent);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        ResponsiveCanvasUtility.StretchRoot(panel.GetComponent<RectTransform>());

        GameObject window = CreateUiObject("Window", panel.transform);
        var windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(760f, 1040f);

        var layout = window.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 36, 36);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text title = CreateText("Title", window.transform, "Configuracoes", 34, TextAnchor.MiddleCenter);
        title.GetComponent<LayoutElement>().preferredHeight = 56f;

        CreateSliderRow(window.transform, "Master", "MasterSlider", "MasterValue");
        CreateSliderRow(window.transform, "Musica", "MusicaSlider", "MusicaValue");
        CreateSliderRow(window.transform, "Efeitos", "EfeitosSlider", "EfeitosValue");

        CreateSectionLabel(window.transform, "Teclas");
        CreateKeyRow(window.transform, GameAction.MoveLeft, "MoveLeftRow", "MoveLeftText");
        CreateKeyRow(window.transform, GameAction.MoveRight, "MoveRightRow", "MoveRightText");
        CreateKeyRow(window.transform, GameAction.Jump, "JumpRow", "JumpText");
        CreateKeyRow(window.transform, GameAction.AttackLine1, "AttackLine1Row", "AttackLine1Text");
        CreateKeyRow(window.transform, GameAction.AttackLine2, "AttackLine2Row", "AttackLine2Text");
        CreateKeyRow(window.transform, GameAction.AttackLine3, "AttackLine3Row", "AttackLine3Text");
        CreateKeyRow(window.transform, GameAction.AttackLine4, "AttackLine4Row", "AttackLine4Text");
        CreateKeyRow(window.transform, GameAction.RangedFire, "RangedFireRow", "RangedFireText");
        CreateKeyRow(window.transform, GameAction.Interact, "InteractRow", "InteractText");
        CreateKeyRow(window.transform, GameAction.Dash, "DashRow", "DashText");
        CreateKeyRow(window.transform, GameAction.Pause, "PauseRow", "PauseText");

        Text status = CreateText("StatusText", window.transform, string.Empty, 20, TextAnchor.MiddleCenter);
        status.color = new Color(0.85f, 0.9f, 1f, 1f);
        status.GetComponent<LayoutElement>().preferredHeight = 36f;

        Button backButton = CreateButton(window.transform, "VoltarButton", "Voltar");
        backButton.GetComponent<LayoutElement>().preferredHeight = 54f;

        var controller = panel.AddComponent<SettingsPanelController>();
        controller.SetCloseAction(onClose);
        return controller;
    }

    public void SetCloseAction(UnityAction onClose)
    {
        closeAction = onClose;
        Initialize();
        WireBackButtons();
    }

    public void ChangeScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void CloseSettings()
    {
        pendingRebindAction = null;
        if (closeAction != null)
        {
            closeAction.Invoke();
            return;
        }

        ChangeScene("TelaInicial");
    }

    private void Awake()
    {
        GameServices.EnsureInstance();
        Initialize();
    }

    private void OnEnable()
    {
        if (initialized)
        {
            RefreshUi();
        }
    }

    private void OnDisable()
    {
        pendingRebindAction = null;
    }

    private void Update()
    {
        if (!pendingRebindAction.HasValue || Time.frameCount <= listenStartFrame)
        {
            return;
        }

        KeyCode pressedKey;
        if (!TryReadPressedKey(out pressedKey))
        {
            return;
        }

        GameAction action = pendingRebindAction.Value;
        pendingRebindAction = null;

        string error;
        if (!GameServices.Instance.Settings.TryRebind(action, pressedKey, out error))
        {
            SetStatus(error);
        }
        else
        {
            SetStatus(GameActionDefaults.GetDisplayName(action) + ": " + pressedKey);
        }

        RefreshUi();
    }

    private void Initialize()
    {
        GameServices.EnsureInstance();
        ResponsiveCanvasUtility.ConfigureAllCanvases();
        CacheControls();
        WireSliders();
        WireKeyButtons();
        if (closeAction != null)
        {
            WireBackButtons();
        }

        initialized = true;
        RefreshUi();
    }

    private void CacheControls()
    {
        masterSlider = FindComponentByName<Slider>("MasterSlider");
        musicSlider = FindComponentByName<Slider>("MusicaSlider");
        sfxSlider = FindComponentByName<Slider>("EfeitosSlider");
        masterValue = FindTextByName("MasterValue");
        musicValue = FindTextByName("MusicaValue");
        sfxValue = FindTextByName("EfeitosValue");
        statusText = FindTextByName("StatusText");

        keyLabels.Clear();
        CacheKeyLabel(GameAction.MoveLeft, "MoveLeftText");
        CacheKeyLabel(GameAction.MoveRight, "MoveRightText");
        CacheKeyLabel(GameAction.Jump, "JumpText");
        CacheKeyLabel(GameAction.AttackLine1, "AttackLine1Text");
        CacheKeyLabel(GameAction.AttackLine2, "AttackLine2Text");
        CacheKeyLabel(GameAction.AttackLine3, "AttackLine3Text");
        CacheKeyLabel(GameAction.AttackLine4, "AttackLine4Text");
        CacheKeyLabel(GameAction.RangedFire, "RangedFireText");
        CacheKeyLabel(GameAction.Interact, "InteractText");
        CacheKeyLabel(GameAction.Dash, "DashText");
        CacheKeyLabel(GameAction.Pause, "PauseText");
    }

    private void CacheKeyLabel(GameAction action, string objectName)
    {
        Text label = FindTextByName(objectName);
        if (label != null)
        {
            keyLabels[action] = label;
        }
    }

    private void WireSliders()
    {
        WireSlider(masterSlider, HandleMasterVolumeChanged);
        WireSlider(musicSlider, HandleMusicVolumeChanged);
        WireSlider(sfxSlider, HandleSfxVolumeChanged);
    }

    private static void WireSlider(Slider slider, UnityAction<float> callback)
    {
        if (slider == null)
        {
            return;
        }

        slider.onValueChanged.RemoveListener(callback);
        slider.onValueChanged.AddListener(callback);
    }

    private void WireKeyButtons()
    {
        WireKeyButton(GameAction.MoveLeft, "MoveLeftRow");
        WireKeyButton(GameAction.MoveRight, "MoveRightRow");
        WireKeyButton(GameAction.Jump, "JumpRow");
        WireKeyButton(GameAction.AttackLine1, "AttackLine1Row");
        WireKeyButton(GameAction.AttackLine2, "AttackLine2Row");
        WireKeyButton(GameAction.AttackLine3, "AttackLine3Row");
        WireKeyButton(GameAction.AttackLine4, "AttackLine4Row");
        WireKeyButton(GameAction.RangedFire, "RangedFireRow");
        WireKeyButton(GameAction.Interact, "InteractRow");
        WireKeyButton(GameAction.Dash, "DashRow");
        WireKeyButton(GameAction.Pause, "PauseRow");
    }

    private void WireKeyButton(GameAction action, string rowName)
    {
        Button button = FindButtonInRow(rowName);
        if (button == null || !wiredKeyButtons.Add(button))
        {
            return;
        }

        GameAction capturedAction = action;
        button.onClick.AddListener(() => StartListeningForKey(capturedAction));
    }

    private void WireBackButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null || button.name != "VoltarButton" || !wiredBackButtons.Add(button))
            {
                continue;
            }

            button.onClick.AddListener(CloseSettings);
        }
    }

    private void StartListeningForKey(GameAction action)
    {
        pendingRebindAction = action;
        listenStartFrame = Time.frameCount;
        Text label;
        if (keyLabels.TryGetValue(action, out label) && label != null)
        {
            label.text = "...";
        }

        SetStatus("Pressione uma tecla para " + GameActionDefaults.GetDisplayName(action) + ".");
    }

    private void HandleMasterVolumeChanged(float value)
    {
        ApplyVolumeSliders();
    }

    private void HandleMusicVolumeChanged(float value)
    {
        ApplyVolumeSliders();
    }

    private void HandleSfxVolumeChanged(float value)
    {
        ApplyVolumeSliders();
    }

    private void ApplyVolumeSliders()
    {
        AudioSettingsData audio = GameServices.Instance.Settings.Data.audio;
        float master = masterSlider != null ? masterSlider.value : audio.masterVolume;
        float music = musicSlider != null ? musicSlider.value : audio.musicVolume;
        float sfx = sfxSlider != null ? sfxSlider.value : audio.sfxVolume;
        GameServices.Instance.Settings.SetVolumes(master, music, sfx);
        RefreshVolumeLabels();
    }

    private void RefreshUi()
    {
        if (!GameServices.HasInstance || GameServices.Instance.Settings == null || GameServices.Instance.Settings.Data == null)
        {
            return;
        }

        AudioSettingsData audio = GameServices.Instance.Settings.Data.audio;
        SetSliderValue(masterSlider, audio.masterVolume);
        SetSliderValue(musicSlider, audio.musicVolume);
        SetSliderValue(sfxSlider, audio.sfxVolume);
        RefreshVolumeLabels();

        foreach (var pair in keyLabels)
        {
            if (pair.Value != null)
            {
                pair.Value.text = GameServices.Instance.Settings.GetKey(pair.Key).ToString();
            }
        }
    }

    private void RefreshVolumeLabels()
    {
        SetVolumeLabel(masterValue, masterSlider);
        SetVolumeLabel(musicValue, musicSlider);
        SetVolumeLabel(sfxValue, sfxSlider);
    }

    private static void SetSliderValue(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }
    }

    private static void SetVolumeLabel(Text label, Slider slider)
    {
        if (label != null && slider != null)
        {
            label.text = Mathf.RoundToInt(slider.value * 100f) + "%";
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private static bool TryReadPressedKey(out KeyCode pressedKey)
    {
        foreach (KeyCode keyCode in GetKeyCodes())
        {
            if (keyCode == KeyCode.None)
            {
                continue;
            }

            if (Input.GetKeyDown(keyCode))
            {
                pressedKey = keyCode;
                return true;
            }
        }

        pressedKey = KeyCode.None;
        return false;
    }

    private static KeyCode[] GetKeyCodes()
    {
        if (cachedKeyCodes == null)
        {
            cachedKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        }

        return cachedKeyCodes;
    }

    private Button FindButtonInRow(string rowName)
    {
        Transform row = FindChildRecursive(transform, rowName);
        if (row == null)
        {
            return null;
        }

        Button[] buttons = row.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.name == "KeyButton")
            {
                return button;
            }
        }

        return buttons.Length > 0 ? buttons[0] : null;
    }

    private Text FindTextByName(string objectName)
    {
        Transform target = FindChildRecursive(transform, objectName);
        return target != null ? target.GetComponent<Text>() : null;
    }

    private T FindComponentByName<T>(string objectName) where T : Component
    {
        Transform target = FindChildRecursive(transform, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int index = 0; index < root.childCount; index++)
        {
            Transform child = FindChildRecursive(root.GetChild(index), childName);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }

    private static void CreateSectionLabel(Transform parent, string label)
    {
        Text text = CreateText(label + "Section", parent, label, 24, TextAnchor.MiddleLeft);
        text.color = new Color(0.75f, 0.82f, 0.95f, 1f);
        text.GetComponent<LayoutElement>().preferredHeight = 34f;
    }

    private static void CreateSliderRow(Transform parent, string label, string sliderName, string valueName)
    {
        GameObject row = CreateUiObject(label + "Row", parent);
        ConfigureHorizontalRow(row, 46f);

        Text labelText = CreateText(label + "Label", row.transform, label, 22, TextAnchor.MiddleLeft);
        labelText.GetComponent<LayoutElement>().preferredWidth = 150f;

        Slider slider = CreateSlider(row.transform, sliderName);
        slider.GetComponent<LayoutElement>().preferredWidth = 420f;

        Text valueText = CreateText(valueName, row.transform, "100%", 20, TextAnchor.MiddleRight);
        valueText.GetComponent<LayoutElement>().preferredWidth = 80f;
    }

    private static void CreateKeyRow(Transform parent, GameAction action, string rowName, string textName)
    {
        GameObject row = CreateUiObject(rowName, parent);
        ConfigureHorizontalRow(row, 44f);

        Text label = CreateText(action + "Label", row.transform, GameActionDefaults.GetDisplayName(action), 20, TextAnchor.MiddleLeft);
        label.GetComponent<LayoutElement>().preferredWidth = 380f;

        Button button = CreateButton(row.transform, "KeyButton", string.Empty);
        button.GetComponent<LayoutElement>().preferredWidth = 210f;
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.name = textName;
        }
    }

    private static void ConfigureHorizontalRow(GameObject row, float height)
    {
        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 16f;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        var layoutElement = row.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
    }

    private static Slider CreateSlider(Transform parent, string name)
    {
        GameObject sliderObject = CreateUiObject(name, parent);
        var layoutElement = sliderObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 34f;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        GameObject background = CreateUiObject("Background", sliderObject.transform);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.18f, 0.22f, 0.3f, 1f);
        Stretch(background.GetComponent<RectTransform>(), new Vector2(0f, 0.32f), new Vector2(1f, 0.68f));

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));

        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.45f, 0.66f, 1f, 1f);
        Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        Stretch(handleArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject handle = CreateUiObject("Handle", handleArea.transform);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.95f, 0.98f, 1f, 1f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(24f, 24f);

        slider.targetGraphic = handleImage;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        return slider;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.25f, 0.35f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 44f;

        Text text = CreateText("Label", buttonObject.transform, label, 20, TextAnchor.MiddleCenter);
        ResponsiveCanvasUtility.StretchRoot(text.GetComponent<RectTransform>());
        return button;
    }

    private static Text CreateText(string name, Transform parent, string textValue, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = CreateUiObject(name, parent);
        Text text = textObject.AddComponent<Text>();
        text.text = textValue;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        var layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 32f;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}

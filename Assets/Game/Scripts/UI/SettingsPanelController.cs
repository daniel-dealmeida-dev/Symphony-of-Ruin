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
        // Painel de fundo fullscreen
        GameObject panel = CreateUiObject("RuntimeSettingsPanel", parent);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);
        ResponsiveCanvasUtility.StretchRoot(panel.GetComponent<RectTransform>());

        // Janela centralizada
        GameObject window = CreateUiObject("Window", panel.transform);
        var windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(780f, 900f);

        // Layout vertical da janela (header + scroll + footer)
        var windowLayout = window.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(0, 0, 0, 0);
        windowLayout.spacing = 0f;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        // Header: titulo
        GameObject header = CreateUiObject("Header", window.transform);
        var headerLayout = header.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 70f;
        headerLayout.flexibleHeight = 0f;
        var headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.06f, 0.08f, 0.12f, 1f);
        Text title = CreateText("Title", header.transform, "Configuracoes", 32, TextAnchor.MiddleCenter);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Área scrollável
        GameObject scrollObj = CreateUiObject("ScrollView", window.transform);
        var scrollLayoutElem = scrollObj.AddComponent<LayoutElement>();
        scrollLayoutElem.flexibleHeight = 1f;
        scrollLayoutElem.minHeight = 100f;

        var scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewport = CreateUiObject("Viewport", scrollObj.transform);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        GameObject content = CreateUiObject("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(40, 40, 20, 20);
        contentLayout.spacing = 6f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        var contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = vpRect;
        scrollRect.content = contentRect;

        // Seção Audio
        CreateSectionLabel(content.transform, "Audio");
        CreateSliderRow(content.transform, "Master", "MasterSlider", "MasterValue");
        CreateSliderRow(content.transform, "Musica", "MusicaSlider", "MusicaValue");
        CreateSliderRow(content.transform, "Efeitos", "EfeitosSlider", "EfeitosValue");

        CreateSpacer(content.transform, 10f);

        // Seção Teclas
        CreateSectionLabel(content.transform, "Teclas");
        CreateKeyRow(content.transform, GameAction.MoveLeft, "MoveLeftRow", "MoveLeftText");
        CreateKeyRow(content.transform, GameAction.MoveRight, "MoveRightRow", "MoveRightText");
        CreateKeyRow(content.transform, GameAction.Jump, "JumpRow", "JumpText");
        CreateKeyRow(content.transform, GameAction.AttackLine1, "AttackLine1Row", "AttackLine1Text");
        CreateKeyRow(content.transform, GameAction.AttackLine2, "AttackLine2Row", "AttackLine2Text");
        CreateKeyRow(content.transform, GameAction.AttackLine3, "AttackLine3Row", "AttackLine3Text");
        CreateKeyRow(content.transform, GameAction.AttackLine4, "AttackLine4Row", "AttackLine4Text");
        CreateKeyRow(content.transform, GameAction.RangedFire, "RangedFireRow", "RangedFireText");
        CreateKeyRow(content.transform, GameAction.Interact, "InteractRow", "InteractText");
        CreateKeyRow(content.transform, GameAction.Dash, "DashRow", "DashText");
        CreateKeyRow(content.transform, GameAction.Pause, "PauseRow", "PauseText");

        // Status
        CreateSpacer(content.transform, 4f);
        Text status = CreateText("StatusText", content.transform, string.Empty, 18, TextAnchor.MiddleCenter);
        status.color = new Color(0.85f, 0.9f, 1f, 1f);
        status.GetComponent<LayoutElement>().preferredHeight = 30f;

        // Footer: botao Voltar + restaurar padroes
        GameObject footer = CreateUiObject("Footer", window.transform);
        var footerLayout = footer.AddComponent<LayoutElement>();
        footerLayout.preferredHeight = 72f;
        footerLayout.flexibleHeight = 0f;
        var footerBg = footer.AddComponent<Image>();
        footerBg.color = new Color(0.06f, 0.08f, 0.12f, 1f);

        var footerRow = footer.AddComponent<HorizontalLayoutGroup>();
        footerRow.padding = new RectOffset(40, 40, 12, 12);
        footerRow.spacing = 16f;
        footerRow.childAlignment = TextAnchor.MiddleCenter;
        footerRow.childControlWidth = false;
        footerRow.childControlHeight = true;
        footerRow.childForceExpandWidth = false;
        footerRow.childForceExpandHeight = true;

        Button restoreButton = CreateButton(footer.transform, "RestaurarPadraoButton", "Restaurar Padroes");
        restoreButton.GetComponent<LayoutElement>().preferredWidth = 260f;
        restoreButton.GetComponent<Image>().color = new Color(0.25f, 0.18f, 0.12f, 1f);

        Button backButton = CreateButton(footer.transform, "VoltarButton", "Voltar");
        backButton.GetComponent<LayoutElement>().preferredWidth = 260f;

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

    public void RestoreDefaults()
    {
        GameServices.Instance.Settings.ResetProgress();
        RefreshUi();
        SetStatus("Teclas restauradas para o padrao.");
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
        WireRestoreButton();
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
        if (slider == null) return;
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
        if (button == null || !wiredKeyButtons.Add(button)) return;
        GameAction capturedAction = action;
        button.onClick.AddListener(() => StartListeningForKey(capturedAction));
    }

    private void WireBackButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null || button.name != "VoltarButton" || !wiredBackButtons.Add(button)) continue;
            button.onClick.AddListener(CloseSettings);
        }
    }

    private void WireRestoreButton()
    {
        Transform t = FindChildRecursive(transform, "RestaurarPadraoButton");
        if (t == null) return;
        Button btn = t.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveListener(RestoreDefaults);
        btn.onClick.AddListener(RestoreDefaults);
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

    private void HandleMasterVolumeChanged(float value) { ApplyVolumeSliders(); }
    private void HandleMusicVolumeChanged(float value) { ApplyVolumeSliders(); }
    private void HandleSfxVolumeChanged(float value) { ApplyVolumeSliders(); }

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
        if (!GameServices.HasInstance || GameServices.Instance.Settings == null || GameServices.Instance.Settings.Data == null) return;

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
        if (slider != null) slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }

    private static void SetVolumeLabel(Text label, Slider slider)
    {
        if (label != null && slider != null)
            label.text = Mathf.RoundToInt(slider.value * 100f) + "%";
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private static bool TryReadPressedKey(out KeyCode pressedKey)
    {
        foreach (KeyCode keyCode in GetKeyCodes())
        {
            if (keyCode == KeyCode.None) continue;
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
            cachedKeyCodes = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        return cachedKeyCodes;
    }

    private Button FindButtonInRow(string rowName)
    {
        Transform row = FindChildRecursive(transform, rowName);
        if (row == null) return null;
        Button[] buttons = row.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.name == "KeyButton") return button;
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
        if (root == null) return null;
        if (root.name == childName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = FindChildRecursive(root.GetChild(i), childName);
            if (child != null) return child;
        }
        return null;
    }

    // ── Helpers de criação de UI ──────────────────────────────────────────

    private static void CreateSectionLabel(Transform parent, string label)
    {
        // Linha separadora
        GameObject divider = CreateUiObject(label + "Divider", parent);
        var dividerImage = divider.AddComponent<Image>();
        dividerImage.color = new Color(0.35f, 0.45f, 0.65f, 0.4f);
        var dividerLayout = divider.AddComponent<LayoutElement>();
        dividerLayout.preferredHeight = 1f;

        Text text = CreateText(label + "Section", parent, label, 22, TextAnchor.MiddleLeft);
        text.color = new Color(0.6f, 0.78f, 1f, 1f);
        text.fontStyle = FontStyle.Bold;
        text.GetComponent<LayoutElement>().preferredHeight = 36f;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        GameObject spacer = CreateUiObject("Spacer", parent);
        var layout = spacer.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
    }

    private static void CreateSliderRow(Transform parent, string label, string sliderName, string valueName)
    {
        GameObject row = CreateUiObject(label + "Row", parent);
        ConfigureHorizontalRow(row, 44f);

        Text labelText = CreateText(label + "Label", row.transform, label, 20, TextAnchor.MiddleLeft);
        labelText.GetComponent<LayoutElement>().preferredWidth = 120f;

        Slider slider = CreateSlider(row.transform, sliderName);
        var sliderLayout = slider.GetComponent<LayoutElement>();
        sliderLayout.preferredWidth = -1f;
        sliderLayout.flexibleWidth = 1f;

        Text valueText = CreateText(valueName, row.transform, "100%", 18, TextAnchor.MiddleRight);
        valueText.GetComponent<LayoutElement>().preferredWidth = 56f;
    }

    private static void CreateKeyRow(Transform parent, GameAction action, string rowName, string textName)
    {
        GameObject row = CreateUiObject(rowName, parent);
        ConfigureHorizontalRow(row, 42f);

        // Fundo alternado leve para legibilidade
        var rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(1f, 1f, 1f, 0.03f);

        Text label = CreateText(action + "Label", row.transform, GameActionDefaults.GetDisplayName(action), 19, TextAnchor.MiddleLeft);
        label.GetComponent<LayoutElement>().flexibleWidth = 1f;

        Button button = CreateButton(row.transform, "KeyButton", string.Empty);
        button.GetComponent<LayoutElement>().preferredWidth = 180f;
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null) buttonText.name = textName;
    }

    private static void ConfigureHorizontalRow(GameObject row, float height)
    {
        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
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
        layoutElement.preferredHeight = 32f;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        GameObject background = CreateUiObject("Background", sliderObject.transform);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.18f, 0.22f, 0.3f, 1f);
        Stretch(background.GetComponent<RectTransform>(), new Vector2(0f, 0.3f), new Vector2(1f, 0.7f));

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 0.25f), new Vector2(1f, 0.75f));

        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.35f, 0.6f, 1f, 1f);
        Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        Stretch(handleArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject handle = CreateUiObject("Handle", handleArea.transform);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.95f, 0.98f, 1f, 1f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(20f, 20f);

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
        layoutElement.preferredHeight = 42f;

        Text text = CreateText("Label", buttonObject.transform, label, 19, TextAnchor.MiddleCenter);
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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
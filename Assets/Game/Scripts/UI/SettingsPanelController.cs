using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelController : MonoBehaviour
{
    private readonly Dictionary<GameAction, Text> bindingLabels = new Dictionary<GameAction, Text>();
    private readonly Dictionary<string, Text> volumeValueLabels = new Dictionary<string, Text>();

    private Action onCloseRequested;
    private GameAction? waitingRebind;
    private Text feedbackText;
    private Text titleText;
    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;

    public bool IsVisible
    {
        get { return gameObject.activeSelf; }
    }

    public static SettingsPanelController CreateOrGet(string canvasName)
    {
        var existing = GameObject.Find(canvasName);
        if (existing != null && existing.TryGetComponent(out SettingsPanelController existingController))
        {
            existingController.EnsureCanvasRoot();
            return existingController;
        }

        var root = new GameObject(
            canvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(SettingsPanelController));

        var controller = root.GetComponent<SettingsPanelController>();
        controller.EnsureCanvasRoot();
        return controller;
    }

    public void Show(string title, Action onClose)
    {
        onCloseRequested = onClose;
        EnsureCanvasRoot();
        BuildIfNeeded();
        Refresh();
        SetTitle(title);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        waitingRebind = null;
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        EnsureCanvasRoot();
    }

    private void Update()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        if (waitingRebind.HasValue)
        {
            CaptureRebindInput();
            return;
        }

        if (GameServices.Instance.Settings.GetButtonDown(GameAction.Cancel))
        {
            Close();
        }
    }

    private void EnsureCanvasRoot()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        ResponsiveCanvasUtility.StretchRoot(GetComponent<RectTransform>());
    }

    private void BuildIfNeeded()
    {
        if (transform.childCount > 0 && titleText != null)
        {
            return;
        }

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        bindingLabels.Clear();
        volumeValueLabels.Clear();

        var overlay = CreateUiObject("Overlay", transform);
        var overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0.03f, 0.05f, 0.08f, 0.82f);
        var overlayRect = overlay.GetComponent<RectTransform>();
        ResponsiveCanvasUtility.StretchRoot(overlayRect);

        var window = CreateUiObject("Window", transform);
        var windowImage = window.AddComponent<Image>();
        windowImage.color = new Color(0.09f, 0.11f, 0.16f, 0.97f);
        var windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(960f, 860f);

        var topBackButton = CreateButton(window.transform, "Voltar", Close);
        var topBackRect = topBackButton.GetComponent<RectTransform>();
        topBackRect.anchorMin = new Vector2(1f, 1f);
        topBackRect.anchorMax = new Vector2(1f, 1f);
        topBackRect.pivot = new Vector2(1f, 1f);
        topBackRect.sizeDelta = new Vector2(160f, 44f);
        topBackRect.anchoredPosition = new Vector2(-24f, -24f);

        var layoutRoot = CreateUiObject("LayoutRoot", window.transform);
        var layoutRect = layoutRoot.GetComponent<RectTransform>();
        layoutRect.anchorMin = Vector2.zero;
        layoutRect.anchorMax = Vector2.one;
        layoutRect.offsetMin = new Vector2(42f, 36f);
        layoutRect.offsetMax = new Vector2(-42f, -36f);

        var layout = layoutRoot.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        titleText = CreateLabel("Title", layoutRoot.transform, "Configuracoes", 36, TextAnchor.MiddleCenter, new Vector2(0f, 60f));

        CreateSectionTitle(layoutRoot.transform, "Audio");
        masterSlider = CreateVolumeSlider(layoutRoot.transform, "Master", "master");
        musicSlider = CreateVolumeSlider(layoutRoot.transform, "Musica", "music");
        sfxSlider = CreateVolumeSlider(layoutRoot.transform, "Efeitos", "sfx");

        CreateSectionTitle(layoutRoot.transform, "Teclas");
        CreateBindingButton(layoutRoot.transform, GameAction.MoveLeft, "Mover para a esquerda");
        CreateBindingButton(layoutRoot.transform, GameAction.MoveRight, "Mover para a direita");
        CreateBindingButton(layoutRoot.transform, GameAction.Jump, "Pular");
        CreateBindingButton(layoutRoot.transform, GameAction.Fire, "Atacar");
        CreateBindingButton(layoutRoot.transform, GameAction.Interact, "Interagir");
        CreateBindingButton(layoutRoot.transform, GameAction.Dash, "Dash");
        CreateBindingButton(layoutRoot.transform, GameAction.Pause, "Pause");

        feedbackText = CreateLabel("Feedback", layoutRoot.transform, string.Empty, 20, TextAnchor.MiddleCenter, new Vector2(0f, 34f));

        var actionsRow = CreateUiObject("Actions", layoutRoot.transform);
        var actionsLayout = actionsRow.AddComponent<HorizontalLayoutGroup>();
        actionsLayout.spacing = 16f;
        actionsLayout.childAlignment = TextAnchor.MiddleCenter;
        actionsLayout.childControlWidth = false;
        actionsLayout.childControlHeight = false;
        actionsLayout.childForceExpandWidth = false;
        actionsLayout.childForceExpandHeight = false;
        actionsRow.AddComponent<LayoutElement>().preferredHeight = 60f;

        CreateButton(actionsRow.transform, "Restaurar Padrao", RestoreDefaults);
        CreateButton(actionsRow.transform, "Voltar", Close);
    }

    private void Refresh()
    {
        var settings = GameServices.Instance.Settings;

        SetSliderWithoutNotify(masterSlider, settings.Data.audio.masterVolume, "master");
        SetSliderWithoutNotify(musicSlider, settings.Data.audio.musicVolume, "music");
        SetSliderWithoutNotify(sfxSlider, settings.Data.audio.sfxVolume, "sfx");

        foreach (var action in bindingLabels.Keys)
        {
            RefreshBindingText(action);
        }

        SetFeedback(string.Empty, false);
    }

    private void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
    }

    private void CaptureRebindInput()
    {
        if (!Input.anyKeyDown)
        {
            return;
        }

        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(keyCode))
            {
                continue;
            }

            TryApplyRebind(waitingRebind.Value, keyCode);
            waitingRebind = null;
            break;
        }
    }

    private void RestoreDefaults()
    {
        var defaults = SaveSystem.CreateDefault();
        foreach (var entry in defaults.keybindings)
        {
            if (Enum.TryParse(entry.keyCode, out KeyCode parsedKey))
            {
                GameServices.Instance.Settings.TryRebind(entry.action, parsedKey, out _);
            }
        }

        GameServices.Instance.Settings.SetVolumes(defaults.audio.masterVolume, defaults.audio.musicVolume, defaults.audio.sfxVolume);
        GameServices.Instance.Audio.ApplyVolumes();
        Refresh();
        SetFeedback("Configuracoes restauradas.");
    }

    private Slider CreateVolumeSlider(Transform parent, string label, string key)
    {
        var row = CreateUiObject(label + "Row", parent);
        var rowLayout = row.AddComponent<VerticalLayoutGroup>();
        rowLayout.spacing = 8f;
        rowLayout.childControlHeight = false;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandHeight = false;
        row.AddComponent<LayoutElement>().preferredHeight = 92f;

        var topRow = CreateUiObject(label + "TopRow", row.transform);
        var topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
        topLayout.spacing = 10f;
        topLayout.childControlWidth = false;
        topLayout.childControlHeight = false;
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = false;
        topRow.AddComponent<LayoutElement>().preferredHeight = 30f;

        CreateLabel(label + "Label", topRow.transform, label, 22, TextAnchor.MiddleLeft, new Vector2(500f, 30f));
        var valueText = CreateLabel(label + "Value", topRow.transform, "100%", 22, TextAnchor.MiddleRight, new Vector2(220f, 30f));
        volumeValueLabels[key] = valueText;

        var sliderObject = CreateUiObject(label + "Slider", row.transform);
        var slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        sliderObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0f, 32f);

        var background = CreateUiObject("Background", sliderObject.transform);
        var fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        var fill = CreateUiObject("Fill", fillArea.transform);
        var handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        var handle = CreateUiObject("Handle", handleArea.transform);

        background.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        fill.AddComponent<Image>().color = new Color(0.32f, 0.78f, 0.96f, 0.95f);
        handle.AddComponent<Image>().color = new Color(0.95f, 0.98f, 1f, 1f);

        StretchSliderPart(background.GetComponent<RectTransform>(), 0f, 1f);
        StretchSliderPart(fillArea.GetComponent<RectTransform>(), 0f, 1f, 10f, -10f);
        StretchSliderPart(fill.GetComponent<RectTransform>(), 0f, 1f);
        StretchSliderPart(handleArea.GetComponent<RectTransform>(), 0f, 1f, 10f, -10f);
        handle.GetComponent<RectTransform>().sizeDelta = new Vector2(24f, 24f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.onValueChanged.AddListener(value => OnVolumeChanged(key, value));

        return slider;
    }

    private void CreateBindingButton(Transform parent, GameAction action, string label)
    {
        var row = CreateUiObject(action + "Row", parent);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        row.AddComponent<LayoutElement>().preferredHeight = 56f;

        var description = CreateLabel(action + "Text", row.transform, label, 22, TextAnchor.MiddleLeft, new Vector2(420f, 48f));
        description.gameObject.AddComponent<LayoutElement>().preferredWidth = 420f;

        var button = CreateButton(row.transform, string.Empty, () =>
        {
            waitingRebind = action;
            SetFeedback("Pressione uma tecla para " + label.ToLowerInvariant() + ".");
        });

        if (button.TryGetComponent(out LayoutElement buttonLayout) == false)
        {
            buttonLayout = button.gameObject.AddComponent<LayoutElement>();
        }

        buttonLayout.preferredWidth = 260f;
        buttonLayout.preferredHeight = 48f;

        bindingLabels[action] = button.GetComponentInChildren<Text>();
        RefreshBindingText(action);
    }

    private void OnVolumeChanged(string key, float value)
    {
        var settings = GameServices.Instance.Settings;
        switch (key)
        {
            case "master":
                settings.SetVolumes(value, settings.Data.audio.musicVolume, settings.Data.audio.sfxVolume);
                break;
            case "music":
                settings.SetVolumes(settings.Data.audio.masterVolume, value, settings.Data.audio.sfxVolume);
                break;
            case "sfx":
                settings.SetVolumes(settings.Data.audio.masterVolume, settings.Data.audio.musicVolume, value);
                break;
        }

        GameServices.Instance.Audio.ApplyVolumes();
        UpdateVolumeLabel(key, value);
    }

    private void TryApplyRebind(GameAction action, KeyCode keyCode)
    {
        if (GameServices.Instance.Settings.TryRebind(action, keyCode, out string error))
        {
            RefreshBindingText(action);
            SetFeedback(action + " alterado para " + keyCode + ".");
        }
        else
        {
            SetFeedback(error);
        }
    }

    private void RefreshBindingText(GameAction action)
    {
        if (!bindingLabels.TryGetValue(action, out Text label))
        {
            return;
        }

        label.text = GameServices.Instance.Settings.GetKey(action).ToString();
    }

    private void SetSliderWithoutNotify(Slider slider, float value, string key)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(value);
        UpdateVolumeLabel(key, value);
    }

    private void UpdateVolumeLabel(string key, float value)
    {
        if (volumeValueLabels.TryGetValue(key, out Text label))
        {
            label.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }

    private void SetFeedback(string message, bool autoClear = true)
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = message;
        StopAllCoroutines();
        if (autoClear && !string.IsNullOrWhiteSpace(message))
        {
            StartCoroutine(ClearFeedbackRoutine());
        }
    }

    private IEnumerator ClearFeedbackRoutine()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }
    }

    private void Close()
    {
        waitingRebind = null;
        onCloseRequested?.Invoke();
        Hide();
    }

    private static void CreateSectionTitle(Transform parent, string title)
    {
        CreateLabel(title + "Section", parent, title, 28, TextAnchor.MiddleLeft, new Vector2(0f, 40f));
    }

    private static void StretchSliderPart(RectTransform rectTransform, float anchorMinX, float anchorMaxX, float offsetMinX = 0f, float offsetMaxX = 0f)
    {
        rectTransform.anchorMin = new Vector2(anchorMinX, 0.25f);
        rectTransform.anchorMax = new Vector2(anchorMaxX, 0.75f);
        rectTransform.offsetMin = new Vector2(offsetMinX, 0f);
        rectTransform.offsetMax = new Vector2(offsetMaxX, 0f);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Text CreateLabel(string name, Transform parent, string textValue, int fontSize, TextAnchor anchor, Vector2 preferredSize)
    {
        var labelObject = CreateUiObject(name, parent);
        var text = labelObject.AddComponent<Text>();
        text.text = textValue;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = new Color(0.95f, 0.96f, 1f, 1f);

        var rect = labelObject.GetComponent<RectTransform>();
        rect.sizeDelta = preferredSize;

        var layoutElement = labelObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = Mathf.Max(36f, preferredSize.y);
        return text;
    }

    private static Button CreateButton(Transform parent, string label, Action onClick)
    {
        var buttonObject = CreateUiObject(string.IsNullOrWhiteSpace(label) ? "KeyButton" : label + "Button", parent);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.28f, 0.38f, 1f);

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => onClick());

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220f, 48f);

        var text = CreateLabel("Label", buttonObject.transform, label, 20, TextAnchor.MiddleCenter, new Vector2(0f, 0f));
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }
}

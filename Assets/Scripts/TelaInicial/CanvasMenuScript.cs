using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class CanvasMenuScript : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string newGameSceneName = "PrimeiraFase";
    [SerializeField] private string fallbackLoadSceneName = "PrimeiraFase";
    [SerializeField] private string spriteScaleTuningSceneName = "SpriteScaleTuning";

    [Header("Optional References")]
    [SerializeField] private GameObject mainMenuRoot;

    private Transform runtimeMenuRoot;
    private Text statusLabel;
    private Dropdown attackSpriteDropdown;
    private bool updatingAttackSpriteDropdown;
    private bool loadingSavedGame;

    private void Awake()
    {
        GameServices.EnsureInstance();
        EnsureEventSystem();
   
        ResponsiveCanvasUtility.ConfigureAllCanvases();
    }

    private void Start()
    {
        BuildCleanRuntimeMenu();
        ShowMainMenu();
    }

    public void ChangeScene(string sceneName)
    {
        Time.timeScale = 1f;
        if (!loadingSavedGame && SceneManager.GetActiveScene().name == "TelaInicial" && sceneName == newGameSceneName)
        {
            GameServices.Instance.Settings.ResetProgress();
        }

        loadingSavedGame = false;
        SceneManager.LoadScene(sceneName);
    }

    public void StartNewGame()
    {
        ApplySelectedAttackSpriteVersion();
        GameServices.Instance.Settings.ResetProgress();
        ChangeScene(newGameSceneName);
    }

    public void LoadLastGame()
    {
        if (!GameServices.Instance.Settings.HasSave())
        {
            UpdateSaveStatus("Nenhum save encontrado. Inicie um novo jogo.");
            return;
        }

        string sceneName = GameServices.Instance.Settings.Data.progress.lastScene;
        if (string.IsNullOrWhiteSpace(sceneName) || sceneName == SceneManager.GetActiveScene().name)
        {
            sceneName = fallbackLoadSceneName;
        }

        loadingSavedGame = true;
        ChangeScene(sceneName);
    }

    public void OpenSettings()
    {
        if (runtimeMenuRoot != null)
        {
            runtimeMenuRoot.gameObject.SetActive(false);
        }

        EnsureSettingsCanvas();
    }

    public void OpenSpriteScaleTuning()
    {
        ApplySelectedAttackSpriteVersion();
        ChangeScene(spriteScaleTuningSceneName);
    }

    public void ShowMainMenu()
    {

        if (runtimeMenuRoot != null)
        {
            runtimeMenuRoot.gameObject.SetActive(true);
        }

        UpdateSaveStatus();
    }



    public void FecharAplicativo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    
}

    private void HideBrokenSceneUi()
    {
        var namesToHide = new[]
        {
            "JogarDoInicio",
            "CarregarJogo",
            "Sair",
            "Configuracoes",
            "Teste SpritesButton",
            "MainMenuAutoLayout",
            "MainMenuTitle",
            "SaveStatusLabel"
        };

        foreach (var itemName in namesToHide)
        {
            Transform target = FindChildRecursive(transform.root, itemName);
            if (target != null && target != transform)
            {
                target.gameObject.SetActive(false);
            }
        }
    }

    private void BuildCleanRuntimeMenu()
    {
        if (mainMenuRoot == null)
        {
            return;
        }

        var existing = FindChildRecursive(mainMenuRoot.transform, "RuntimeMainMenuRoot");
        if (existing != null)
        {
            runtimeMenuRoot = existing;
            CacheStatusLabel();
            CacheAttackSpriteDropdown();
            EnsureRuntimeMenuHasAttackSpriteDropdown();
            EnsureRuntimeMenuHasSpriteScaleTuningButton();
            RefreshAttackSpriteDropdownSelection();
            return;
        }

        runtimeMenuRoot = CreateUiObject("RuntimeMainMenuRoot", mainMenuRoot.transform).transform;
        var rootRect = runtimeMenuRoot.GetComponent<RectTransform>();
        ResponsiveCanvasUtility.StretchRoot(rootRect);

        var overlay = CreateUiObject("Overlay", runtimeMenuRoot);
        var overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0.04f, 0.05f, 0.09f, 0.32f);
        ResponsiveCanvasUtility.StretchRoot(overlay.GetComponent<RectTransform>());

        var title = CreateText("Title", runtimeMenuRoot, "Symphony of Ruin", 58, TextAnchor.MiddleCenter);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(900f, 90f);
        titleRect.anchoredPosition = new Vector2(0f, 260f);

        var subtitle = CreateText("Subtitle", runtimeMenuRoot, "Menu Principal", 24, TextAnchor.MiddleCenter);
        var subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        subtitleRect.pivot = new Vector2(0.5f, 0.5f);
        subtitleRect.sizeDelta = new Vector2(500f, 40f);
        subtitleRect.anchoredPosition = new Vector2(0f, 208f);
        subtitle.color = new Color(0.82f, 0.88f, 1f, 0.9f);

        var buttonColumn = CreateUiObject("ButtonColumn", runtimeMenuRoot);
        var columnRect = buttonColumn.GetComponent<RectTransform>();
        columnRect.anchorMin = new Vector2(0.5f, 0.5f);
        columnRect.anchorMax = new Vector2(0.5f, 0.5f);
        columnRect.pivot = new Vector2(0.5f, 0.5f);
        columnRect.sizeDelta = new Vector2(560f, 620f);
        columnRect.anchoredPosition = new Vector2(0f, -55f);

        var columnLayout = buttonColumn.AddComponent<VerticalLayoutGroup>();
        columnLayout.spacing = 14f;
        columnLayout.childAlignment = TextAnchor.MiddleCenter;
        columnLayout.childControlWidth = false;
        columnLayout.childControlHeight = false;
        columnLayout.childForceExpandWidth = false;
        columnLayout.childForceExpandHeight = false;
        var fitter = buttonColumn.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateAttackSpriteVersionSelector(buttonColumn.transform);
        CreateMenuButton(buttonColumn.transform, "Novo Jogo", StartNewGame);
        CreateMenuButton(buttonColumn.transform, "Carregar Jogo", LoadLastGame);
        CreateMenuButton(buttonColumn.transform, "Teste Sprites", OpenSpriteScaleTuning);
        CreateMenuButton(buttonColumn.transform, "Configuracoes", OpenSettings);
        CreateMenuButton(buttonColumn.transform, "Sair", FecharAplicativo);

        statusLabel = CreateText("SaveStatusLabel", runtimeMenuRoot, string.Empty, 24, TextAnchor.MiddleCenter);
        var statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRect.pivot = new Vector2(0.5f, 0.5f);
        statusRect.sizeDelta = new Vector2(900f, 50f);
        statusRect.anchoredPosition = new Vector2(0f, -370f);
        statusLabel.color = new Color(0.88f, 0.93f, 1f, 1f);

        RefreshAttackSpriteDropdownSelection();
    }

    private void CacheStatusLabel()
    {
        var labelTransform = FindChildRecursive(runtimeMenuRoot, "SaveStatusLabel");
        if (labelTransform != null)
        {
            statusLabel = labelTransform.GetComponent<Text>();
        }
    }

    private void CacheAttackSpriteDropdown()
    {
        var dropdownTransform = FindChildRecursive(runtimeMenuRoot, "AttackSpriteVersionDropdown");
        if (dropdownTransform != null)
        {
            attackSpriteDropdown = dropdownTransform.GetComponent<Dropdown>();
            if (attackSpriteDropdown != null)
            {
                attackSpriteDropdown.onValueChanged.RemoveListener(HandleAttackSpriteDropdownChanged);
                attackSpriteDropdown.onValueChanged.AddListener(HandleAttackSpriteDropdownChanged);
                PopulateAttackSpriteDropdownOptions();
            }
        }
    }

    private void EnsureSettingsCanvas()
    {
        ChangeScene("Configuracoes");
    }

    private void UpdateSaveStatus(string customMessage = null)
    {
        if (statusLabel == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            statusLabel.text = customMessage;
            return;
        }

        string spriteVersion = GameServices.Instance.Settings.GetSelectedAttackSpriteDisplayName();

        if (!GameServices.Instance.Settings.HasSave())
        {
            statusLabel.text = "Nenhum save encontrado. Sprites: " + spriteVersion;
            return;
        }

        string sceneName = GameServices.Instance.Settings.Data.progress.lastScene;
        int coins = GameServices.Instance.Settings.Data.progress.coinsCollected;
        int lives = GameServices.Instance.Settings.Data.progress.lives;
        statusLabel.text = "Save atual: " + sceneName + " | Moedas: " + coins + " | Vidas: " + lives + " | Sprites: " + spriteVersion;
    }

    private GameObject CreateAttackSpriteVersionSelector(Transform parent)
    {
        var selectorObject = CreateUiObject("AttackSpriteVersionSelector", parent);
        var selectorRect = selectorObject.GetComponent<RectTransform>();
        selectorRect.sizeDelta = new Vector2(520f, 96f);

        var selectorLayout = selectorObject.AddComponent<VerticalLayoutGroup>();
        selectorLayout.spacing = 8f;
        selectorLayout.padding = new RectOffset(0, 0, 0, 0);
        selectorLayout.childAlignment = TextAnchor.MiddleCenter;
        selectorLayout.childControlWidth = false;
        selectorLayout.childControlHeight = false;
        selectorLayout.childForceExpandWidth = false;
        selectorLayout.childForceExpandHeight = false;

        var selectorLayoutElement = selectorObject.AddComponent<LayoutElement>();
        selectorLayoutElement.preferredWidth = 520f;
        selectorLayoutElement.preferredHeight = 96f;

        var label = CreateText("AttackSpriteVersionLabel", selectorObject.transform, "Versao dos ataques", 20, TextAnchor.MiddleCenter);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(520f, 28f);
        var labelLayout = label.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 520f;
        labelLayout.preferredHeight = 28f;
        label.color = new Color(0.82f, 0.88f, 1f, 0.95f);

        attackSpriteDropdown = CreateAttackSpriteDropdown(selectorObject.transform);
        attackSpriteDropdown.onValueChanged.AddListener(HandleAttackSpriteDropdownChanged);
        return selectorObject;
    }

    private Dropdown CreateAttackSpriteDropdown(Transform parent)
    {
        var dropdownObject = CreateUiObject("AttackSpriteVersionDropdown", parent);
        var dropdownRect = dropdownObject.GetComponent<RectTransform>();
        dropdownRect.sizeDelta = new Vector2(520f, 52f);

        var layoutElement = dropdownObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 520f;
        layoutElement.preferredHeight = 52f;

        var image = dropdownObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.17f, 0.24f, 0.98f);

        var dropdown = dropdownObject.AddComponent<Dropdown>();
        dropdown.targetGraphic = image;

        var caption = CreateText("Label", dropdownObject.transform, string.Empty, 22, TextAnchor.MiddleLeft);
        var captionRect = caption.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(18f, 0f);
        captionRect.offsetMax = new Vector2(-54f, 0f);
        dropdown.captionText = caption;

        var arrow = CreateText("Arrow", dropdownObject.transform, "v", 22, TextAnchor.MiddleCenter);
        var arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0f);
        arrowRect.anchorMax = new Vector2(1f, 1f);
        arrowRect.pivot = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(46f, 0f);
        arrowRect.anchoredPosition = Vector2.zero;
        arrow.color = new Color(0.88f, 0.93f, 1f, 0.9f);

        var template = CreateDropdownTemplate(dropdownObject.transform);
        dropdown.template = template.GetComponent<RectTransform>();
        dropdown.itemText = FindChildRecursive(template.transform, "Item Label").GetComponent<Text>();
        template.SetActive(false);

        attackSpriteDropdown = dropdown;
        PopulateAttackSpriteDropdownOptions();
        return dropdown;
    }

    private GameObject CreateDropdownTemplate(Transform parent)
    {
        var template = CreateUiObject("Template", parent);
        var templateRect = template.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0f, 240f);
        templateRect.anchoredPosition = new Vector2(0f, -54f);

        var templateImage = template.AddComponent<Image>();
        templateImage.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);

        var scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        var viewport = CreateUiObject("Viewport", template.transform);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(4f, 4f);
        viewportRect.offsetMax = new Vector2(-4f, -4f);
        var viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.08f, 0.11f, 0.16f, 0.9f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = CreateUiObject("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 0f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        var contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var item = CreateDropdownItem(content.transform);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        return template;
    }

    private GameObject CreateDropdownItem(Transform parent)
    {
        var item = CreateUiObject("Item", parent);
        var itemRect = item.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0f, 42f);

        var itemLayout = item.AddComponent<LayoutElement>();
        itemLayout.preferredHeight = 42f;

        var itemImage = item.AddComponent<Image>();
        itemImage.color = new Color(0.13f, 0.19f, 0.27f, 0.98f);

        var toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = itemImage;

        var itemLabel = CreateText("Item Label", item.transform, string.Empty, 20, TextAnchor.MiddleLeft);
        var itemLabelRect = itemLabel.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(18f, 0f);
        itemLabelRect.offsetMax = new Vector2(-18f, 0f);

        return item;
    }

    private void RefreshAttackSpriteDropdownSelection()
    {
        if (attackSpriteDropdown == null)
        {
            return;
        }

        string selectedId = GameServices.Instance.Settings.SelectedAttackSpriteVersionId;
        int selectedIndex = 0;
        for (int index = 0; index < PlayerAttackSpriteVersions.All.Count; index++)
        {
            if (PlayerAttackSpriteVersions.All[index].Id == selectedId)
            {
                selectedIndex = index;
                break;
            }
        }

        updatingAttackSpriteDropdown = true;
        attackSpriteDropdown.value = selectedIndex;
        attackSpriteDropdown.RefreshShownValue();
        updatingAttackSpriteDropdown = false;
    }

    private void PopulateAttackSpriteDropdownOptions()
    {
        if (attackSpriteDropdown == null)
        {
            return;
        }

        bool wasUpdating = updatingAttackSpriteDropdown;
        updatingAttackSpriteDropdown = true;
        int previousValue = attackSpriteDropdown.value;
        attackSpriteDropdown.options.Clear();
        foreach (PlayerAttackSpriteVersion version in PlayerAttackSpriteVersions.All)
        {
            attackSpriteDropdown.options.Add(new Dropdown.OptionData(version.DisplayName));
        }

        attackSpriteDropdown.value = Mathf.Clamp(previousValue, 0, Mathf.Max(0, attackSpriteDropdown.options.Count - 1));
        attackSpriteDropdown.RefreshShownValue();
        updatingAttackSpriteDropdown = wasUpdating;
    }

    private void HandleAttackSpriteDropdownChanged(int selectedIndex)
    {
        if (updatingAttackSpriteDropdown || selectedIndex < 0 || selectedIndex >= PlayerAttackSpriteVersions.All.Count)
        {
            return;
        }

        GameServices.Instance.Settings.SetSelectedAttackSpriteVersion(PlayerAttackSpriteVersions.All[selectedIndex].Id);
        UpdateSaveStatus();
    }

    private void ApplySelectedAttackSpriteVersion()
    {
        if (attackSpriteDropdown == null)
        {
            return;
        }

        int selectedIndex = attackSpriteDropdown.value;
        if (selectedIndex >= 0 && selectedIndex < PlayerAttackSpriteVersions.All.Count)
        {
            GameServices.Instance.Settings.SetSelectedAttackSpriteVersion(PlayerAttackSpriteVersions.All[selectedIndex].Id);
        }
    }

    private void EnsureRuntimeMenuHasAttackSpriteDropdown()
    {
        if (runtimeMenuRoot == null)
        {
            return;
        }

        ConfigureExistingRuntimeMenuLayout();
        if (attackSpriteDropdown != null)
        {
            return;
        }

        Transform buttonColumn = FindChildRecursive(runtimeMenuRoot, "ButtonColumn");
        if (buttonColumn == null)
        {
            return;
        }

        var selectorObject = CreateAttackSpriteVersionSelector(buttonColumn);
        selectorObject.transform.SetSiblingIndex(0);
    }

    private void EnsureRuntimeMenuHasSpriteScaleTuningButton()
    {
        if (runtimeMenuRoot == null)
        {
            return;
        }

        Transform buttonColumn = FindChildRecursive(runtimeMenuRoot, "ButtonColumn");
        if (buttonColumn == null)
        {
            return;
        }

        Transform existingButton = FindChildRecursive(buttonColumn, "Teste SpritesButton");
        if (existingButton != null)
        {
            Button button = existingButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(OpenSpriteScaleTuning);
                button.onClick.AddListener(OpenSpriteScaleTuning);
            }

            ConfigureMenuButtonSize(existingButton.gameObject);
            return;
        }

        Button tuningButton = CreateMenuButton(buttonColumn, "Teste Sprites", OpenSpriteScaleTuning);
        Transform loadButton = FindChildRecursive(buttonColumn, "Carregar JogoButton");
        if (loadButton != null)
        {
            tuningButton.transform.SetSiblingIndex(loadButton.GetSiblingIndex() + 1);
        }
    }

    private void ConfigureExistingRuntimeMenuLayout()
    {
        ConfigureAnchoredRect("Title", new Vector2(900f, 90f), new Vector2(0f, 260f));
        ConfigureAnchoredRect("Subtitle", new Vector2(500f, 40f), new Vector2(0f, 208f));
        ConfigureAnchoredRect("SaveStatusLabel", new Vector2(900f, 50f), new Vector2(0f, -370f));

        Transform buttonColumn = FindChildRecursive(runtimeMenuRoot, "ButtonColumn");
        if (buttonColumn == null)
        {
            return;
        }

        var columnRect = buttonColumn.GetComponent<RectTransform>();
        if (columnRect != null)
        {
            columnRect.anchorMin = new Vector2(0.5f, 0.5f);
            columnRect.anchorMax = new Vector2(0.5f, 0.5f);
            columnRect.pivot = new Vector2(0.5f, 0.5f);
            columnRect.sizeDelta = new Vector2(560f, 620f);
            columnRect.anchoredPosition = new Vector2(0f, -55f);
        }

        var columnLayout = buttonColumn.GetComponent<VerticalLayoutGroup>();
        if (columnLayout == null)
        {
            columnLayout = buttonColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        columnLayout.spacing = 14f;
        columnLayout.childAlignment = TextAnchor.MiddleCenter;
        columnLayout.childControlWidth = false;
        columnLayout.childControlHeight = false;
        columnLayout.childForceExpandWidth = false;
        columnLayout.childForceExpandHeight = false;

        var fitter = buttonColumn.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = buttonColumn.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button[] buttons = buttonColumn.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            ConfigureMenuButtonSize(button.gameObject);
        }
    }

    private void ConfigureAnchoredRect(string objectName, Vector2 size, Vector2 position)
    {
        Transform target = FindChildRecursive(runtimeMenuRoot, objectName);
        if (target == null)
        {
            return;
        }

        var rect = target.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Button CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = CreateUiObject(label + "Button", parent);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.22f, 0.31f, 0.96f);

        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);

        ConfigureMenuButtonSize(buttonObject);

        var labelText = CreateText("Label", buttonObject.transform, label, 26, TextAnchor.MiddleCenter);
        ResponsiveCanvasUtility.StretchRoot(labelText.GetComponent<RectTransform>());
        return button;
    }

    private static void ConfigureMenuButtonSize(GameObject buttonObject)
    {
        if (buttonObject == null || !buttonObject.name.EndsWith("Button"))
        {
            return;
        }

        var rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(520f, 76f);
        }

        var layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = 520f;
        layoutElement.preferredHeight = 76f;
    }

    private static Text CreateText(string name, Transform parent, string textValue, int fontSize, TextAnchor alignment)
    {
        var textObject = CreateUiObject(name, parent);
        var text = textObject.AddComponent<Text>();
        text.text = textValue;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
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
            var child = FindChildRecursive(root.GetChild(index), childName);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }
}

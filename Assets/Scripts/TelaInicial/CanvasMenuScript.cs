using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CanvasMenuScript : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string newGameSceneName = "PrimeiraFase";
    [SerializeField] private string fallbackLoadSceneName = "PrimeiraFase";

    [Header("Optional References")]
    [SerializeField] private GameObject mainMenuRoot;

    private Transform runtimeMenuRoot;
    private Text statusLabel;
    private bool loadingSavedGame;

    private void Awake()
    {
        GameServices.EnsureInstance();
        EnsureEventSystem();
        ResolveReferences();
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
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ResolveReferences()
    {
        if (mainMenuRoot == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            mainMenuRoot = canvas != null ? canvas.gameObject : gameObject;
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
            EnsureRuntimeMenuHasSpriteScaleTuningButton();
            return;
        }

        runtimeMenuRoot = CreateUiObject("RuntimeMainMenuRoot", mainMenuRoot.transform).transform;
        var rootRect = runtimeMenuRoot.GetComponent<RectTransform>();
        ResponsiveCanvasUtility.StretchRoot(rootRect);

        var overlay = CreateUiObject("Overlay", runtimeMenuRoot);

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
        var overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0.04f, 0.05f, 0.09f, 0.32f);
        ResponsiveCanvasUtility.StretchRoot(overlay.GetComponent<RectTransform>());

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

        CreateMenuButton(buttonColumn.transform, "Novo Jogo", StartNewGame);
        CreateMenuButton(buttonColumn.transform, "Carregar Jogo", LoadLastGame);
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
    }

    private void CacheStatusLabel()
    {
        var labelTransform = FindChildRecursive(runtimeMenuRoot, "SaveStatusLabel");
        if (labelTransform != null)
        {
            statusLabel = labelTransform.GetComponent<Text>();
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

        if (!GameServices.Instance.Settings.HasSave())
        {
            statusLabel.text = "Nenhum save encontrado.";
            return;
        }

        string sceneName = GameServices.Instance.Settings.Data.progress.lastScene;
        int coins = GameServices.Instance.Settings.Data.progress.coinsCollected;
        int lives = GameServices.Instance.Settings.Data.progress.lives;
        statusLabel.text = "Save atual: " + sceneName + " | Moedas: " + coins + " | Vidas: " + lives;
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

        // Remove botao "Teste Sprites" se existir
        Transform existingButton = FindChildRecursive(buttonColumn, "Teste SpritesButton");
        if (existingButton != null)
        {
            Object.Destroy(existingButton.gameObject);
        }

        ConfigureExistingRuntimeMenuLayout();
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
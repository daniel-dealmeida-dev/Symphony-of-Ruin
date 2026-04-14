using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static GameManager gm;

    [Header("Menus de Interface")]
    public GameObject painelPause;
    public GameObject painelGameOver;

    [Header("Status do Jogo")]
    public int moedasColetadas = 0;
    public bool jogoPausado = false;
    public bool gameIsOver = false;

    private Text hudText;
    private SettingsPanelController settingsPanelController;
    private Transform playerRoot;
    private bool pauseInputReleased = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            gm = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        GameServices.EnsureInstance();
        EnsureEnemyBootstrap();
        EnsureSceneUi();
        moedasColetadas = GameServices.Instance.Settings.Data.progress.coinsCollected;
        playerRoot = GameObject.Find("Personagem")?.transform;
        RestoreSavedPlayerPosition();
        UpdateHud();
    }

    private void Update()
    {
        if (settingsPanelController != null && settingsPanelController.IsVisible)
        {
            return;
        }

        bool pausePressed = GameServices.Instance.Settings.GetButton(GameAction.Pause);
        if (!pausePressed)
        {
            pauseInputReleased = true;
        }

        if (pauseInputReleased && GameServices.Instance.Settings.GetButtonDown(GameAction.Pause))
        {
            pauseInputReleased = false;
            if (jogoPausado)
            {
                Retomar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Pausar()
    {
        jogoPausado = true;
        Time.timeScale = 0f;
        SaveProgress();
        if (painelPause != null)
        {
            painelPause.SetActive(true);
        }
    }

    public void Retomar()
    {
        jogoPausado = false;
        Time.timeScale = 1f;

        if (settingsPanelController != null)
        {
            settingsPanelController.Hide();
        }

        if (painelPause != null)
        {
            painelPause.SetActive(false);
        }
    }

    public void FinalizarJogo()
    {
        gameIsOver = true;
        Time.timeScale = 0f;
        if (painelGameOver != null)
        {
            painelGameOver.SetActive(true);
        }
    }

    public void ReiniciarFase()
    {
        gameIsOver = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        GameServices.Instance.Settings.MarkSceneCompleted(SceneManager.GetActiveScene().name);
        int proximaCena = SceneManager.GetActiveScene().buildIndex + 1;
        if (proximaCena < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(proximaCena);
        }
        else
        {
            SceneManager.LoadScene("TelaInicial");
        }
    }

    public void RestartGame()
    {
        ReiniciarFase();
    }

    public void targetHit(int pontuacao, float tempoExtra)
    {
        moedasColetadas += pontuacao;
        GameServices.Instance.Settings.SetCoins(moedasColetadas);
        UpdateHud();
    }

    public void SyncLives(int currentLives)
    {
        GameServices.Instance.Settings.SetLives(currentLives);
        UpdateHud();
    }

    public void SaveProgress()
    {
        GameServices.Instance.Settings.SetLastScene(SceneManager.GetActiveScene().name);
        if (playerRoot != null)
        {
            GameServices.Instance.Settings.SetPlayerPosition(playerRoot.position);
        }
        else
        {
            GameServices.Instance.Settings.Save();
        }
    }

    private void EnsureSceneUi()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("GameplayCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        ResponsiveCanvasUtility.ConfigureAllCanvases();

        if (painelPause == null)
        {
            painelPause = CreatePanel(canvas.transform, "PausePanel", new Color(0f, 0f, 0f, 0.72f));
            painelPause.SetActive(false);
            BuildPausePanel(painelPause.transform);
        }

        if (painelGameOver == null)
        {
            painelGameOver = CreatePanel(canvas.transform, "GameOverPanel", new Color(0.15f, 0f, 0f, 0.75f));
            painelGameOver.SetActive(false);
            BuildGameOverPanel(painelGameOver.transform);
        }

        if (hudText == null)
        {
            var hudRoot = new GameObject("HUD", typeof(RectTransform));
            hudRoot.transform.SetParent(canvas.transform, false);
            var hudRect = hudRoot.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(0f, 1f);
            hudRect.pivot = new Vector2(0f, 1f);
            hudRect.anchoredPosition = new Vector2(32f, -32f);
            hudRect.sizeDelta = new Vector2(420f, 80f);

            hudText = CreateText(hudRoot.transform, "HUDText", TextAnchor.UpperLeft, 28);
            var hudTextRect = hudText.GetComponent<RectTransform>();
            hudTextRect.anchorMin = Vector2.zero;
            hudTextRect.anchorMax = Vector2.one;
            hudTextRect.offsetMin = Vector2.zero;
            hudTextRect.offsetMax = Vector2.zero;
        }
    }

    private static void EnsureEnemyBootstrap()
    {
        if (FindFirstObjectByType<EnemySceneBootstrap>() != null)
        {
            return;
        }

        new GameObject("EnemySceneBootstrap").AddComponent<EnemySceneBootstrap>();
    }

    private void BuildPausePanel(Transform parent)
    {
        CreateCenteredTitle(parent, "Jogo Pausado");
        CreateMenuButton(parent, "Retomar", Retomar, new Vector2(0f, 70f));
        CreateMenuButton(parent, "Salvar", SaveProgress, new Vector2(0f, 10f));
        CreateMenuButton(parent, "Configuracoes", OpenSettingsPanel, new Vector2(0f, -50f));
        CreateMenuButton(parent, "Menu Inicial", ReturnToMainMenu, new Vector2(0f, -110f));
    }

    private void BuildGameOverPanel(Transform parent)
    {
        CreateCenteredTitle(parent, "Game Over");
        CreateMenuButton(parent, "Reiniciar", ReiniciarFase, new Vector2(0f, -10f));
        CreateMenuButton(parent, "Menu Inicial", ReturnToMainMenu, new Vector2(0f, -80f));
    }

    private void OpenSettingsPanel()
    {
        EnsureSettingsCanvas();
        painelPause.SetActive(false);
        settingsPanelController.Show("Configuracoes", () =>
        {
            if (painelPause != null)
            {
                painelPause.SetActive(true);
            }
        });
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SaveProgress();
        SceneManager.LoadScene("TelaInicial");
    }

    private void UpdateHud()
    {
        if (hudText != null)
        {
            hudText.text = "Moedas: " + moedasColetadas + "\nVidas: " + GameServices.Instance.Settings.Data.progress.lives;
        }
    }

    private void RestoreSavedPlayerPosition()
    {
        if (playerRoot == null)
        {
            return;
        }

        if (!GameServices.Instance.Settings.HasSave())
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != GameServices.Instance.Settings.Data.progress.lastScene)
        {
            return;
        }

        Vector2 savedPosition = GameServices.Instance.Settings.GetSavedPlayerPosition();
        if (savedPosition == Vector2.zero)
        {
            return;
        }

        playerRoot.position = new Vector3(savedPosition.x, savedPosition.y, playerRoot.position.z);
    }

    private void EnsureSettingsCanvas()
    {
        if (settingsPanelController != null)
        {
            return;
        }

        settingsPanelController = SettingsPanelController.CreateOrGet("SettingsCanvasGameplay");
        settingsPanelController.Hide();
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        ResponsiveCanvasUtility.StretchRoot(panel.GetComponent<RectTransform>());
        return panel;
    }

    private static Text CreateText(Transform parent, string name, TextAnchor alignment, int size)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static void CreateCenteredTitle(Transform parent, string title)
    {
        var text = CreateText(parent, title + "Text", TextAnchor.MiddleCenter, 36);
        text.text = title;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(480f, 60f);
        rect.anchoredPosition = new Vector2(0f, 120f);
    }

    private static void CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction callback, Vector2 position)
    {
        var buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 52f);
        rect.anchoredPosition = position;

        buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.35f, 1f);
        buttonObject.GetComponent<Button>().onClick.AddListener(callback);

        var text = CreateText(buttonObject.transform, "Label", TextAnchor.MiddleCenter, 22);
        text.text = label;
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}

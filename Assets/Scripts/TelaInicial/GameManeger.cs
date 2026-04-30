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
    private GameObject attackButtonsRoot;
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
        Time.timeScale = 1f;
        EnsureEnemyBootstrap();
        EnsureSceneUi();
        moedasColetadas = GameServices.Instance.Settings.Data.progress.coinsCollected;
        playerRoot = ResolvePlayerRoot();
        RestoreSavedPlayerPosition();
        UpdateHud();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            gm = null;
        }
    }

    private void Update()
    {

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
        if (playerRoot == null)
        {
            playerRoot = ResolvePlayerRoot();
        }

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

        EnsureAttackButtons(canvas.transform);

        if (attackButtonsRoot != null)
        {
            attackButtonsRoot.transform.SetAsLastSibling();
        }

        if (painelPause != null)
        {
            painelPause.transform.SetAsLastSibling();
        }

        if (painelGameOver != null)
        {
            painelGameOver.transform.SetAsLastSibling();
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
        if (settingsPanelController == null)
        {
            return;
        }

        if (painelPause != null)
        {
            painelPause.SetActive(false);
        }

        settingsPanelController.gameObject.SetActive(true);
    }

    private void CloseSettingsPanel()
    {
        if (settingsPanelController != null)
        {
            settingsPanelController.gameObject.SetActive(false);
        }

        if (painelPause != null)
        {
            painelPause.SetActive(true);
        }
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
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            int maxLives = playerHealth != null ? playerHealth.MaxHealth : GameplayBalance.PlayerMaxHealth;
            hudText.text = "Moedas: " + moedasColetadas + "\nVidas: " + GameServices.Instance.Settings.Data.progress.lives + "/" + maxLives;
        }
    }

    private void EnsureAttackButtons(Transform canvasTransform)
    {
        if (attackButtonsRoot != null || canvasTransform == null)
        {
            return;
        }

        attackButtonsRoot = new GameObject("AttackTouchButtons", typeof(RectTransform));
        attackButtonsRoot.transform.SetParent(canvasTransform, false);

        var rootRect = attackButtonsRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(1f, 0f);
        rootRect.anchoredPosition = new Vector2(-28f, 28f);
        rootRect.sizeDelta = new Vector2(228f, 228f);

        CreateAttackTouchButton(attackButtonsRoot.transform, "Attack1Button", "Z", 0, new Vector2(-118f, 118f));
        CreateAttackTouchButton(attackButtonsRoot.transform, "Attack2Button", "X", 1, new Vector2(-8f, 118f));
        CreateAttackTouchButton(attackButtonsRoot.transform, "Attack3Button", "C", 2, new Vector2(-118f, 8f));
        CreateAttackTouchButton(attackButtonsRoot.transform, "Attack4Button", "V", 3, new Vector2(-8f, 8f));
    }

    private void CreateAttackTouchButton(Transform parent, string name, string label, int attackIndex, Vector2 anchoredPosition)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(96f, 96f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.11f, 0.16f, 0.72f);

        var touchButton = buttonObject.AddComponent<AttackTouchButton>();
        touchButton.Initialize(this, attackIndex, image);

        var text = CreateText(buttonObject.transform, "Label", TextAnchor.MiddleCenter, 34);
        text.text = label;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.9f, 0.96f, 1f, 1f);
        text.raycastTarget = false;

        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    public void SolicitarAtaquePeloHud(int attackIndex)
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        MovimentoJogador movimento = FindFirstObjectByType<MovimentoJogador>();
        if (movimento == null)
        {
            return;
        }

        movimento.SolicitarAtaqueGuitarra(attackIndex);
    }

    private void RestoreSavedPlayerPosition()
    {
        if (playerRoot == null)
        {
            playerRoot = ResolvePlayerRoot();
        }

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

    private static Transform ResolvePlayerRoot()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth.transform;
        }

        MovimentoJogador movimento = FindFirstObjectByType<MovimentoJogador>();
        if (movimento != null)
        {
            return movimento.transform;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer.transform;
        }

        GameObject legacyRoot = GameObject.Find("Personagem");
        return legacyRoot != null ? legacyRoot.transform : null;
    }

    private void EnsureSettingsCanvas()
    {
        if (settingsPanelController != null)
        {
            settingsPanelController.SetCloseAction(CloseSettingsPanel);
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EnsureSceneUi();
            canvas = FindFirstObjectByType<Canvas>();
        }

        if (canvas == null)
        {
            Debug.LogWarning("Nao foi possivel criar painel de configuracoes: canvas nao encontrado.");
            return;
        }

        settingsPanelController = SettingsPanelController.CreateRuntimePanel(canvas.transform, CloseSettingsPanel);
        settingsPanelController.gameObject.SetActive(false);
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
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
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

internal class AttackTouchButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private static readonly Color NormalColor = new Color(0.08f, 0.11f, 0.16f, 0.72f);
    private static readonly Color PressedColor = new Color(0.48f, 0.72f, 1f, 0.92f);

    private GameManager gameManager;
    private Image targetImage;
    private int attackIndex;

    public void Initialize(GameManager manager, int attackLineIndex, Image image)
    {
        gameManager = manager;
        attackIndex = attackLineIndex;
        targetImage = image;
        SetPressed(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameManager != null)
        {
            gameManager.SolicitarAtaquePeloHud(attackIndex);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    private void SetPressed(bool pressed)
    {
        if (targetImage != null)
        {
            targetImage.color = pressed ? PressedColor : NormalColor;
        }
    }
}

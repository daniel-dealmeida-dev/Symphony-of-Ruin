using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Estado global da sessao: pontuacao, pausa e game over.
/// O ecra de game over e um canvas em runtime (GameOverRuntime), invisivel ate <see cref="FinalizarJogo"/>.
/// </summary>
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

    private TMP_Text gameOverScoreText;

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
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;

        AutoBindUiReferences();
        EnsureRuntimeGameOverCanvas();

        if (painelPause != null)
        {
            painelPause.SetActive(false);
        }

        if (painelGameOver != null)
        {
            painelGameOver.SetActive(false);
        }
    }

    private void Update()
    {
        if (!gameIsOver && !jogoPausado && Time.timeScale > 0f)
        {
            DifficultyRuntime.Tick(Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameIsOver)
            {
                return;
            }

            if (jogoPausado)
            {
                Retomar();
            }
            else
            {
                Pausar();
            }
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.K))
        {
            FinalizarJogo();
        }
#endif
    }

    private void AutoBindUiReferences()
    {
        if (painelPause == null)
        {
            GameObject p = GameObject.Find("PausePanel");
            if (p != null)
            {
                painelPause = p;
            }
        }
    }

    /// <summary>
    /// Esconde o canvas antigo da cena (se existir) e cria o overlay de game over em runtime.
    /// </summary>
    private void EnsureRuntimeGameOverCanvas()
    {
        if (painelGameOver != null)
        {
            return;
        }

        Scene s = gameObject.scene;
        foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null || !go.scene.IsValid() || go.scene != s)
            {
                continue;
            }

            if (go.name == "GameOver")
            {
                go.SetActive(false);
            }
        }

        GameObject root = new GameObject("GameOverRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        painelGameOver = root;

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;
        rootRt.anchoredPosition = Vector2.zero;

        GameObject tint = new GameObject("RedTint", typeof(RectTransform), typeof(Image));
        tint.transform.SetParent(root.transform, false);
        RectTransform tintRt = tint.GetComponent<RectTransform>();
        tintRt.anchorMin = Vector2.zero;
        tintRt.anchorMax = Vector2.one;
        tintRt.offsetMin = Vector2.zero;
        tintRt.offsetMax = Vector2.zero;
        Image tintImg = tint.GetComponent<Image>();
        tintImg.color = new Color(0.55f, 0.02f, 0.02f, 0.62f);
        tintImg.raycastTarget = true;

        GameObject titleGo = new GameObject("TitleGameOver", typeof(RectTransform));
        titleGo.transform.SetParent(root.transform, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.58f);
        titleRt.anchorMax = new Vector2(0.5f, 0.58f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(1000f, 220f);
        TMP_Text title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "GAME OVER";
        title.fontSize = 110;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;

        GameObject btnGo = new GameObject("BtnJogarNovamente", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(root.transform, false);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.38f);
        btnRt.anchorMax = new Vector2(0.5f, 0.38f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.sizeDelta = new Vector2(620f, 96f);
        Image btnImg = btnGo.GetComponent<Image>();
        btnImg.color = new Color(0.15f, 0.02f, 0.02f, 0.85f);
        Button btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(ReiniciarFase);

        GameObject btnLabel = new GameObject("Label", typeof(RectTransform));
        btnLabel.transform.SetParent(btnGo.transform, false);
        RectTransform bl = btnLabel.GetComponent<RectTransform>();
        bl.anchorMin = Vector2.zero;
        bl.anchorMax = Vector2.one;
        bl.offsetMin = Vector2.zero;
        bl.offsetMax = Vector2.zero;
        TMP_Text btnTxt = btnLabel.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "Jogar novamente";
        btnTxt.fontSize = 48;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = Color.white;

        GameObject scoreGo = new GameObject("GameOverScoreText", typeof(RectTransform));
        scoreGo.transform.SetParent(root.transform, false);
        RectTransform scoreRt = scoreGo.GetComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(0.5f, 0.26f);
        scoreRt.anchorMax = new Vector2(0.5f, 0.26f);
        scoreRt.pivot = new Vector2(0.5f, 0.5f);
        scoreRt.sizeDelta = new Vector2(900f, 56f);
        gameOverScoreText = scoreGo.AddComponent<TextMeshProUGUI>();
        gameOverScoreText.fontSize = 34;
        gameOverScoreText.alignment = TextAlignmentOptions.Center;
        gameOverScoreText.color = new Color(1f, 0.92f, 0.92f, 1f);
        gameOverScoreText.text = string.Empty;
    }

    public void Pausar()
    {
        if (gameIsOver)
        {
            return;
        }

        jogoPausado = true;
        Time.timeScale = 0f;

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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOverMusic();
        }

        if (painelGameOver == null)
        {
            EnsureRuntimeGameOverCanvas();
        }

        if (painelGameOver != null)
        {
            painelGameOver.SetActive(true);
        }

        if (gameOverScoreText != null)
        {
            gameOverScoreText.text = "Fragmentos coletados: " + moedasColetadas;
        }
    }

    public void ReiniciarFase()
    {
        gameIsOver = false;
        jogoPausado = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        int proximaCena = SceneManager.GetActiveScene().buildIndex + 1;

        if (proximaCena < SceneManager.sceneCountInBuildSettings)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(proximaCena);
        }
    }

    public void RestartGame()
    {
        ReiniciarFase();
    }

    public void targetHit(int pontuacao, float tempoExtra)
    {
        moedasColetadas += pontuacao;
    }

    public void SairDoJogo()
    {
        Application.Quit();
    }
}

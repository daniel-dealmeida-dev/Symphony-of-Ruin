using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Estado global da sess?o: pontua??o, pausa e game over.
/// O bootstrap de cena cria este objeto em fases com <see cref="Controle"/> se n?o existir na hierarquia.
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
        if (painelGameOver == null)
        {
            GameObject go = GameObject.Find("GameOver");
            if (go != null)
            {
                painelGameOver = go;
            }
        }

        if (painelPause == null)
        {
            GameObject p = GameObject.Find("PausePanel");
            if (p != null)
            {
                painelPause = p;
            }
        }

        EnsureGameOverScoreLabel();
    }

    private void EnsureGameOverScoreLabel()
    {
        if (painelGameOver == null)
        {
            return;
        }

        Transform existing = painelGameOver.transform.Find("GameOverScoreText");
        if (existing != null)
        {
            gameOverScoreText = existing.GetComponent<TMP_Text>();
            return;
        }

        GameObject row = new GameObject("GameOverScoreText", typeof(RectTransform));
        row.transform.SetParent(painelGameOver.transform, false);
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.58f);
        rt.anchorMax = new Vector2(0.5f, 0.58f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(700f, 64f);

        gameOverScoreText = row.AddComponent<TextMeshProUGUI>();
        gameOverScoreText.fontSize = 32;
        gameOverScoreText.alignment = TextAlignmentOptions.Center;
        gameOverScoreText.color = Color.white;
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

        if (painelGameOver != null)
        {
            painelGameOver.SetActive(true);
            Transform dim = painelGameOver.transform.Find("GameOverPanel");
            if (dim != null)
            {
                dim.gameObject.SetActive(true);
            }
        }

        if (gameOverScoreText == null)
        {
            EnsureGameOverScoreLabel();
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

using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // SINGLETON
    public static GameManager instance;
    public static GameManager gm;

    [Header("Menus de Interface")]
    public GameObject painelPause;
    public GameObject painelGameOver;

    [Header("Status do Jogo")]
    public int moedasColetadas = 0;
    public bool jogoPausado = false;
    public bool gameIsOver = false;

    // AWAKE
    void Awake()
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

    // START
    void Start()
    {
        Time.timeScale = 1f;

        if (painelPause != null)
        {
            painelPause.SetActive(false);
        }

        if (painelGameOver != null)
        {
            painelGameOver.SetActive(false);
        }
    }

    // UPDATE
    void Update()
    {
        // PAUSE
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (jogoPausado)
            {
                Retomar();
            }
            else
            {
                Pausar();
            }
        }

        // TESTE DE GAME OVER
        if (Input.GetKeyDown(KeyCode.K))
        {
            FinalizarJogo();
        }
    }

    // PAUSAR JOGO
    public void Pausar()
    {
        jogoPausado = true;

        Time.timeScale = 0f;

        if (painelPause != null)
        {
            painelPause.SetActive(true);
        }
    }

    // RETOMAR JOGO
    public void Retomar()
    {
        jogoPausado = false;

        Time.timeScale = 1f;

        if (painelPause != null)
        {
            painelPause.SetActive(false);
        }
    }

    // GAME OVER
    public void FinalizarJogo()
    {
        gameIsOver = true;

        Time.timeScale = 0f;

        if (painelGameOver != null)
        {
            painelGameOver.SetActive(true);
        }
    }

    // REINICIAR FASE
    public void ReiniciarFase()
    {
        gameIsOver = false;

        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // PRÓXIMA FASE
    public void NextLevel()
    {
        int proximaCena = SceneManager.GetActiveScene().buildIndex + 1;

        if (proximaCena < SceneManager.sceneCountInBuildSettings)
        {
            Time.timeScale = 1f;

            SceneManager.LoadScene(proximaCena);
        }
    }

    // RESTART GAME
    public void RestartGame()
    {
        ReiniciarFase();
    }

    // PONTUAÇÃO
    public void targetHit(int pontuacao, float tempoExtra)
    {
        moedasColetadas += pontuacao;
    }

    // SAIR DO JOGO
    public void SairDoJogo()
    {
        Application.Quit();
    }
}
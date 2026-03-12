using UnityEngine;
using UnityEngine.SceneManagement; // Para mudar de fases

public class GameManager : MonoBehaviour
{
    // Singleton: Garante que só exista UM GameManager no jogo todo
    public static GameManager instance;

    [Header("Menus de Interface")]
    public GameObject painelPause;
    public GameObject painelGameOver;

    [Header("Status do Jogo")]
    public int moedasColetadas = 0;
    public bool jogoPausado = false;

    void Awake()
    {
        // Configuração do Singleton
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject); // Opcional: Se quiser que ele dure entre fases
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Atalho para Pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (jogoPausado) Retomar(); else Pausar();
        }
    }

    // --- FUNÇÕES DE CONTROLE ---

    public void Pausar()
    {
        jogoPausado = true;
        Time.timeScale = 0f;
        painelPause.SetActive(true);
    }

    public void Retomar()
    {
        jogoPausado = false;
        Time.timeScale = 1f;
        painelPause.SetActive(false);
    }

    public void FinalizarJogo() // Chamado quando o jogador morre
    {
        Time.timeScale = 0f;
        painelGameOver.SetActive(true);
    }

    public void ReiniciarFase()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
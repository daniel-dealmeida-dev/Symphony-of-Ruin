using UnityEngine;
using UnityEngine.SceneManagement; // Para mudar de fases

public class GameManager : MonoBehaviour
{
    // Singleton: Garante que só exista UM GameManager no jogo todo
    public static GameManager instance;

    // Adicionado para compatibilidade com scripts que usam GameManager.gm
    public static GameManager gm;

    [Header("Menus de Interface")]
    public GameObject painelPause;
    public GameObject painelGameOver;

    [Header("Status do Jogo")]
    public int moedasColetadas = 0;
    public bool jogoPausado = false;

    // Adicionado para compatibilidade com scripts que usam GameManager.gm.gameIsOver
    public bool gameIsOver = false;

    void Awake()
    {
        // Configuração do Singleton
        if (instance == null)
        {
            instance = this;
            gm = this; // define gm também
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
        gameIsOver = true; // marca o jogo como acabado
        Time.timeScale = 0f;
        painelGameOver.SetActive(true);
    }

    public void ReiniciarFase()
    {
        gameIsOver = false; // reseta o status do jogo
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Adicionado para compatibilidade com NextLevel.cs
    public void NextLevel()
    {
        // Mantém o funcionamento padrão: passa para a próxima cena
        int proximaCena = SceneManager.GetActiveScene().buildIndex + 1;
        if (proximaCena < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(proximaCena);
    }

    // Adicionado para compatibilidade com PlayAgain.cs
    public void RestartGame()
    {
        // Apenas chama ReiniciarFase, mantém o funcionamento
        ReiniciarFase();
    }

    // Adicionado para compatibilidade com ComportamentoAlvo.cs
    public void targetHit(int pontuacao, float tempoExtra)
    {
        // Mantém o funcionamento básico:
        // aqui você pode adicionar lógica de pontuação ou tempo extra,
        // se quiser manter exatamente o funcionamento do merge antigo.
        moedasColetadas += pontuacao;
        // tempoExtra poderia ser usado aqui se houvesse um temporizador, mas mantemos apenas o básico
    }
}
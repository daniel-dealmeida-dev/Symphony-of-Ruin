using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGame : MonoBehaviour
{
    
    public GameObject endGameUI;

    void Start()
    {
        endGameUI.SetActive(false);
    }

    public void ShowEndGame()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.FinalizeScore();
        }

        Time.timeScale = 0f; 
        endGameUI.SetActive(true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Sair do jogo (funciona no build)");
    }
}

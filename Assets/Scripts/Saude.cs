using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class Saude : MonoBehaviour
{

    public bool morto;
    public int saude;
    private Animator animator;

    // Use this for initialization
    void Start()
    {
        morto = false;
        animator = gameObject.GetComponent<Animator>();
    }

    void Update()
    {
    }

    public void dano(int x)
    {
        if (morto || x <= 0)
        {
            return;
        }

        saude -= x;
        if (saude <= 0)
        {
            morto = true;
            RegistrarPontuacaoSeInimigo();
            if (animator != null)
            {
                animator.SetTrigger("Morte");
            }
            if (gameObject.tag == "Player")
            {  // Só reicicia a fase se quem morreu foi o jogador.
                StartCoroutine(morre());
            }
        }
    }

    public void danoMax()
    {
        if (morto)
        {
            return;
        }

        saude = 0;
        morto = true;
        RegistrarPontuacaoSeInimigo();
        if (animator != null)
        {
            animator.SetTrigger("Morte");
        }
        if (gameObject.tag == "Player")
        {  // Só reicicia a fase se quem morreu foi o jogador.
            StartCoroutine(morre());
        }
    }

    IEnumerator morre()
    {
        yield return new WaitForSeconds(2.0f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void RegistrarPontuacaoSeInimigo()
    {
        if (gameObject.tag == "Player" || GetComponent<EnemyHealth>() != null)
        {
            return;
        }

        ScoreManager.EnsureInstance().RegisterEnemyDefeated(gameObject);
    }
}

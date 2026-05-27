using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controle : MonoBehaviour
{
    public int velocidade = 10;
    public float forcaDoPulo = 12f;

    public Transform terra;
    public LayerMask chao;
    public float raioChao = 0.2f;

    private float moveX;
    private bool direita = true;
    private bool noChao;

    private Animator animator;
    private Rigidbody2D rb;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            Debug.LogError("Animator não encontrado no Protagonista!");

        if (rb == null)
            Debug.LogError("Rigidbody2D não encontrado no Protagonista!");
    }

    void Update()
    {
        moveJogador();
        viraJogador();
    }

    void moveJogador()
    {
        // INPUT
        moveX = Input.GetAxis("Horizontal");

        noChao = Physics2D.OverlapCircle(terra.position, raioChao, chao);

        if (Input.GetKeyDown(KeyCode.J))
        {
            ataca();
        }

        if (Input.GetButtonDown("Jump") && noChao)
        {
            pula();
        }

        // MOVIMENTO
        rb.velocity = new Vector2(moveX * velocidade, rb.velocity.y);

        // ANIMAÇÃO
        if (animator != null)
        {
            animator.SetBool("NoChao", noChao);
            animator.SetBool("Correndo", moveX != 0);
        }
    }

    void ataca()
    {
        if (animator != null)
            animator.SetTrigger("Ataque");
    }

    void pula()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * forcaDoPulo, ForceMode2D.Impulse);
    }

    void viraJogador()
    {
        if (moveX > 0)
            direita = true;
        else if (moveX < 0)
            direita = false;

        Vector2 escala = transform.localScale;

        if ((escala.x > 0 && !direita) || (escala.x < 0 && direita))
        {
            escala.x *= -1;
            transform.localScale = escala;
        }
    }

    void OnCollisionEnter2D(Collision2D outro)
    {
        if (outro.gameObject.CompareTag("PlataformaMovel"))
        {
            transform.parent = outro.transform;
        }
    }

    void OnCollisionExit2D(Collision2D outro)
    {
        if (outro.gameObject.CompareTag("PlataformaMovel"))
        {
            transform.parent = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (terra != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(terra.position, raioChao);
        }
    }
}
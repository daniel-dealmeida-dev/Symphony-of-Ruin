using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controle : MonoBehaviour
{

    public int velocidade = 10;
    public int forcaDoPulo = 1250;
    public Transform terra;
    public LayerMask chao;

    private float moveX;
    private bool direita = true;
    private bool noChao;
    private Animator animator;
    private Rigidbody2D corpoRigido;

    // Use this for initialization
    void Start()
    {
        GameServices.EnsureInstance();
        animator = gameObject.GetComponent<Animator>();
        corpoRigido = gameObject.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        moveJogador();
    }

    private void LateUpdate()
    {
        viraJogador();
    }

    void moveJogador()
    {
        // CONTROLES
        moveX = GameServices.Instance.Settings.GetHorizontal();
        noChao = terra != null && Physics2D.Linecast(transform.position, terra.position, chao);
        bool ataquePressionado =
            GameServices.Instance.Settings.GetButtonDown(GameAction.AttackLine1) ||
            GameServices.Instance.Settings.GetButtonDown(GameAction.AttackLine2) ||
            GameServices.Instance.Settings.GetButtonDown(GameAction.AttackLine3) ||
            GameServices.Instance.Settings.GetButtonDown(GameAction.AttackLine4);
        bool puloPressionado = GameServices.Instance.Settings.GetButtonDown(GameAction.Jump);
        if (ataquePressionado)
        {
            ataca();
        }
        if (puloPressionado && noChao)
        {
            pula();
        }

        // FÍSICA
        if (corpoRigido != null)
        {
            corpoRigido.velocity = new Vector2(moveX * velocidade, corpoRigido.velocity.y);
        }

        int camadaChao = LayerMask.NameToLayer("chao");
        if (camadaChao >= 0 && corpoRigido != null)
        {
            Physics2D.IgnoreLayerCollision(this.gameObject.layer, camadaChao, corpoRigido.velocity.y > 0.0f);
        }

        // ANIMAÇAO
        if (animator == null)
        {
            return;
        }

        animator.SetBool("NoChao", noChao);

        if (moveX != 0)
        {
            animator.SetBool("Correndo", true);
        }
        else
        {
            animator.SetBool("Correndo", false);
        }
    }

    void ataca(){
        if (animator != null)
        {
            animator.SetTrigger("Ataque");
        }
    }

    void pula(){
        if (corpoRigido != null)
        {
            corpoRigido.AddForce(Vector2.up * forcaDoPulo);
        }
    }

    void viraJogador()
    {
        if (moveX > 0){
            direita = true;
        }
        else if(moveX < 0){
            direita = false;
        }
        Vector2 escala = transform.localScale;
        if((escala.x > 0 && !direita) || (escala.x < 0 && direita)){
            escala.x = escala.x * -1;
            transform.localScale = escala;
        }
    }

	// Código da plataforma movel
	void OnCollisionEnter2D(Collision2D outro)
	{
        if(outro.gameObject.tag=="PlataformaMovel"){
            this.transform.parent = outro.transform;
        }
	}

	private void OnCollisionExit2D(Collision2D outro)
	{
        if (outro.gameObject.tag == "PlataformaMovel")
        {
            this.transform.parent = null;
        }
	}
}

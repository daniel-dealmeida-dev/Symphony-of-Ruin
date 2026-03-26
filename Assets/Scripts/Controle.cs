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

    // Use this for initialization
    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
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
        moveX = Input.GetAxis("Horizontal");
        noChao = Physics2D.Linecast(transform.position, terra.position, chao);
        if (Input.GetButtonDown("Fire1"))
        {
            ataca();
        }
        if (Input.GetButtonDown("Jump") && noChao)
        {
            pula();
        }

        // FÍSICA
        gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(moveX * velocidade,
                                                                      gameObject.GetComponent<Rigidbody2D>().velocity.y);

        Physics2D.IgnoreLayerCollision(this.gameObject.layer, LayerMask.NameToLayer("chao"),
                                       (gameObject.GetComponent<Rigidbody2D>().velocity.y > 0.0f));

        // ANIMAÇAO
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
        animator.SetTrigger("Ataque");
    }

    void pula(){
        GetComponent<Rigidbody2D>().AddForce(Vector2.up * forcaDoPulo);
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

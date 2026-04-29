using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controle : MonoBehaviour
{

    public int velocidade = 10;
    public int forcaDoPulo = 1250;
    public Transform terra;
    public LayerMask chao;
    public FixedJoystick joystick;
    [SerializeField, Range(0f, 1f)] private float joystickDeadZone = 0.15f;
    [SerializeField, Range(0f, 1f)] private float joystickJumpThreshold = 0.65f;

    private float moveX;
    private bool direita = true;
    private bool noChao;
    private bool joystickJumpHeld;
    private Animator animator;
    private Rigidbody2D corpo;
    private MobileJoystick mobileJoystick;

    // Use this for initialization
    void Start()
    {
        animator = gameObject.GetComponent<Animator>();
        corpo = gameObject.GetComponent<Rigidbody2D>();

        if (joystick == null)
        {
            joystick = FindFirstObjectByType<FixedJoystick>(FindObjectsInactive.Include);
        }

        if (joystick == null)
        {
            mobileJoystick = MobileJoystick.EnsureInScene();
        }
        else
        {
            mobileJoystick = FindFirstObjectByType<MobileJoystick>(FindObjectsInactive.Include);
        }
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
        moveX = pegaMovimentoHorizontal();
        noChao = Physics2D.Linecast(transform.position, terra.position, chao);
        if (Input.GetButtonDown("Fire1"))
        {
            ataca();
        }
        if ((Input.GetButtonDown("Jump") || joystickPediuPulo()) && noChao)
        {
            pula();
        }

        // FÍSICA
        corpo.velocity = new Vector2(moveX * velocidade, corpo.velocity.y);

        Physics2D.IgnoreLayerCollision(this.gameObject.layer, LayerMask.NameToLayer("chao"),
                                       (corpo.velocity.y > 0.0f));

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

    float pegaMovimentoHorizontal()
    {
        float entradaJoystick = 0f;

        if (joystick != null)
        {
            entradaJoystick = joystick.Horizontal;
        }

        if (Mathf.Abs(entradaJoystick) < joystickDeadZone && mobileJoystick != null)
        {
            entradaJoystick = mobileJoystick.Horizontal;
        }

        if (Mathf.Abs(entradaJoystick) >= joystickDeadZone)
        {
            return entradaJoystick;
        }

        return Input.GetAxis("Horizontal");
    }

    bool joystickPediuPulo()
    {
        float vertical = 0f;

        if (joystick != null)
        {
            vertical = joystick.Vertical;
        }

        if (Mathf.Abs(vertical) < joystickDeadZone && mobileJoystick != null)
        {
            vertical = mobileJoystick.Vertical;
        }

        bool pressionandoParaCima = vertical >= joystickJumpThreshold;
        bool pediuPulo = pressionandoParaCima && !joystickJumpHeld;
        joystickJumpHeld = pressionandoParaCima;

        return pediuPulo;
    }

    void ataca(){
        animator.SetTrigger("Ataque");
    }

    void pula(){
        corpo.AddForce(Vector2.up * forcaDoPulo);
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

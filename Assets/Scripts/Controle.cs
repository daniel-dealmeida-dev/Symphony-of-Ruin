using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controle : MonoBehaviour
{
    [Header("Movimento")]
    public int velocidade = 10;
    public int forcaDoPulo = 1250;

    [Header("Vida")]
    public int vida = 3;

    [Header("Chão")]
    public Transform terra;
    public LayerMask chao;

    [Header("Joystick")]
    public FixedJoystick joystick;

    [SerializeField, Range(0f, 1f)]
    private float joystickDeadZone = 0.15f;

    [SerializeField, Range(0f, 1f)]
    private float joystickJumpThreshold = 0.65f;

    private float moveX;
    private bool direita = true;
    private bool noChao;
    private bool joystickJumpHeld;

    private Animator animator;
    private Rigidbody2D corpo;
    private MobileJoystick mobileJoystick;

    // START
    void Start()
    {
        animator = GetComponent<Animator>();
        corpo = GetComponent<Rigidbody2D>();

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

    // UPDATE
    void Update()
    {
        moveJogador();

        // TESTE DE DANO
        if (Input.GetKeyDown(KeyCode.K))
        {
            TomarDano(1);
        }
    }

    private void LateUpdate()
    {
        viraJogador();
    }

    // MOVIMENTO
    void moveJogador()
    {
        // CONTROLES
        moveX = pegaMovimentoHorizontal();

        noChao = Physics2D.Linecast(
            transform.position,
            terra.position,
            chao
        );

        // ATAQUE
        if (Input.GetButtonDown("Fire1"))
        {
            ataca();
        }

        // PULO
        if ((Input.GetButtonDown("Jump") || joystickPediuPulo()) && noChao)
        {
            pula();
        }

        // FÍSICA
        corpo.velocity = new Vector2(
            moveX * velocidade,
            corpo.velocity.y
        );

        Physics2D.IgnoreLayerCollision(
            this.gameObject.layer,
            LayerMask.NameToLayer("chao"),
            (corpo.velocity.y > 0.0f)
        );

        // ANIMAÇÕES
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

    // MOVIMENTO HORIZONTAL
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

    // PULO MOBILE
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

    // ATAQUE
    void ataca()
    {
        animator.SetTrigger("Ataque");
    }

    // PULO
    void pula()
    {
        corpo.AddForce(Vector2.up * forcaDoPulo);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJump();
        }
    }

    // VIRAR PERSONAGEM
    void viraJogador()
    {
        if (moveX > 0)
        {
            direita = true;
        }
        else if (moveX < 0)
        {
            direita = false;
        }

        Vector2 escala = transform.localScale;

        if ((escala.x > 0 && !direita) || (escala.x < 0 && direita))
        {
            escala.x *= -1;
            transform.localScale = escala;
        }
    }

    // TOMAR DANO
    public void TomarDano(int dano)
    {
        vida -= dano;

        Debug.Log("Vida atual: " + vida);

        if (vida <= 0)
        {
            Morrer();
        }
    }

    public void ForcarMorte()
    {
        vida = 0;
        Morrer();
    }

    // MORTE
    void Morrer()
    {
        animator.SetTrigger("Morte");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDeathOrHit();
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.FinalizarJogo();
        }

        Destroy(gameObject, 1f);
    }

    // PLATAFORMA MÓVEL
    void OnCollisionEnter2D(Collision2D outro)
    {
        if (outro.gameObject.tag == "PlataformaMovel")
        {
            transform.parent = outro.transform;
        }
    }

    void OnCollisionExit2D(Collision2D outro)
    {
        if (outro.gameObject.tag == "PlataformaMovel")
        {
            transform.parent = null;
        }
    }
}
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
    [Tooltip("Distância extra para raycast/shape cast a partir da base do collider.")]
    [SerializeField] private float alcanceChecagemChao = 0.12f;

    [Tooltip("Ignora solo enquanto o corpo já está subindo (evita pulo infinito com IgnoreLayerCollision).")]
    [SerializeField] private float velocidadeMaximaYZeradaNoChao = 0.08f;

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
    private Collider2D hitbox;
    private MobileJoystick mobileJoystick;

    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[4];

    // START
    void Start()
    {
        animator = GetComponent<Animator>();
        corpo = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();

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

        noChao = EstaApoiadoNoChao();

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

    bool EstaApoiadoNoChao()
    {
        ContactFilter2D filtro = new ContactFilter2D();
        filtro.SetLayerMask(chao);
        filtro.useTriggers = false;

        if (hitbox != null)
        {
            int encontrados = hitbox.Cast(Vector2.down, filtro, _hitBuffer, alcanceChecagemChao);

            const float tolDistancia = 0.011f;

            for (int i = 0; i < encontrados; i++)
            {
                if (_hitBuffer[i].collider == null ||
                    ReferenceEquals(_hitBuffer[i].collider.gameObject, gameObject))
                {
                    continue;
                }

                if (_hitBuffer[i].distance <= alcanceChecagemChao + tolDistancia &&
                    VelocidadePermiteConsiderarNoChao())
                {
                    return true;
                }
            }

            return false;
        }

        Vector2 pes = terra != null ? (Vector2)terra.position : (Vector2)transform.position;

        pes += Vector2.up * 0.02f;
        RaycastHit2D golpe = Physics2D.Raycast(pes, Vector2.down, 0.04f + alcanceChecagemChao, chao);

        bool bateNoChao = golpe.collider != null && golpe.collider.gameObject != gameObject;

        return bateNoChao && VelocidadePermiteConsiderarNoChao();
    }

    bool VelocidadePermiteConsiderarNoChao()
    {
        return corpo == null || corpo.velocity.y <= velocidadeMaximaYZeradaNoChao;
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

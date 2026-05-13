using UnityEngine;

public class Controle : MonoBehaviour
{
    [Header("Vida")]
    public int vida = 3;

    [Header("Chão")]
    public Transform terra;
    public LayerMask chao;

    private bool noChao;
    private bool direita = true;

    private Animator animator;
    private PlayerMovement movement;

    void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        VerificarChao();

        if (movement != null)
            movement.ProcessarMovimento();

        ProcessarPulo();
        ProcessarAtaque();
        AtualizarAnimacoes();
        viraJogador();
    }

    void FixedUpdate()
    {
        if (movement != null)
            movement.AplicarMovimento();
    }

    // ======================
    // CHÃO
    // ======================

    void VerificarChao()
    {
        if (terra == null)
        {
            noChao = false;
            return;
        }

        noChao = Physics2D.Linecast(
            transform.position,
            terra.position,
            chao
        );
    }

    // ======================
    // PULO
    // ======================

    void ProcessarPulo()
    {
        bool pulo = Input.GetButtonDown("Jump");

        bool joystickUp = false;

        var joy = GetComponent<PlayerMovement>();
        if (joy != null && joy.joystick != null)
        {
            joystickUp = joy.joystick.Vertical > 0.7f;
        }

        if ((pulo || joystickUp) && noChao)
        {
            pula();
        }
    }

    void pula()
    {
        Rigidbody2D corpo = GetComponent<Rigidbody2D>();

        if (corpo == null) return;

        corpo.velocity = new Vector2(corpo.velocity.x, 0f);
        corpo.AddForce(Vector2.up * 1250f);
    }

    // ======================
    // ATAQUE
    // ======================

    void ProcessarAtaque()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            ataca();
        }
    }

    void ataca()
    {
        if (animator != null)
            animator.SetTrigger("Ataque");
    }

    // ======================
    // ANIMAÇÕES
    // ======================

    void AtualizarAnimacoes()
    {
        if (animator == null) return;

        animator.SetBool("NoChao", noChao);

        float moveX = movement != null ? movement.GetMoveX() : 0f;

        animator.SetBool("Correndo", Mathf.Abs(moveX) > 0.1f);
    }

    // ======================
    // VIRAR PERSONAGEM
    // ======================

    void viraJogador()
    {
        float moveX = movement != null ? movement.GetMoveX() : 0f;

        if (moveX > 0)
            direita = true;
        else if (moveX < 0)
            direita = false;

        Vector3 scale = transform.localScale;

        if ((scale.x > 0 && !direita) ||
            (scale.x < 0 && direita))
        {
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    // ======================
    // VIDA
    // ======================

    public void TomarDano(int dano)
    {
        vida -= dano;

        if (vida <= 0)
            Morrer();
    }

    void Morrer()
    {
        if (animator != null)
            animator.SetTrigger("Morte");

        if (GameManager.instance != null)
            GameManager.instance.FinalizarJogo();

        Destroy(gameObject, 1f);
    }
}
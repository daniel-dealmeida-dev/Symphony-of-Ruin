using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class MovimentoJogador : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float velocidade = 7.5f;
    [SerializeField] private float forcaPulo = 14f;
    [SerializeField] private float maxVelocidadeQueda = -24f;
    [SerializeField] private Rigidbody2D corpoRigido;
    [SerializeField] private Collider2D colisor;
    [SerializeField] private LayerMask mascaraChao = ~0;
    [SerializeField] private float distanciaChecagemChao = 0.16f;

    [Header("Combate")]
    [SerializeField] private int danoAtaque = 1;
    [SerializeField] private float intervaloAtaque = 0.34f;
    [SerializeField] private Vector2 tamanhoAtaque = new Vector2(1.35f, 1.05f);
    [SerializeField] private Vector2 deslocamentoAtaque = new Vector2(0.85f, 0.05f);
    [SerializeField] private LayerMask mascaraAtaque = ~0;

    private readonly HashSet<string> parametrosAnimator = new HashSet<string>();
    private float moveX;
    private float proximoAtaque;
    private bool direita = true;
    private bool noChao;
    private bool atacando;
    private bool controlesAtivos = true;
    private Animator animator;
    private string estadoAtual;

    public bool EstaViradoParaDireita
    {
        get { return direita; }
    }

    public bool EstaNoChao
    {
        get { return noChao; }
    }

    private void Awake()
    {
        GameServices.EnsureInstance();
        if (GameManager.gm == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        ResolverComponentes();
        ConfigurarFisica();
        CachearParametrosAnimator();
    }

    private void Update()
    {
        AtualizaNoChao();

        if (!PodeReceberComando())
        {
            moveX = 0f;
            AtualizaAnimacao();
            return;
        }

        moveX = GameServices.Instance.Settings.GetHorizontal();

        if (GameServices.Instance.Settings.GetButtonDown(GameAction.Jump) && noChao)
        {
            Pular();
        }

        if (GameServices.Instance.Settings.GetButtonDown(GameAction.Fire))
        {
            Atacar();
        }

        AtualizaAnimacao();
    }

    private void FixedUpdate()
    {
        if (corpoRigido == null || !controlesAtivos)
        {
            return;
        }

        Vector2 velocidadeAtual = corpoRigido.velocity;
        velocidadeAtual.x = moveX * velocidade;
        if (velocidadeAtual.y < maxVelocidadeQueda)
        {
            velocidadeAtual.y = maxVelocidadeQueda;
        }

        corpoRigido.velocity = velocidadeAtual;
    }

    private void LateUpdate()
    {
        VirarJogador();
    }

    public void SetControlesAtivos(bool ativo)
    {
        controlesAtivos = ativo;
        if (!ativo)
        {
            moveX = 0f;
        }
    }

    public void AplicarEmpurrao(Vector2 origem, float forca)
    {
        if (corpoRigido == null || forca <= 0f)
        {
            return;
        }

        float direcao = transform.position.x >= origem.x ? 1f : -1f;
        corpoRigido.velocity = new Vector2(direcao * forca, Mathf.Max(corpoRigido.velocity.y, forca * 0.35f));
    }

    public void TocarMorte()
    {
        PlayEstado("Morte", "Death", "Derrotado");
        SetTriggerSeExistir("Morte");
        SetTriggerSeExistir("Death");
    }

    private void ResolverComponentes()
    {
        if (corpoRigido == null)
        {
            corpoRigido = GetComponent<Rigidbody2D>();
        }

        if (colisor == null)
        {
            colisor = GetComponent<Collider2D>();
        }

        animator = GetComponent<Animator>();
    }

    private void ConfigurarFisica()
    {
        if (corpoRigido == null)
        {
            return;
        }

        corpoRigido.freezeRotation = true;
        corpoRigido.gravityScale = Mathf.Max(1f, corpoRigido.gravityScale);
        corpoRigido.interpolation = RigidbodyInterpolation2D.Interpolate;
        corpoRigido.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void CachearParametrosAnimator()
    {
        parametrosAnimator.Clear();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            parametrosAnimator.Add(parametro.name);
        }
    }

    private bool PodeReceberComando()
    {
        if (!controlesAtivos)
        {
            return false;
        }

        if (GameManager.gm != null && (GameManager.gm.jogoPausado || GameManager.gm.gameIsOver))
        {
            return false;
        }

        PlayerHealth vida = GetComponent<PlayerHealth>();
        return vida == null || vida.IsAlive;
    }

    private void AtualizaNoChao()
    {
        if (colisor == null)
        {
            noChao = false;
            return;
        }

        Bounds bounds = colisor.bounds;
        float largura = Mathf.Max(0.08f, bounds.size.x * 0.82f);
        Vector2 origem = new Vector2(bounds.center.x, bounds.min.y + 0.04f);
        RaycastHit2D hit = Physics2D.BoxCast(origem, new Vector2(largura, 0.06f), 0f, Vector2.down, distanciaChecagemChao, mascaraChao);
        noChao = hit.collider != null && hit.collider != colisor && !hit.collider.isTrigger;
    }

    private void Pular()
    {
        if (corpoRigido == null)
        {
            return;
        }

        Vector2 velocidadeAtual = corpoRigido.velocity;
        velocidadeAtual.y = 0f;
        corpoRigido.velocity = velocidadeAtual;
        corpoRigido.AddForce(Vector2.up * forcaPulo, ForceMode2D.Impulse);
        PlayEstado("PuloAnim", "Pulo", "Jump");
        SetTriggerSeExistir("Pulo");
        SetTriggerSeExistir("Jump");
    }

    private void Atacar()
    {
        if (Time.time < proximoAtaque)
        {
            return;
        }

        proximoAtaque = Time.time + intervaloAtaque;
        atacando = true;
        PlayEstado("Ataque", "Attack");
        SetTriggerSeExistir("Ataque");
        StartCoroutine(FinalizarAtaque());

        Vector2 centro = (Vector2)transform.position + new Vector2((direita ? 1f : -1f) * deslocamentoAtaque.x, deslocamentoAtaque.y);
        Collider2D[] atingidos = Physics2D.OverlapBoxAll(centro, tamanhoAtaque, 0f, mascaraAtaque);
        foreach (Collider2D alvo in atingidos)
        {
            if (alvo == null || alvo.isTrigger || alvo.transform == transform || alvo.transform.IsChildOf(transform))
            {
                continue;
            }

            EnemyHealth inimigo = alvo.GetComponentInParent<EnemyHealth>();
            if (inimigo != null)
            {
                inimigo.TakeDamage(danoAtaque, transform.position);
                continue;
            }

            Saude saudeLegada = alvo.GetComponentInParent<Saude>();
            if (saudeLegada != null && !alvo.CompareTag("Player"))
            {
                saudeLegada.dano(danoAtaque);
            }
        }
    }

    private IEnumerator FinalizarAtaque()
    {
        yield return new WaitForSeconds(intervaloAtaque * 0.72f);
        atacando = false;
    }

    private void AtualizaAnimacao()
    {
        bool andando = Mathf.Abs(moveX) > 0.05f;
        SetBoolSeExistir("NoChao", noChao);
        SetBoolSeExistir("noChao", noChao);
        SetBoolSeExistir("Grounded", noChao);
        SetBoolSeExistir("Correndo", andando);
        SetBoolSeExistir("correndo", andando);
        SetBoolSeExistir("Andando", andando);
        SetBoolSeExistir("Running", andando);

        if (atacando)
        {
            return;
        }

        if (!noChao)
        {
            PlayEstado("PuloAnim", "Pulo", "Jump");
        }
        else if (andando)
        {
            PlayEstado("perdoAndando", "Correndo", "Run", "Andando");
        }
        else
        {
            PlayEstado("parado", "Idle");
        }
    }

    private void VirarJogador()
    {
        if (moveX > 0.05f)
        {
            direita = true;
        }
        else if (moveX < -0.05f)
        {
            direita = false;
        }

        Vector2 escala = transform.localScale;
        if ((escala.x > 0f && !direita) || (escala.x < 0f && direita))
        {
            escala.x *= -1f;
            transform.localScale = escala;
        }
    }

    private void PlayEstado(params string[] nomes)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        foreach (string nome in nomes)
        {
            int hash = Animator.StringToHash(nome);
            if (!animator.HasState(0, hash))
            {
                continue;
            }

            if (estadoAtual != nome)
            {
                animator.Play(hash, 0, 0f);
                estadoAtual = nome;
            }

            return;
        }
    }

    private void SetBoolSeExistir(string nome, bool valor)
    {
        if (animator != null && parametrosAnimator.Contains(nome))
        {
            animator.SetBool(nome, valor);
        }
    }

    private void SetTriggerSeExistir(string nome)
    {
        if (animator != null && parametrosAnimator.Contains(nome))
        {
            animator.SetTrigger(nome);
        }
    }

    private void OnCollisionEnter2D(Collision2D outro)
    {
        if (outro.gameObject.tag == "PlataformaMovel")
        {
            transform.parent = outro.transform;
        }
    }

    private void OnCollisionExit2D(Collision2D outro)
    {
        if (outro.gameObject.tag == "PlataformaMovel")
        {
            transform.parent = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        bool olhandoDireita = transform.localScale.x >= 0f;
        Vector2 centro = (Vector2)transform.position + new Vector2((olhandoDireita ? 1f : -1f) * deslocamentoAtaque.x, deslocamentoAtaque.y);
        Gizmos.DrawWireCube(centro, tamanhoAtaque);
    }
}

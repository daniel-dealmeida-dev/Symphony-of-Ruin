using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class MovimentoJogador : MonoBehaviour
{
    private const string RecursoSpritesAtaqueGuitarraPadrao = PlayerAttackSpriteVersions.DefaultResourcePath;

    [Header("Movimento")]
    [SerializeField] private float velocidade = 7.5f;
    [SerializeField] private float aceleracaoChao = 72f;
    [SerializeField] private float desaceleracaoChao = 88f;
    [SerializeField] private float aceleracaoAr = 42f;
    [SerializeField] private float desaceleracaoAr = 34f;
    [Header("Pulo")]
    [SerializeField] private float alturaPuloMaxima = 2.8f;
    [SerializeField] private float alturaPuloMinima = 1.15f;
    [SerializeField] private float tempoAteTopoPulo = 0.38f;
    [SerializeField] private float maxVelocidadeQueda = -20f;
    [SerializeField] private Rigidbody2D corpoRigido;
    [SerializeField] private Collider2D colisor;
    [SerializeField] private LayerMask mascaraChao = ~0;
    [SerializeField] private float distanciaChecagemChao = 0.18f;
    [SerializeField] private float tempoCoyote = 0.12f;
    [SerializeField] private float tempoBufferPulo = 0.12f;
    [SerializeField] private float tempoBloqueioChaoAposPulo = 0.08f;
    [SerializeField] private float multiplicadorQueda = 1.35f;
    [SerializeField] private float multiplicadorPuloCurto = 2.25f;
    [SerializeField] private float multiplicadorTopoPulo = 0.9f;
    [SerializeField] private float limiarVelocidadeTopoPulo = 1.2f;

    [Header("Combate")]
    [SerializeField] private int danoAtaque = 1;
    [SerializeField] private float intervaloAtaque = 0.5f;
    [SerializeField] private float atrasoHitAtaque = 0.25f;
    [SerializeField] private float duracaoHitAtaque = 0.1f;
    [SerializeField] private float duracaoAnimacaoAtaque = 0.5f;
    [SerializeField] private Vector2 tamanhoAtaque = new Vector2(1.45f, 1.05f);
    [SerializeField] private Vector2 deslocamentoAtaque = new Vector2(0.9f, 0.05f);
    [SerializeField] private LayerMask mascaraAtaque = ~0;

    [Header("Animacao do Ataque")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private string recursoSpritesAtaqueGuitarra = RecursoSpritesAtaqueGuitarraPadrao;
    [SerializeField] private int linhasAtaqueGuitarra = 4;
    [SerializeField] private int[] quadrosPorLinhaAtaqueGuitarra = { 11, 9, 11, 13 };
    [SerializeField] private float quadrosPorSegundoAtaqueGuitarra = 24f;

    [Header("Animacao de Dano")]
    [SerializeField] private float duracaoAnimacaoDano = 0.25f;

    private readonly HashSet<string> parametrosAnimator = new HashSet<string>();
    private float moveX;
    private float proximoAtaque;
    private float contadorCoyote;
    private float contadorBufferPulo;
    private float contadorBloqueioChao;
    private bool direita = true;
    private bool noChao;
    private bool atacando;
    private bool puloSegurado;
    private bool controlesAtivos = true;
    private Animator animator;
    private string estadoAtual;
    private Coroutine rotinaAtaque;
    private Coroutine rotinaDano;
    private Sprite[][] quadrosAtaqueGuitarra;
    private int proximoAtaqueGuitarra;
    private bool animatorControladoPorAtaque;
    private bool animatorHabilitadoAntesDoAtaque;
    private bool recebendoDano;
    private bool morto;

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
        CarregarQuadrosAtaqueGuitarra();
        direita = transform.localScale.x >= 0f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(recursoSpritesAtaqueGuitarra))
        {
            recursoSpritesAtaqueGuitarra = RecursoSpritesAtaqueGuitarraPadrao;
        }
    }
#endif

    private void Update()
    {
        AtualizaNoChao();
        AtualizaTemporizadoresPulo();

        if (!PodeReceberComando())
        {
            moveX = 0f;
            puloSegurado = false;
            if (!morto)
            {
                AtualizaAnimacao();
            }

            return;
        }

        moveX = GameServices.Instance.Settings.GetHorizontal();
        AtualizarDirecaoPorEntrada();
        puloSegurado = GameServices.Instance.Settings.GetButton(GameAction.Jump);

        if (GameServices.Instance.Settings.GetButtonDown(GameAction.Jump))
        {
            contadorBufferPulo = tempoBufferPulo;
        }

        if (GameServices.Instance.Settings.GetButtonUp(GameAction.Jump))
        {
            CortarPuloSeNecessario();
        }

        if (contadorBufferPulo > 0f && contadorCoyote > 0f)
        {
            Pular();
        }

        bool ataqueSolicitado;
        int indiceAtaqueVisual = ObterIndiceAtaqueSolicitado(out ataqueSolicitado);
        if (ataqueSolicitado)
        {
            Atacar(indiceAtaqueVisual);
        }

        AtualizaAnimacao();
    }

    private void FixedUpdate()
    {
        if (corpoRigido == null)
        {
            return;
        }

        Vector2 velocidadeAtual = corpoRigido.velocity;
        float velocidadeAlvo = controlesAtivos ? moveX * velocidade : 0f;
        float aceleracao = Mathf.Abs(velocidadeAlvo) > 0.01f
            ? (noChao ? aceleracaoChao : aceleracaoAr)
            : (noChao ? desaceleracaoChao : desaceleracaoAr);

        velocidadeAtual.x = Mathf.MoveTowards(velocidadeAtual.x, velocidadeAlvo, aceleracao * Time.fixedDeltaTime);

        AplicarGravidadePulo(ref velocidadeAtual);

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
        if (ativo)
        {
            morto = false;
        }

        if (!ativo)
        {
            moveX = 0f;
            puloSegurado = false;
            if (corpoRigido != null)
            {
                corpoRigido.velocity = new Vector2(0f, corpoRigido.velocity.y);
            }
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
        if (rotinaAtaque != null)
        {
            StopCoroutine(rotinaAtaque);
            rotinaAtaque = null;
        }

        if (rotinaDano != null)
        {
            StopCoroutine(rotinaDano);
            rotinaDano = null;
        }

        recebendoDano = false;
        FinalizarAtaqueVisual(false);
        GarantirAnimatorDisponivel();
        morto = true;
        PlayEstado("Morte", "Death", "Derrotado");
        SetTriggerSeExistir("Morte");
        SetTriggerSeExistir("Death");
        if (animator != null && animator.enabled)
        {
            animator.Update(0f);
        }
    }

    public float ObterDuracaoAnimacaoMorte(float duracaoPadrao)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return duracaoPadrao;
        }

        float duracao = 0f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip == null || !NomeClipMorte(clip.name))
            {
                continue;
            }

            duracao = Mathf.Max(duracao, clip.length);
        }

        return duracao > 0f ? duracao : duracaoPadrao;
    }

    public void TocarDano()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (rotinaDano != null)
        {
            StopCoroutine(rotinaDano);
        }

        rotinaDano = StartCoroutine(ExecutarDanoVisual());
    }

    public void SolicitarAtaqueGuitarra(int indiceAtaqueVisual)
    {
        if (!PodeReceberComando())
        {
            return;
        }

        int indiceSeguro = Mathf.Clamp(indiceAtaqueVisual, 0, Mathf.Max(0, linhasAtaqueGuitarra - 1));
        AtualizarDirecaoPorEntrada();
        Atacar(indiceSeguro);
    }

    private void OnDisable()
    {
        if (rotinaAtaque != null)
        {
            StopCoroutine(rotinaAtaque);
            rotinaAtaque = null;
        }

        if (rotinaDano != null)
        {
            StopCoroutine(rotinaDano);
            rotinaDano = null;
        }

        recebendoDano = false;
        FinalizarAtaqueVisual(false);
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
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
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
        if (contadorBloqueioChao > 0f)
        {
            contadorBloqueioChao = Mathf.Max(0f, contadorBloqueioChao - Time.deltaTime);
            noChao = false;
            return;
        }

        if (colisor == null)
        {
            noChao = false;
            return;
        }

        Bounds bounds = colisor.bounds;
        float largura = Mathf.Max(0.08f, bounds.size.x * 0.82f);
        Vector2 origem = new Vector2(bounds.center.x, bounds.min.y + 0.04f);
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origem, new Vector2(largura, 0.06f), 0f, Vector2.down, distanciaChecagemChao, mascaraChao);
        noChao = false;
        float menorDistancia = float.MaxValue;
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D outroColisor = hit.collider;
            if (outroColisor == null || outroColisor == colisor || outroColisor.isTrigger)
            {
                continue;
            }

            if (corpoRigido != null && outroColisor.attachedRigidbody == corpoRigido)
            {
                continue;
            }

            if (outroColisor.transform == transform || outroColisor.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < menorDistancia)
            {
                menorDistancia = hit.distance;
                noChao = true;
            }
        }
    }

    private void AtualizaTemporizadoresPulo()
    {
        if (noChao)
        {
            contadorCoyote = tempoCoyote;
        }
        else
        {
            contadorCoyote = Mathf.Max(0f, contadorCoyote - Time.deltaTime);
        }

        if (contadorBufferPulo > 0f)
        {
            contadorBufferPulo = Mathf.Max(0f, contadorBufferPulo - Time.deltaTime);
        }
    }

    private void Pular()
    {
        if (corpoRigido == null)
        {
            return;
        }

        contadorCoyote = 0f;
        contadorBufferPulo = 0f;
        contadorBloqueioChao = tempoBloqueioChaoAposPulo;
        noChao = false;
        Vector2 velocidadeAtual = corpoRigido.velocity;
        velocidadeAtual.y = CalcularVelocidadeInicialPulo();
        corpoRigido.velocity = velocidadeAtual;
        PlayEstado("PuloAnim", "Pulo", "Jump");
        SetTriggerSeExistir("Pulo");
        SetTriggerSeExistir("Jump");
    }

    private void CortarPuloSeNecessario()
    {
        if (corpoRigido == null || corpoRigido.velocity.y <= 0f)
        {
            return;
        }

        float velocidadeMinima = CalcularVelocidadeMinimaPulo();
        corpoRigido.velocity = new Vector2(corpoRigido.velocity.x, Mathf.Min(corpoRigido.velocity.y, velocidadeMinima));
        puloSegurado = false;
    }

    private void AplicarGravidadePulo(ref Vector2 velocidadeAtual)
    {
        if (corpoRigido == null || noChao)
        {
            return;
        }

        float gravidadeDesejada = CalcularGravidadePulo();
        float multiplicador = 1f;
        if (velocidadeAtual.y < -limiarVelocidadeTopoPulo)
        {
            multiplicador = multiplicadorQueda;
        }
        else if (velocidadeAtual.y > limiarVelocidadeTopoPulo && !puloSegurado)
        {
            multiplicador = multiplicadorPuloCurto;
        }
        else if (Mathf.Abs(velocidadeAtual.y) <= limiarVelocidadeTopoPulo && puloSegurado)
        {
            multiplicador = multiplicadorTopoPulo;
        }

        float aceleracaoDesejada = -gravidadeDesejada * multiplicador;
        float aceleracaoBaseUnity = Physics2D.gravity.y * corpoRigido.gravityScale;
        velocidadeAtual.y += (aceleracaoDesejada - aceleracaoBaseUnity) * Time.fixedDeltaTime;
    }

    private float CalcularVelocidadeInicialPulo()
    {
        return CalcularGravidadePulo() * Mathf.Max(0.05f, tempoAteTopoPulo);
    }

    private float CalcularVelocidadeMinimaPulo()
    {
        float alturaMinima = Mathf.Clamp(alturaPuloMinima, 0.2f, alturaPuloMaxima);
        return Mathf.Sqrt(2f * CalcularGravidadePulo() * alturaMinima);
    }

    private float CalcularGravidadePulo()
    {
        float tempoTopoSeguro = Mathf.Max(0.05f, tempoAteTopoPulo);
        float alturaSegura = Mathf.Max(0.2f, alturaPuloMaxima);
        return (2f * alturaSegura) / (tempoTopoSeguro * tempoTopoSeguro);
    }

    private void Atacar()
    {
        Atacar(ObterProximoAtaqueGuitarra());
    }

    private void Atacar(int indiceAtaqueVisual)
    {
        if (Time.time < proximoAtaque)
        {
            return;
        }

        AtualizarDirecaoPorEntrada();
        bool ataqueParaDireita = direita;
        float duracaoVisualAtaque = ObterDuracaoAtaqueGuitarra(indiceAtaqueVisual);
        proximoAtaque = Time.time + Mathf.Max(intervaloAtaque, duracaoVisualAtaque);
        atacando = true;
        estadoAtual = null;
        PlayEstado("Ataque", "Attack");
        SetTriggerSeExistir("Ataque");

        if (rotinaAtaque != null)
        {
            StopCoroutine(rotinaAtaque);
        }

        rotinaAtaque = StartCoroutine(ExecutarAtaque(ataqueParaDireita, indiceAtaqueVisual));
    }

    private int ObterIndiceAtaqueSolicitado(out bool ataqueSolicitado)
    {
        ataqueSolicitado = true;

        if (GameServices.Instance.Settings.GetButtonDown(GameAction.AttackLine1))
        {
            return 0;
        }

        if (GameServices.Instance.Settings.GetButtonDown(GameAction.AttackLine2))
        {
            return 1;
        }

        if (GameServices.Instance.Settings.GetButtonDown(GameAction.AttackLine3))
        {
            return 2;
        }

        if (GameServices.Instance.Settings.GetButtonDown(GameAction.AttackLine4))
        {
            return 3;
        }

        ataqueSolicitado = false;
        return -1;
    }

    private IEnumerator ExecutarAtaque(bool ataqueParaDireita, int indiceAtaqueVisual)
    {
        Sprite[] quadrosAtaque = ObterQuadrosAtaqueGuitarra(indiceAtaqueVisual);
        bool usarSpritesAtaque = spriteRenderer != null && quadrosAtaque != null && quadrosAtaque.Length > 0;
        float duracaoQuadro = 1f / Mathf.Max(1f, quadrosPorSegundoAtaqueGuitarra);
        float duracaoVisual = usarSpritesAtaque ? quadrosAtaque.Length * duracaoQuadro : duracaoAnimacaoAtaque;
        float atraso = Mathf.Clamp(atrasoHitAtaque, 0f, Mathf.Max(intervaloAtaque, duracaoVisual));
        float fimHit = atraso + Mathf.Max(0.01f, duracaoHitAtaque);
        float tempoTotal = Mathf.Max(duracaoAnimacaoAtaque, duracaoVisual, fimHit);
        var alvosAtingidos = new HashSet<int>();
        float tempoDecorrido = 0f;
        int quadroAtual = -1;

        if (usarSpritesAtaque && animator != null)
        {
            animatorHabilitadoAntesDoAtaque = animator.enabled;
            animator.enabled = false;
            animatorControladoPorAtaque = true;
        }

        try
        {
            while (tempoDecorrido <= tempoTotal)
            {
                if (usarSpritesAtaque)
                {
                    int proximoQuadro = Mathf.Clamp(Mathf.FloorToInt(tempoDecorrido / duracaoQuadro), 0, quadrosAtaque.Length - 1);
                    if (proximoQuadro != quadroAtual)
                    {
                        spriteRenderer.sprite = quadrosAtaque[proximoQuadro];
                        quadroAtual = proximoQuadro;
                    }
                }

                if (tempoDecorrido >= atraso && tempoDecorrido <= fimHit)
                {
                    AplicarDanoAtaque(ataqueParaDireita, alvosAtingidos);
                }

                tempoDecorrido += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            FinalizarAtaqueVisual(true);
        }
    }

    private void CarregarQuadrosAtaqueGuitarra()
    {
        quadrosAtaqueGuitarra = null;
        string versaoSelecionada = PlayerAttackSpriteVersions.DefaultVersionId;
        string recursoSelecionado = PlayerAttackSpriteVersions.DefaultResourcePath;
        if (GameServices.HasInstance && GameServices.Instance.Settings != null)
        {
            versaoSelecionada = GameServices.Instance.Settings.SelectedAttackSpriteVersionId;
            recursoSelecionado = GameServices.Instance.Settings.GetSelectedAttackSpriteResourcePath();
        }

        if (!string.Equals(recursoSpritesAtaqueGuitarra, recursoSelecionado, StringComparison.Ordinal))
        {
            Debug.Log(
                $"MovimentoJogador: usando sprites de ataque {versaoSelecionada} em Resources/{recursoSelecionado}.",
                this);
            recursoSpritesAtaqueGuitarra = recursoSelecionado;
        }

        if (string.IsNullOrWhiteSpace(recursoSpritesAtaqueGuitarra))
        {
            return;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(recursoSpritesAtaqueGuitarra);
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"MovimentoJogador: nenhum sprite de ataque encontrado em Resources/{recursoSpritesAtaqueGuitarra}. Tentando versao padrao.", this);
            recursoSpritesAtaqueGuitarra = RecursoSpritesAtaqueGuitarraPadrao;
            sprites = Resources.LoadAll<Sprite>(recursoSpritesAtaqueGuitarra);
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogWarning($"MovimentoJogador: nenhum sprite de ataque encontrado em Resources/{recursoSpritesAtaqueGuitarra}.", this);
                return;
            }
        }

        Array.Sort(sprites, CompararSpritesAtaqueGuitarra);
        Debug.Log(
            $"MovimentoJogador: carregou {sprites.Length} sprites de ataque de Resources/{recursoSpritesAtaqueGuitarra}. Primeiro='{sprites[0].name}', Ultimo='{sprites[sprites.Length - 1].name}'.",
            this);

        int linhas = Mathf.Max(1, linhasAtaqueGuitarra);
        quadrosAtaqueGuitarra = new Sprite[linhas][];
        int cursor = 0;

        for (int linha = 0; linha < linhas; linha++)
        {
            int restantes = sprites.Length - cursor;
            if (restantes <= 0)
            {
                quadrosAtaqueGuitarra[linha] = new Sprite[0];
                continue;
            }

            int quantidade = ObterQuantidadeQuadrosLinhaAtaque(linha, restantes, linhas - linha);
            quadrosAtaqueGuitarra[linha] = new Sprite[quantidade];
            Array.Copy(sprites, cursor, quadrosAtaqueGuitarra[linha], 0, quantidade);
            cursor += quantidade;
        }
    }

    private int CompararSpritesAtaqueGuitarra(Sprite a, Sprite b)
    {
        string nomeA = a != null ? a.name : string.Empty;
        string nomeB = b != null ? b.name : string.Empty;
        return string.CompareOrdinal(nomeA, nomeB);
    }

    private int ObterQuantidadeQuadrosLinhaAtaque(int linha, int spritesRestantes, int linhasRestantes)
    {
        if (quadrosPorLinhaAtaqueGuitarra != null
            && linha < quadrosPorLinhaAtaqueGuitarra.Length
            && quadrosPorLinhaAtaqueGuitarra[linha] > 0)
        {
            return Mathf.Min(quadrosPorLinhaAtaqueGuitarra[linha], spritesRestantes);
        }

        return Mathf.Max(1, Mathf.CeilToInt(spritesRestantes / Mathf.Max(1f, linhasRestantes)));
    }

    private int ObterProximoAtaqueGuitarra()
    {
        if (quadrosAtaqueGuitarra == null || quadrosAtaqueGuitarra.Length == 0)
        {
            return -1;
        }

        for (int tentativa = 0; tentativa < quadrosAtaqueGuitarra.Length; tentativa++)
        {
            int indice = proximoAtaqueGuitarra % quadrosAtaqueGuitarra.Length;
            proximoAtaqueGuitarra = (proximoAtaqueGuitarra + 1) % quadrosAtaqueGuitarra.Length;

            if (quadrosAtaqueGuitarra[indice] != null && quadrosAtaqueGuitarra[indice].Length > 0)
            {
                return indice;
            }
        }

        return -1;
    }

    private Sprite[] ObterQuadrosAtaqueGuitarra(int indiceAtaqueVisual)
    {
        if (quadrosAtaqueGuitarra == null
            || indiceAtaqueVisual < 0
            || indiceAtaqueVisual >= quadrosAtaqueGuitarra.Length)
        {
            return null;
        }

        return quadrosAtaqueGuitarra[indiceAtaqueVisual];
    }

    private float ObterDuracaoAtaqueGuitarra(int indiceAtaqueVisual)
    {
        Sprite[] quadros = ObterQuadrosAtaqueGuitarra(indiceAtaqueVisual);
        if (quadros == null || quadros.Length == 0)
        {
            return duracaoAnimacaoAtaque;
        }

        return quadros.Length / Mathf.Max(1f, quadrosPorSegundoAtaqueGuitarra);
    }

    private void RestaurarAnimatorAposAtaque()
    {
        if (animatorControladoPorAtaque && animator != null)
        {
            animator.enabled = animatorHabilitadoAntesDoAtaque;
        }

        animatorControladoPorAtaque = false;
        animatorHabilitadoAntesDoAtaque = false;
    }

    private void FinalizarAtaqueVisual(bool atualizarAnimacao)
    {
        RestaurarAnimatorAposAtaque();
        atacando = false;
        rotinaAtaque = null;
        estadoAtual = null;

        if (!atualizarAnimacao || !isActiveAndEnabled)
        {
            return;
        }

        AtualizaAnimacao();
        if (animator != null && animator.enabled)
        {
            animator.Update(0f);
        }
    }

    private IEnumerator ExecutarDanoVisual()
    {
        if (atacando)
        {
            if (rotinaAtaque != null)
            {
                StopCoroutine(rotinaAtaque);
                rotinaAtaque = null;
            }

            FinalizarAtaqueVisual(false);
        }

        recebendoDano = true;
        estadoAtual = null;
        GarantirAnimatorDisponivel();
        PlayEstado("Dano", "Hit", "Damage");
        SetTriggerSeExistir("Dano");
        SetTriggerSeExistir("Hit");
        SetTriggerSeExistir("Damage");

        yield return new WaitForSeconds(Mathf.Max(0.01f, duracaoAnimacaoDano));

        recebendoDano = false;
        rotinaDano = null;
        estadoAtual = null;
        AtualizaAnimacao();
    }

    private void AplicarDanoAtaque(bool ataqueParaDireita, HashSet<int> alvosAtingidos)
    {
        Vector2 centro = (Vector2)transform.position + new Vector2((ataqueParaDireita ? 1f : -1f) * deslocamentoAtaque.x, deslocamentoAtaque.y);
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
                if (!alvosAtingidos.Add(inimigo.GetInstanceID()))
                {
                    continue;
                }

                inimigo.TakeDamage(danoAtaque, transform.position);
                continue;
            }

            Saude saudeLegada = alvo.GetComponentInParent<Saude>();
            if (saudeLegada != null && !alvo.CompareTag("Player"))
            {
                if (!alvosAtingidos.Add(saudeLegada.GetInstanceID()))
                {
                    continue;
                }

                saudeLegada.dano(danoAtaque);
            }
        }
    }

    private void AtualizaAnimacao()
    {
        bool animacaoBloqueada = atacando || recebendoDano || morto;
        if (!animacaoBloqueada)
        {
            GarantirAnimatorDisponivel();
        }

        bool andando = Mathf.Abs(moveX) > 0.05f;
        SetBoolSeExistir("NoChao", noChao);
        SetBoolSeExistir("noChao", noChao);
        SetBoolSeExistir("Grounded", noChao);
        SetBoolSeExistir("Correndo", andando);
        SetBoolSeExistir("correndo", andando);
        SetBoolSeExistir("Andando", andando);
        SetBoolSeExistir("Running", andando);

        if (animacaoBloqueada)
        {
            return;
        }

        if (!noChao)
        {
            bool caindo = corpoRigido != null && corpoRigido.velocity.y < -0.05f;
            if (caindo)
            {
                PlayEstadoMantendoVivo("Queda", "Fall");
            }
            else
            {
                PlayEstadoMantendoVivo("PuloAnim", "Pulo", "Jump");
            }
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

    private void GarantirAnimatorDisponivel()
    {
        if (animator == null)
        {
            return;
        }

        if (!animator.enabled)
        {
            animator.enabled = true;
            animatorControladoPorAtaque = false;
            animatorHabilitadoAntesDoAtaque = false;
            estadoAtual = null;
        }

        if (animator.speed <= 0f)
        {
            animator.speed = 1f;
            estadoAtual = null;
        }
    }

    private void VirarJogador()
    {
        AtualizarDirecaoPorEntrada();

        Vector2 escala = transform.localScale;
        if ((escala.x > 0f && !direita) || (escala.x < 0f && direita))
        {
            escala.x *= -1f;
            transform.localScale = escala;
        }
    }

    private void AtualizarDirecaoPorEntrada()
    {
        if (moveX > 0.05f)
        {
            direita = true;
        }
        else if (moveX < -0.05f)
        {
            direita = false;
        }
    }

    private void PlayEstado(params string[] nomes)
    {
        PlayEstadoInterno(false, nomes);
    }

    private void PlayEstadoMantendoVivo(params string[] nomes)
    {
        PlayEstadoInterno(true, nomes);
    }

    private void PlayEstadoInterno(bool reiniciarSeFinalizado, params string[] nomes)
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

            bool estadoAnimatorDiferente = !AnimatorEstaNoEstado(hash);
            bool estadoFinalizado = reiniciarSeFinalizado && AnimatorEstadoAtualFinalizado(hash);
            if (estadoAtual != nome || estadoAnimatorDiferente || estadoFinalizado)
            {
                animator.Play(hash, 0, 0f);
                estadoAtual = nome;
            }

            return;
        }
    }

    private bool AnimatorEstaNoEstado(int hash)
    {
        if (animator == null || !animator.enabled)
        {
            return false;
        }

        AnimatorStateInfo estado = animator.GetCurrentAnimatorStateInfo(0);
        return !animator.IsInTransition(0) && estado.shortNameHash == hash;
    }

    private bool AnimatorEstadoAtualFinalizado(int hash)
    {
        if (animator == null || !animator.enabled)
        {
            return false;
        }

        AnimatorStateInfo estado = animator.GetCurrentAnimatorStateInfo(0);
        return estado.shortNameHash == hash && !estado.loop && estado.normalizedTime >= 0.98f;
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

    private static bool NomeClipMorte(string nome)
    {
        return !string.IsNullOrEmpty(nome)
            && (nome.IndexOf("Morte", StringComparison.OrdinalIgnoreCase) >= 0
                || nome.IndexOf("Death", StringComparison.OrdinalIgnoreCase) >= 0
                || nome.IndexOf("Derrotado", StringComparison.OrdinalIgnoreCase) >= 0);
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

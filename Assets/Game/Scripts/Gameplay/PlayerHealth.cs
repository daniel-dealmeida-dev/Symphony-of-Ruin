using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = GameplayBalance.PlayerMaxHealth;
    [SerializeField] private float invulnerabilityDuration = GameplayBalance.PlayerInvulnerabilityAfterHitSeconds;
    [SerializeField] private float knockbackForce = GameplayBalance.PlayerKnockbackForce;
    [SerializeField] private float gameOverDelay = 0.65f;
    [SerializeField] private float extraDelayAfterDeathAnimation = 0.15f;
    [SerializeField] private SpriteRenderer targetRenderer;

    private int currentHealth;
    private bool isInvulnerable;
    private bool isAlive = true;
    private MovimentoJogador movimento;
    private float nextDamageTime;

    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    public bool IsAlive
    {
        get { return isAlive; }
    }

    public int MaxHealth
    {
        get { return maxHealth; }
    }

    private void Awake()
    {
        GameServices.EnsureInstance();
        movimento = GetComponent<MovimentoJogador>();
        maxHealth = Mathf.Max(1, GameplayBalance.PlayerMaxHealth);
        invulnerabilityDuration = GameplayBalance.PlayerInvulnerabilityAfterHitSeconds;
        knockbackForce = GameplayBalance.PlayerKnockbackForce;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        // A fase sempre comeca com vida cheia; saves antigos guardavam 3 vidas e deixavam o jogador fragil ao iniciar.
        currentHealth = Mathf.Clamp(GameplayBalance.PlayerInitialHealth, 1, maxHealth);
        nextDamageTime = Time.unscaledTime + GameplayBalance.PlayerSpawnDamageGraceSeconds;
        GameServices.Instance.Settings.SetLives(currentHealth);
    }

    public void ReceiveDamage(int damage)
    {
        ReceiveDamage(damage, transform.position);
    }

    public void ReceiveDamage(int damage, Vector2 damageSource)
    {
        if (isInvulnerable || damage <= 0 || Time.unscaledTime < nextDamageTime)
        {
            return;
        }

        if (!isAlive || (GameManager.gm != null && GameManager.gm.gameIsOver))
        {
            return;
        }

        isInvulnerable = true;
        nextDamageTime = Time.unscaledTime + invulnerabilityDuration;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        GameServices.Instance.Settings.SetLives(currentHealth);

        if (GameManager.gm != null)
        {
            GameManager.gm.SyncLives(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (movimento != null)
        {
            movimento.AplicarEmpurrao(damageSource, knockbackForce);
            movimento.TocarDano();
        }

        StartCoroutine(InvulnerabilityRoutine());
    }

    public void HealToFull()
    {
        currentHealth = maxHealth;
        isAlive = true;
        isInvulnerable = false;
        nextDamageTime = Time.unscaledTime + GameplayBalance.PlayerSpawnDamageGraceSeconds;
        GameServices.Instance.Settings.SetLives(currentHealth);
        if (GameManager.gm != null)
        {
            GameManager.gm.SyncLives(currentHealth);
        }
    }

    private void Die()
    {
        if (!isAlive)
        {
            return;
        }

        isAlive = false;
        isInvulnerable = true;

        if (movimento != null)
        {
            movimento.SetControlesAtivos(false);
            movimento.TocarMorte();
        }

        StartCoroutine(FinalizarJogoDepoisDaAnimacao());
    }

    private IEnumerator FinalizarJogoDepoisDaAnimacao()
    {
        float duracaoMorte = movimento != null
            ? movimento.ObterDuracaoAnimacaoMorte(gameOverDelay)
            : gameOverDelay;
        float espera = Mathf.Max(gameOverDelay, duracaoMorte + Mathf.Max(0f, extraDelayAfterDeathAnimation));

        yield return new WaitForSecondsRealtime(espera);
        if (GameManager.gm != null)
        {
            GameManager.gm.FinalizarJogo();
        }
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        while (Time.unscaledTime < nextDamageTime && isAlive)
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = !targetRenderer.enabled;
            }

            yield return new WaitForSecondsRealtime(0.12f);
        }

        if (targetRenderer != null)
        {
            targetRenderer.enabled = true;
        }

        isInvulnerable = false;
    }
}

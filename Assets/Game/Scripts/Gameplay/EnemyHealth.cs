using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private int touchDamage = GameplayBalance.DefaultEnemyDamage;
    [SerializeField] private float deathDestroyDelay = 0.9f;

    private int currentHealth;
    private bool isDead;
    private EnemyPresentation2D presentation;
    private float nextFallbackContactDamageTime;

    public int TouchDamage
    {
        get { return touchDamage; }
    }

    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    public bool IsDead
    {
        get { return isDead; }
    }

    public int MaxHealth
    {
        get { return maxHealth; }
    }

    private void Awake()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        presentation = GetComponent<EnemyPresentation2D>();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(int damage, Vector2 damageSource)
    {
        if (isDead || damage <= 0)
        {
            return;
        }

        if (presentation == null)
        {
            presentation = GetComponent<EnemyPresentation2D>();
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            float direction = transform.position.x >= damageSource.x ? 1f : -1f;
            body.velocity = new Vector2(direction * 2.5f, Mathf.Max(body.velocity.y, 1.5f));
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var projectile = other.GetComponent<Projectile2D>();
        if (projectile != null)
        {
            TakeDamage(projectile.Damage, other.transform.position);
            Destroy(other.gameObject);
            return;
        }

        var player = other.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            TryDamagePlayerThroughCombat(player);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var projectile = collision.gameObject.GetComponent<Projectile2D>();
        if (projectile != null)
        {
            TakeDamage(projectile.Damage, collision.gameObject.transform.position);
            Destroy(collision.gameObject);
            return;
        }

        var player = collision.gameObject.GetComponentInParent<PlayerHealth>();
        if (player != null)
        {
            TryDamagePlayerThroughCombat(player);
        }
    }

    private void TryDamagePlayerThroughCombat(PlayerHealth player)
    {
        EnemyPatrol2D patrol = GetComponent<EnemyPatrol2D>();
        if (patrol != null)
        {
            patrol.TryApplyContactDamage(player);
            return;
        }

        // Fallback para inimigos legados sem IA: ainda ha cooldown para evitar dano por multiplos frames.
        if (Time.time < nextFallbackContactDamageTime)
        {
            return;
        }

        nextFallbackContactDamageTime = Time.time + GameplayBalance.EnemyContactDamageCooldownSeconds;
        player.ReceiveDamage(Mathf.Clamp(touchDamage, 1, GameplayBalance.DefaultEnemyDamage), transform.position);
    }

    private void Die()
    {
        isDead = true;
        ScoreManager.EnsureInstance().RegisterEnemyDefeated(gameObject);

        EnemyPatrol2D patrol = GetComponent<EnemyPatrol2D>();
        if (patrol != null)
        {
            patrol.enabled = false;
        }

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.velocity = Vector2.zero;
            body.simulated = false;
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D item in colliders)
        {
            item.enabled = false;
        }

        if (presentation == null)
        {
            presentation = GetComponent<EnemyPresentation2D>();
        }

        if (presentation != null)
        {
            presentation.PlayDeath();
        }

        Destroy(gameObject, deathDestroyDelay);
    }
}

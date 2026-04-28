using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private int touchDamage = 1;

    private int currentHealth;
    private EnemyPresentation2D presentation;

    public int TouchDamage
    {
        get { return touchDamage; }
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        presentation = GetComponent<EnemyPresentation2D>();
    }

    public void TakeDamage(int damage)
    {
        if (presentation == null)
        {
            presentation = GetComponent<EnemyPresentation2D>();
        }

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var projectile = other.GetComponent<Projectile2D>();
        if (projectile != null)
        {
            TakeDamage(projectile.Damage);
            Destroy(other.gameObject);
            return;
        }

        var player = other.GetComponent<PlayerHealth>();
        if (player != null)
        {
            if (presentation == null)
            {
                presentation = GetComponent<EnemyPresentation2D>();
            }

            if (presentation != null)
            {
                presentation.PlayAttack();
            }

            player.ReceiveDamage(touchDamage);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var projectile = collision.gameObject.GetComponent<Projectile2D>();
        if (projectile != null)
        {
            TakeDamage(projectile.Damage);
            Destroy(collision.gameObject);
            return;
        }

        var player = collision.gameObject.GetComponent<PlayerHealth>();
        if (player != null)
        {
            if (presentation == null)
            {
                presentation = GetComponent<EnemyPresentation2D>();
            }

            if (presentation != null)
            {
                presentation.PlayAttack();
            }

            player.ReceiveDamage(touchDamage);
        }
    }
}

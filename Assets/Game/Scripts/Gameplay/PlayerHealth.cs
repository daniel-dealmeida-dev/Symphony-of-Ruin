using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invulnerabilityDuration = 1.1f;
    [SerializeField] private SpriteRenderer targetRenderer;

    private int currentHealth;
    private bool isInvulnerable;

    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    private void Awake()
    {
        GameServices.EnsureInstance();

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        currentHealth = Mathf.Clamp(GameServices.Instance.Settings.Data.progress.lives, 1, maxHealth);
        GameServices.Instance.Settings.SetLives(currentHealth);
    }

    public void ReceiveDamage(int damage)
    {
        if (isInvulnerable || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        GameServices.Instance.Settings.SetLives(currentHealth);

        if (GameManager.gm != null)
        {
            GameManager.gm.SyncLives(currentHealth);
        }

        if (currentHealth <= 0)
        {
            if (GameManager.gm != null)
            {
                GameManager.gm.FinalizarJogo();
            }

            return;
        }

        StartCoroutine(InvulnerabilityRoutine());
    }

    public void HealToFull()
    {
        currentHealth = maxHealth;
        GameServices.Instance.Settings.SetLives(currentHealth);
        if (GameManager.gm != null)
        {
            GameManager.gm.SyncLives(currentHealth);
        }
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float elapsed = 0f;
        while (elapsed < invulnerabilityDuration)
        {
            elapsed += Time.unscaledDeltaTime;
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

using UnityEngine;

public class Tiro : MonoBehaviour
{
    [SerializeField] private GameObject projetil;
    [SerializeField] private float forca = 12f;
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private float projectileSpawnOffset = 0.8f;
    [SerializeField] private int projectileDamage = 1;

    private void Awake()
    {
        GameServices.EnsureInstance();
    }

    private void Update()
    {
        bool firePressed = GameServices.Instance.Settings.GetButtonDown(GameAction.Fire) ||
                           GameServices.Instance.Settings.GetButtonDown(GameAction.Jump);

        if (!firePressed || projetil == null)
        {
            if (!firePressed)
            {
                return;
            }
        }

        Vector2 direction = transform.localScale.x >= 0f ? Vector2.right : Vector2.left;
        Vector3 spawnPosition = transform.position + new Vector3(direction.x * projectileSpawnOffset, 0.25f, 0f);
        GameObject newProjectile = projetil != null
            ? Instantiate(projetil, spawnPosition, Quaternion.identity)
            : CreateFallbackProjectile(spawnPosition);

        var projectile2D = newProjectile.GetComponent<Projectile2D>();
        if (projectile2D == null)
        {
            projectile2D = newProjectile.AddComponent<Projectile2D>();
        }

        projectile2D.Launch(direction, forca, projectileDamage);

        if (!newProjectile.TryGetComponent(out CircleCollider2D circleCollider))
        {
            circleCollider = newProjectile.AddComponent<CircleCollider2D>();
        }

        circleCollider.isTrigger = true;

        if (!newProjectile.TryGetComponent(out Rigidbody2D projectileRigidbody))
        {
            projectileRigidbody = newProjectile.AddComponent<Rigidbody2D>();
        }

        projectileRigidbody.gravityScale = 0f;
        projectileRigidbody.isKinematic = true;

        if (shootSFX == null)
        {
            return;
        }

        if (!newProjectile.TryGetComponent(out AudioSource source))
        {
            source = newProjectile.AddComponent<AudioSource>();
        }

        var managed = newProjectile.GetComponent<ManagedAudioSource>();
        if (managed == null)
        {
            managed = newProjectile.AddComponent<ManagedAudioSource>();
        }

        managed.Bus = AudioBus.Sfx;
        source.PlayOneShot(shootSFX);
    }

    private static GameObject CreateFallbackProjectile(Vector3 spawnPosition)
    {
        var projectileObject = new GameObject("Projetil");
        projectileObject.transform.position = spawnPosition;
        var spriteRenderer = projectileObject.AddComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(1f, 0.83f, 0.4f, 1f);
        projectileObject.tag = "Projetil";
        projectileObject.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
        return projectileObject;
    }
}

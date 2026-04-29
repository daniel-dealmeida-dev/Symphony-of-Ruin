using UnityEngine;

public class Tiro : MonoBehaviour
{
    [SerializeField] private GameObject projetil;
    [SerializeField] private float forca = 12f;
    [SerializeField] private AudioClip shootSFX;
    [SerializeField] private float projectileSpawnOffset = 0.8f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float fireCooldown = 0.28f;

    private float nextFireTime;

    private void Awake()
    {
        GameServices.EnsureInstance();
    }

    private void Update()
    {
        if (GameManager.gm != null && (GameManager.gm.jogoPausado || GameManager.gm.gameIsOver))
        {
            return;
        }

        bool firePressed = GameServices.Instance.Settings.GetButtonDown(GameAction.RangedFire);

        if (!firePressed || Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldown;
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
        projectileRigidbody.bodyType = RigidbodyType2D.Kinematic;

        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            Physics2D.IgnoreCollision(circleCollider, playerCollider);
        }

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
        Texture2D texture = Texture2D.whiteTexture;
        spriteRenderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
        spriteRenderer.color = new Color(1f, 0.83f, 0.4f, 1f);
        projectileObject.tag = "Projetil";
        projectileObject.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
        return projectileObject;
    }
}

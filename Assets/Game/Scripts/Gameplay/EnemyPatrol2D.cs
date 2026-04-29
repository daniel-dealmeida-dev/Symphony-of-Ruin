using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol2D : MonoBehaviour
{
    [SerializeField] private float patrolDistance = 4f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.25f;
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float attackRange = 1.15f;
    [SerializeField] private float attackCooldown = GameplayBalance.DefaultEnemyAttackCooldownSeconds;
    [SerializeField] private int attackDamage = GameplayBalance.DefaultEnemyDamage;
    [SerializeField] private bool flyingEnemy = false;
    [SerializeField] private float groundProbeDistance = 1.15f;
    [SerializeField] private float wallProbeDistance = 0.22f;

    private Vector3 startPosition;
    private int direction = 1;
    private Transform player;
    private PlayerHealth playerHealth;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private EnemyPresentation2D presentation;
    private float nextAttackTime;
    private float pauseMovementUntil;
    private Bounds playableBounds;
    private bool hasPlayableBounds;
    private LayerMask groundMask = ~0;

    public bool FlyingEnemy
    {
        get { return flyingEnemy; }
        set
        {
            flyingEnemy = value;
            ApplyBodySettings();
            ConfigureCollider();
        }
    }

    public void ApplyBalanceForEnemyType(bool isWolf, bool isCrow)
    {
        attackDamage = isWolf ? GameplayBalance.WolfDamage : GameplayBalance.DefaultEnemyDamage;
        attackCooldown = isWolf ? GameplayBalance.WolfAttackCooldownSeconds : GameplayBalance.DefaultEnemyAttackCooldownSeconds;
        flyingEnemy = isCrow;
        ApplyBodySettings();
        ConfigureCollider();
    }

    public void TryApplyContactDamage(PlayerHealth target)
    {
        if (target == null)
        {
            return;
        }

        playerHealth = target;
        player = target.transform;
        TryAttackPlayer();
    }

    private void Awake()
    {
        startPosition = transform.position;
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        presentation = GetComponent<EnemyPresentation2D>();
        int groundLayer = LayerMask.NameToLayer("chao");
        if (groundLayer >= 0)
        {
            groundMask = 1 << groundLayer;
        }

        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        ApplyBodySettings();

        if (GetComponent<Collider2D>() == null)
        {
            var collider = gameObject.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = false;
        }

        ConfigureCollider();
    }

    private void Start()
    {
        startPosition = transform.position;
        RefreshPlayableBounds();
        if (presentation == null)
        {
            presentation = GetComponent<EnemyPresentation2D>();
        }

        ResolvePlayer();
    }

    private void Update()
    {
        if (body == null)
        {
            return;
        }

        if (GameManager.gm != null && (GameManager.gm.jogoPausado || GameManager.gm.gameIsOver))
        {
            StopMoving();
            return;
        }

        if (player == null || playerHealth == null || !playerHealth.IsAlive)
        {
            ResolvePlayer();
        }

        Vector2 velocity = body.velocity;
        if (Time.time < pauseMovementUntil)
        {
            velocity.x = 0f;
            if (flyingEnemy)
            {
                velocity.y = 0f;
            }

            body.velocity = velocity;
            SetMoving(false);
            return;
        }

        bool chasing = false;
        if (player != null && playerHealth != null && playerHealth.IsAlive)
        {
            Vector2 toPlayer = player.position - transform.position;
            float distanceToPlayer = toPlayer.magnitude;
            chasing = distanceToPlayer <= detectionRange;

            if (chasing)
            {
                direction = toPlayer.x >= 0f ? 1 : -1;

                if (distanceToPlayer <= attackRange)
                {
                    TryAttackPlayer();
                    velocity.x = 0f;
                    if (flyingEnemy)
                    {
                        velocity.y = Mathf.Clamp(toPlayer.y, -0.75f, 0.75f);
                    }

                    body.velocity = velocity;
                    FaceDirection();
                    SetMoving(false);
                    return;
                }

                velocity.x = chaseSpeed * direction;
                if (flyingEnemy)
                {
                    velocity.y = Mathf.Clamp(toPlayer.y, -1.6f, 1.6f);
                }
            }
        }

        if (!chasing)
        {
            Patrol(ref velocity);
        }

        if (!flyingEnemy)
        {
            velocity.y = body.velocity.y;
        }

        KeepInsidePlayableArea(ref velocity);
        body.velocity = velocity;
        FaceDirection();
        SetMoving(Mathf.Abs(velocity.x) > 0.05f || (flyingEnemy && Mathf.Abs(velocity.y) > 0.05f));
    }

    private void ResolvePlayer()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
        }
    }

    private void Patrol(ref Vector2 velocity)
    {
        float offsetFromStart = transform.position.x - startPosition.x;
        if (offsetFromStart > patrolDistance)
        {
            direction = -1;
        }
        else if (offsetFromStart < -patrolDistance)
        {
            direction = 1;
        }

        if (ShouldTurnAround())
        {
            direction *= -1;
        }

        velocity.x = moveSpeed * direction;
        if (flyingEnemy)
        {
            velocity.y = Mathf.Clamp(startPosition.y - transform.position.y, -1f, 1f);
        }
    }

    private void TryAttackPlayer()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        pauseMovementUntil = Time.time + 0.25f;
        if (presentation != null)
        {
            presentation.PlayAttack();
        }

        if (playerHealth != null)
        {
            playerHealth.ReceiveDamage(attackDamage, transform.position);
        }
    }

    private void FaceDirection()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0;
        }
    }

    private void SetMoving(bool moving)
    {
        if (presentation != null)
        {
            presentation.SetMoving(moving);
        }
    }

    private void StopMoving()
    {
        if (body != null)
        {
            body.velocity = Vector2.zero;
        }

        SetMoving(false);
    }

    private void RefreshPlayableBounds()
    {
        playableBounds = EnemySceneBootstrap.CalculatePlayableBounds();
        hasPlayableBounds = playableBounds.size.x > 0.1f;
    }

    private bool ShouldTurnAround()
    {
        if (!hasPlayableBounds)
        {
            RefreshPlayableBounds();
        }

        if (hasPlayableBounds)
        {
            float margin = flyingEnemy ? 0.8f : 0.45f;
            if (transform.position.x <= playableBounds.min.x + margin && direction < 0)
            {
                return true;
            }

            if (transform.position.x >= playableBounds.max.x - margin && direction > 0)
            {
                return true;
            }
        }

        if (HasWallAhead())
        {
            return true;
        }

        return !flyingEnemy && !HasGroundAhead();
    }

    private void KeepInsidePlayableArea(ref Vector2 velocity)
    {
        if (!hasPlayableBounds)
        {
            RefreshPlayableBounds();
        }

        if (!hasPlayableBounds)
        {
            return;
        }

        float horizontalMargin = flyingEnemy ? 0.8f : 0.45f;
        if (transform.position.x <= playableBounds.min.x + horizontalMargin && velocity.x < 0f)
        {
            direction = 1;
            velocity.x = Mathf.Abs(velocity.x);
        }
        else if (transform.position.x >= playableBounds.max.x - horizontalMargin && velocity.x > 0f)
        {
            direction = -1;
            velocity.x = -Mathf.Abs(velocity.x);
        }

        if (!flyingEnemy)
        {
            return;
        }

        float minY = playableBounds.min.y + 1.2f;
        float maxY = playableBounds.max.y + 5f;
        if (transform.position.y <= minY && velocity.y < 0f)
        {
            velocity.y = Mathf.Abs(velocity.y);
        }
        else if (transform.position.y >= maxY && velocity.y > 0f)
        {
            velocity.y = -Mathf.Abs(velocity.y);
        }
    }

    private bool HasGroundAhead()
    {
        Collider2D currentCollider = GetComponent<Collider2D>();
        if (currentCollider == null)
        {
            return true;
        }

        Bounds bounds = currentCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.08f), bounds.min.y + 0.12f);
        return HasSolidHit(origin, Vector2.down, groundProbeDistance);
    }

    private bool HasWallAhead()
    {
        Collider2D currentCollider = GetComponent<Collider2D>();
        if (currentCollider == null)
        {
            return false;
        }

        Bounds bounds = currentCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.center.y);
        return HasSolidHit(origin, new Vector2(direction, 0f), bounds.extents.x + wallProbeDistance);
    }

    private bool HasSolidHit(Vector2 origin, Vector2 directionVector, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, directionVector, distance, groundMask);
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D hitCollider = hit.collider;
            if (hitCollider == null || hitCollider.isTrigger || hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void ApplyBodySettings()
    {
        if (body == null)
        {
            return;
        }

        body.gravityScale = flyingEnemy ? 0f : 1.6f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void ConfigureCollider()
    {
        Collider2D existingCollider = GetComponent<Collider2D>();
        if (existingCollider == null)
        {
            return;
        }

        existingCollider.isTrigger = false;
        Vector2 desiredWorldSize = flyingEnemy ? new Vector2(1.1f, 0.9f) : new Vector2(1.45f, 0.95f);
        Vector2 localSize = new Vector2(
            desiredWorldSize.x / Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x)),
            desiredWorldSize.y / Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.y)));

        BoxCollider2D box = existingCollider as BoxCollider2D;
        if (box != null)
        {
            box.size = localSize;
            box.offset = flyingEnemy ? Vector2.zero : new Vector2(0f, -localSize.y * 0.08f);
            return;
        }

        CapsuleCollider2D capsule = existingCollider as CapsuleCollider2D;
        if (capsule != null)
        {
            capsule.size = localSize;
            capsule.offset = flyingEnemy ? Vector2.zero : new Vector2(0f, -localSize.y * 0.08f);
        }
    }
}

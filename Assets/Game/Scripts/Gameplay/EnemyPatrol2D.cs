using UnityEngine;

[DisallowMultipleComponent]
public class EnemyPatrol2D : MonoBehaviour
{
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3.25f;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private bool flyingEnemy = false;

    private Vector3 startPosition;
    private int direction = 1;
    private Transform player;
    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private EnemyPresentation2D presentation;

    public bool FlyingEnemy
    {
        get { return flyingEnemy; }
        set { flyingEnemy = value; }
    }

    private void Awake()
    {
        startPosition = transform.position;
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        presentation = GetComponent<EnemyPresentation2D>();

        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody2D>();
        }

        body.gravityScale = flyingEnemy ? 0f : 1f;
        body.freezeRotation = true;

        if (GetComponent<Collider2D>() == null)
        {
            var collider = gameObject.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = false;
        }
    }

    private void Start()
    {
        if (presentation == null)
        {
            presentation = GetComponent<EnemyPresentation2D>();
        }

        var playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            player = playerHealth.transform;
        }
    }

    private void Update()
    {
        if (GameManager.gm != null && (GameManager.gm.jogoPausado || GameManager.gm.gameIsOver))
        {
            if (body != null)
            {
                body.velocity = Vector2.zero;
            }

            if (presentation != null)
            {
                presentation.SetMoving(false);
            }

            return;
        }

        Vector2 velocity = body != null ? body.velocity : Vector2.zero;
        float targetSpeed = moveSpeed * direction;

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRange)
            {
                direction = player.position.x >= transform.position.x ? 1 : -1;
                targetSpeed = chaseSpeed * direction;

                if (flyingEnemy)
                {
                    float verticalDelta = Mathf.Clamp(player.position.y - transform.position.y, -1.5f, 1.5f);
                    velocity.y = verticalDelta;
                }
            }
            else if (flyingEnemy)
            {
                float returnY = Mathf.Clamp(startPosition.y - transform.position.y, -1f, 1f);
                velocity.y = returnY;
            }
        }

        if (!flyingEnemy)
        {
            velocity.y = body.velocity.y;
        }

        velocity.x = targetSpeed;
        body.velocity = velocity;

        float offsetFromStart = transform.position.x - startPosition.x;
        if (offsetFromStart > patrolDistance)
        {
            direction = -1;
        }
        else if (offsetFromStart < -patrolDistance)
        {
            direction = 1;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0;
        }

        bool isMoving = Mathf.Abs(velocity.x) > 0.05f || (flyingEnemy && Mathf.Abs(velocity.y) > 0.05f);
        if (presentation != null)
        {
            presentation.SetMoving(isMoving);
        }
    }
}

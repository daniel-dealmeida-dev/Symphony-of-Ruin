using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 4.5f, -20f);

    [SerializeField] private float smoothTime = 0.5f;
    [SerializeField] private Vector2 deadZone = new Vector2(0.45f, 0.2f);

    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSmoothTime = 0.12f;

    [SerializeField] private float maxFollowSpeed = 32f;
    [SerializeField] private float snapDistance = 12f;

    [SerializeField] private float verticalPadding = 2.3f;
    [SerializeField] private float horizontalPadding = 3.5f;

    private Transform target;
    private Rigidbody2D targetBody;
    private Vector3 velocity;
    private Camera targetCamera;

    private float lookAhead;
    private float lookAheadVelocity;

    private bool hasSnappedToTarget;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        ResolveTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            ResolveTarget();

            if (target == null)
                return;
        }

        Vector3 desired = CalculateDesiredPosition();

        if (!hasSnappedToTarget ||
            Vector2.Distance(transform.position, desired) > snapDistance)
        {
            transform.position = desired;
            velocity = Vector3.zero;
            hasSnappedToTarget = true;
            return;
        }

        desired = ApplyDeadZone(desired);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref velocity,
            smoothTime,
            maxFollowSpeed
        );
    }

    private Vector3 CalculateDesiredPosition()
    {
        UpdateLookAhead();

        Vector3 desired =
            target.position +
            offset +
            new Vector3(lookAhead, 0f, 0f);

        return ClampToPlayableBounds(desired);
    }

    private void UpdateLookAhead()
    {
        float targetLookAhead = 0f;

        if (targetBody != null && Mathf.Abs(targetBody.velocity.x) > 0.1f)
        {
            targetLookAhead =
                Mathf.Sign(targetBody.velocity.x) * lookAheadDistance;
        }

        lookAhead = Mathf.SmoothDamp(
            lookAhead,
            targetLookAhead,
            ref lookAheadVelocity,
            lookAheadSmoothTime
        );
    }

    private Vector3 ApplyDeadZone(Vector3 desired)
    {
        Vector3 adjusted = desired;

        float deltaX = desired.x - transform.position.x;

        if (Mathf.Abs(deltaX) <= deadZone.x)
        {
            adjusted.x = transform.position.x;
        }
        else
        {
            adjusted.x = desired.x - Mathf.Sign(deltaX) * deadZone.x;
        }

        float deltaY = desired.y - transform.position.y;

        if (Mathf.Abs(deltaY) <= deadZone.y)
        {
            adjusted.y = transform.position.y;
        }
        else
        {
            adjusted.y = desired.y - Mathf.Sign(deltaY) * deadZone.y;
        }

        adjusted.z = offset.z;

        return ClampToPlayableBounds(adjusted);
    }

    // CORRIGIDO
    private Vector3 ClampToPlayableBounds(Vector3 desired)
    {
        Bounds bounds = EnemySceneBootstrap.CalculatePlayableBounds();

        if (bounds.size.x > 0.1f && targetCamera != null)
        {
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;

            float minX = bounds.min.x + halfWidth;
            float maxX = bounds.max.x - halfWidth;

            // permite câmera mais alta
            float minY = bounds.min.y + halfHeight;
            float maxY = bounds.max.y - halfHeight + 10f;

            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
        }

        desired.z = offset.z;

        return desired;
    }

    private void ResolveTarget()
    {
        PlayerHealth playerHealth =
            FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            SetTarget(playerHealth.transform);
            return;
        }

        MovimentoJogador movimento =
            FindFirstObjectByType<MovimentoJogador>();

        if (movimento != null)
        {
            SetTarget(movimento.transform);
        }
    }

    private void SetTarget(Transform newTarget)
    {
        if (target == newTarget)
            return;

        target = newTarget;

        targetBody =
            target != null
                ? target.GetComponent<Rigidbody2D>()
                : null;

        hasSnappedToTarget = false;

        lookAhead = 0f;
        lookAheadVelocity = 0f;
    }
}
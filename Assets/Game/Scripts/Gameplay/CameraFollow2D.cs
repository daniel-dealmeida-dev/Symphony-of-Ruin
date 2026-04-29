using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.1f, -25f);
    [SerializeField] private float smoothTime = 0.14f;
    [SerializeField] private float verticalPadding = 2.5f;
    [SerializeField] private float horizontalPadding = 4f;

    private Transform target;
    private Vector3 velocity;
    private Camera targetCamera;

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
            {
                return;
            }
        }

        Vector3 desired = target.position + offset;
        Bounds bounds = EnemySceneBootstrap.CalculatePlayableBounds();
        if (bounds.size.x > 0.1f && targetCamera != null)
        {
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;
            float minX = bounds.min.x + halfWidth - horizontalPadding;
            float maxX = bounds.max.x - halfWidth + horizontalPadding;
            float minY = bounds.min.y + halfHeight - verticalPadding;
            float maxY = bounds.max.y - halfHeight + verticalPadding;

            desired.x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : bounds.center.x;
            desired.y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : desired.y;
        }

        desired.z = offset.z;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    private void ResolveTarget()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            target = playerHealth.transform;
            return;
        }

        MovimentoJogador movimento = FindFirstObjectByType<MovimentoJogador>();
        if (movimento != null)
        {
            target = movimento.transform;
        }
    }
}

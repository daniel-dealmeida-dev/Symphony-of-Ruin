using UnityEngine;

/// <summary>
/// Desloca o fundo em relação à câmera para paralaxe simples (pixel art / camadas).
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform;
    [Range(0f, 1f)] public float parallaxFactor = 0.4f;
    private Vector3 lastCamPos;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            lastCamPos = cameraTransform.position;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 delta = cameraTransform.position - lastCamPos;
        transform.position += new Vector3(delta.x * parallaxFactor, delta.y * parallaxFactor * 0.35f, 0f);
        lastCamPos = cameraTransform.position;
    }
}

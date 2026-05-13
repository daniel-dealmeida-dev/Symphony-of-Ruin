using UnityEngine;

/// <summary>
/// Coleta de fragmentos menores da sinfonia (README): trigger 2D soma pontos no GameManager.
/// Acople a um objeto com Collider2D (Is Trigger) na camada de coleta.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ColetaFragmentoSinfonia : MonoBehaviour
{
    [Min(1)] public int valor = 1;

    private void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (GameManager.gm != null && !GameManager.gm.gameIsOver)
        {
            GameManager.gm.targetHit(valor, 0f);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollect();
        }

        Destroy(gameObject);
    }
}

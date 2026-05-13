using UnityEngine;

/// <summary>
/// Alvo destrutível por projétil (2D ou 3D). Atualiza pontuação no <see cref="GameManager"/>.
/// </summary>
public class ComportamentoAlvo : MonoBehaviour
{
    public int pontuacao = 0;
    public float tempoExtra = 0.0f;
    public GameObject prefabExplosao;

    private void TryHit(GameObject hitObject)
    {
        if (GameManager.gm != null && GameManager.gm.gameIsOver)
        {
            return;
        }

        if (hitObject == null || !hitObject.CompareTag("Projetil"))
        {
            return;
        }

        if (prefabExplosao)
        {
            Instantiate(prefabExplosao, transform.position, transform.rotation);
        }

        if (GameManager.gm != null)
        {
            GameManager.gm.targetHit(pontuacao, tempoExtra);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollect();
        }

        Destroy(hitObject);
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision newCollision)
    {
        TryHit(newCollision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHit(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other.gameObject);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZonaMortal : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            PlayerHealth playerHealth = other.gameObject.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ReceiveDamage(999, transform.position);
                return;
            }

            Saude saude = other.gameObject.GetComponentInParent<Saude>();
            if (saude != null)
            {
                saude.danoMax();
            }
        }
        else
        { // se for qualquer outra coisa, como um inimigo caindo por ex, destrua o objeto
            Object.Destroy(other.gameObject);
        }
    }
}

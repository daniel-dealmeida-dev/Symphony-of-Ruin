using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZonaMortal : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Saude saude = other.gameObject.GetComponent<Saude>();
            if (saude != null)
            {
                saude.danoMax();
                return;
            }

            Controle controle = other.gameObject.GetComponent<Controle>();
            if (controle != null)
            {
                controle.ForcarMorte();
            }

            return;
        }

        Object.Destroy(other.gameObject);
    }
}
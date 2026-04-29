using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DanoArma : MonoBehaviour {

    public int dano = 5;
    public string tagInimigo = "Inimigo";
    public GameObject explosao;

    private void OnTriggerEnter2D(Collider2D outro)
    {
        if (outro.gameObject.tag == tagInimigo || outro.GetComponentInParent<EnemyHealth>() != null)
        {
            EnemyHealth enemyHealth = outro.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(dano, transform.position);
            }
            else
            {
                Saude saude = outro.GetComponentInParent<Saude>();
                if (saude != null)
                {
                    saude.dano(dano);
                }
            }
        }
        if(explosao){
            Instantiate(explosao, transform.position, transform.rotation);
        }
	}

}

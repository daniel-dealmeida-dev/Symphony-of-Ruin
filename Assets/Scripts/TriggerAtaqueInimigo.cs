using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerAtaqueInimigo : MonoBehaviour {

	public int dano = GameplayBalance.WolfDamage;
	public string tagInimigo = "Player";
	public GameObject inimigo;
	public float intervaloDano = GameplayBalance.EnemyContactDamageCooldownSeconds;

	private float proximoDano;

	void Awake()
	{
		if (tagInimigo == "Player")
		{
			dano = GameplayBalance.WolfDamage;
			intervaloDano = Mathf.Max(intervaloDano, GameplayBalance.EnemyContactDamageCooldownSeconds);
		}
	}

	void OnTriggerEnter2D (Collider2D outro)
	{
		if (outro.gameObject.tag != tagInimigo)
		{
			return;
		}

		if (Time.time < proximoDano)
		{
			return;
		}

		proximoDano = Time.time + intervaloDano;

		PlayerHealth playerHealth = outro.GetComponentInParent<PlayerHealth>();
		if (playerHealth != null)
		{
			playerHealth.ReceiveDamage(dano, transform.position);
		}
		else
		{
			Saude saude = outro.GetComponentInParent<Saude>();
			if (saude != null && !saude.morto)
			{
				saude.dano(dano);
			}
		}

		if (inimigo != null && inimigo.GetComponent<IAInimigoRonda>() != null)
		{
			inimigo.GetComponent<IAInimigoRonda>().ataca();
		}
	}

}

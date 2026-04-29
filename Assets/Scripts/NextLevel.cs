using UnityEngine;
using System.Collections;

public class NextLevel : MonoBehaviour {

	// responde nas colisões
	void OnCollisionEnter(Collision newCollision)
	{
		// se atingido por um projétil...
		if (newCollision.gameObject.tag == "Projetil") {
			// Chame a função NextLevel no game manager
			GameManager.gm.NextLevel();
		}
	}

	void OnCollisionEnter2D(Collision2D newCollision)
	{
		if (newCollision.gameObject.CompareTag("Projetil") && GameManager.gm != null) {
			GameManager.gm.NextLevel();
		}
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Player") && GameManager.gm != null) {
			GameManager.gm.NextLevel();
		}
	}
}

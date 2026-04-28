using UnityEngine;

[DisallowMultipleComponent]
public class EnemySceneBootstrap : MonoBehaviour
{
    private void Awake()
    {
        SetupPlayer();
        SetupEnemies();
    }

    private static void SetupPlayer()
    {
        var playerRoot = GameObject.Find("Personagem");
        if (playerRoot == null)
        {
            return;
        }

        var playerVisual = playerRoot.GetComponentInChildren<MovimentoJogador>();
        if (playerVisual == null)
        {
            return;
        }

        if (playerVisual.GetComponent<PlayerHealth>() == null)
        {
            playerVisual.gameObject.AddComponent<PlayerHealth>();
        }
    }

    private static void SetupEnemies()
    {
        var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var item in allTransforms)
        {
            if (item == null)
            {
                continue;
            }

            string lowerName = item.name.ToLowerInvariant();
            bool isWolf = lowerName.Contains("wolf");
            bool isCrow = lowerName.Contains("crow");
            if (!isWolf && !isCrow)
            {
                continue;
            }

            if (item.GetComponent<EnemyPresentation2D>() == null)
            {
                item.gameObject.AddComponent<EnemyPresentation2D>();
            }

            if (item.GetComponent<EnemyHealth>() == null)
            {
                item.gameObject.AddComponent<EnemyHealth>();
            }

            var patrol = item.GetComponent<EnemyPatrol2D>();
            if (patrol == null)
            {
                patrol = item.gameObject.AddComponent<EnemyPatrol2D>();
            }

            if (isCrow)
            {
                patrol.FlyingEnemy = true;
            }

            item.gameObject.layer = 0;
        }
    }
}

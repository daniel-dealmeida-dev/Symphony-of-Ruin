using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public class EnemySceneBootstrap : MonoBehaviour
{
    private const string RuntimeEnemyRootName = "__MainMapEnemySpawns";
    private const string PlayerLayerName = "Player";
    private const string GroundLayerName = "chao";
    private const string BackgroundLayerName = "fundo";
    private const string EnemyLayerName = "Enemy";
    private const string ForegroundLayerName = "Foreground";
    private const string DecorationLayerName = "Decorations";
    private const string PlayerSortingLayer = "Protagonista";
    private const string EnemySortingLayer = "Inimigos";
    private const string GroundSortingLayer = "chao";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapLoadedScene()
    {
        if (!LooksLikeGameplayScene())
        {
            return;
        }

        if (FindFirstObjectByType<EnemySceneBootstrap>() == null)
        {
            new GameObject("EnemySceneBootstrap").AddComponent<EnemySceneBootstrap>();
        }
    }

    private void Awake()
    {
        ConfigureScene();
    }

    private void Start()
    {
        ConfigureScene();
    }

    public static Bounds CalculatePlayableBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || !IsGroundName(renderer.gameObject.name))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
    }

    private static void ConfigureScene()
    {
        SetupGroundColliders();
        OrganizeMapLayers();
        GameObject player = SetupPlayer();
        SetupCamera();
        SetupEnemies();
        PlacePlayerOnMainMap(player);
        PlaceEnemiesOnMainMap(player);
    }

    private static bool LooksLikeGameplayScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name == "TelaInicial" || activeScene.name == "Configuracoes" || activeScene.name == "TelaCarregamento")
        {
            return false;
        }

        return FindPlayerCandidate() != null || FindEnemyTemplate("wolf") != null || FindEnemyTemplate("crow") != null;
    }

    private static GameObject SetupPlayer()
    {
        GameObject player = FindPlayerCandidate();
        if (player == null)
        {
            return null;
        }

        SafeSetTag(player, "Player");
        SetLayerIfExists(player, PlayerLayerName, true);
        ApplyPlayerVisualScale(player);
        SetSortingForObject(player, PlayerSortingLayer, GameplayBalance.PlayerSortingOrder);

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = player.AddComponent<Rigidbody2D>();
        }

        body.freezeRotation = true;
        body.gravityScale = Mathf.Max(1f, body.gravityScale);
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        ConfigurePlayerCollider(player);

        if (player.GetComponent<MovimentoJogador>() == null)
        {
            player.AddComponent<MovimentoJogador>();
        }

        if (player.GetComponent<PlayerHealth>() == null)
        {
            player.AddComponent<PlayerHealth>();
        }

        if (player.GetComponent<Tiro>() == null)
        {
            player.AddComponent<Tiro>();
        }

        return player;
    }

    private static void ConfigurePlayerCollider(GameObject player)
    {
        Collider2D collider = player.GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = player.AddComponent<CapsuleCollider2D>();
        }

        collider.isTrigger = false;
        Vector2 desiredWorldSize = new Vector2(GameplayBalance.PlayerCollisionWidth, GameplayBalance.PlayerCollisionHeight);
        Vector2 localSize = WorldToLocalSize(player.transform, desiredWorldSize);

        CapsuleCollider2D capsule = collider as CapsuleCollider2D;
        if (capsule != null)
        {
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = localSize;
            capsule.offset = new Vector2(0f, -localSize.y * 0.03f);
            return;
        }

        BoxCollider2D box = collider as BoxCollider2D;
        if (box != null)
        {
            box.size = localSize;
            box.offset = new Vector2(0f, -localSize.y * 0.03f);
        }
    }

    private static void SetupGroundColliders()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || !IsGroundName(renderer.gameObject.name))
            {
                continue;
            }

            SetLayerIfExists(renderer.gameObject, GroundLayerName, false);
            ConfigureGroundCollider(renderer.gameObject, renderer);
        }
    }

    private static void ConfigureGroundCollider(GameObject ground, SpriteRenderer renderer)
    {
        BoxCollider2D collider = ground.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = ground.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = false;
        Vector2 localSize = renderer.size;
        if (localSize.x <= 0.01f || localSize.y <= 0.01f)
        {
            localSize = renderer.sprite != null ? (Vector2)renderer.sprite.bounds.size : new Vector2(1f, 1f);
        }

        float colliderHeight = Mathf.Clamp(localSize.y * 0.32f, 0.22f, localSize.y);
        collider.size = new Vector2(localSize.x, colliderHeight);
        collider.offset = new Vector2(0f, (localSize.y * 0.5f) - (colliderHeight * 0.5f));
    }

    private static void OrganizeMapLayers()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            string objectName = renderer.gameObject.name;
            if (IsGroundName(objectName))
            {
                SetLayerIfExists(renderer.gameObject, GroundLayerName, false);
                SetSorting(renderer, GroundSortingLayer, GameplayBalance.TerrainSortingOrder);
            }
            else if (IsBackgroundName(objectName))
            {
                SetLayerIfExists(renderer.gameObject, BackgroundLayerName, false);
                SetSorting(renderer, GuessBackgroundSortingLayer(objectName), GameplayBalance.BackgroundSortingOrder);
            }
            else if (IsForegroundName(objectName))
            {
                SetLayerIfExists(renderer.gameObject, GuessForegroundLayer(objectName), false);
                SetSorting(renderer, GuessForegroundSortingLayer(objectName), GameplayBalance.ForegroundSortingOrder);
            }
            else if (IsDecorationName(objectName))
            {
                SetLayerIfExists(renderer.gameObject, GuessDecorationLayer(objectName), false);
                SetSorting(renderer, GuessDecorationSortingLayer(objectName), GameplayBalance.DecorationSortingOrder);
            }
        }
    }

    private static void SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.orthographic = true;
        camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, 5.2f, 7.2f);

        if (camera.GetComponent<CameraFollow2D>() == null)
        {
            camera.gameObject.AddComponent<CameraFollow2D>();
        }
    }

    private static void SetupEnemies()
    {
        foreach (GameObject enemy in FindEnemyInstances(true))
        {
            ConfigureEnemy(enemy);
        }
    }

    private static void PlacePlayerOnMainMap(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        Bounds bounds = CalculatePlayableBounds();
        if (bounds.size.x <= 0.1f)
        {
            return;
        }

        Vector3 position = player.transform.position;
        bool outside = position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y - 3f || position.y > bounds.max.y + 8f;
        bool unsupported = !IsSupportedByMainGround(position, 4f);
        if (!outside && !unsupported)
        {
            return;
        }

        float desiredX = Mathf.Lerp(bounds.min.x, bounds.max.x, 0.08f);
        if (TryFindGroundPointNearX(desiredX, 0.6f, out float spawnX, out float groundTop))
        {
            player.transform.position = new Vector3(spawnX, groundTop + 1.15f, position.z);
        }
    }

    private static void PlaceEnemiesOnMainMap(GameObject player)
    {
        Bounds bounds = CalculatePlayableBounds();
        if (bounds.size.x <= 0.1f)
        {
            return;
        }

        List<GameObject> existingEnemies = FindEnemyInstances(false);
        float[] existingSlots = { 0.22f, 0.36f, 0.5f, 0.64f, 0.78f, 0.9f };
        for (int i = 0; i < existingEnemies.Count; i++)
        {
            GameObject enemy = existingEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            bool isCrow = enemy.name.ToLowerInvariant().Contains("crow");
            Vector3 current = enemy.transform.position;
            bool needsPlacement = current.x < bounds.min.x || current.x > bounds.max.x || current.y < bounds.min.y - 4f || !IsSupportedByMainGround(current, isCrow ? 7f : 3f) || IsTooCloseToPlayer(current, player, 3f);
            float desiredX = needsPlacement ? Mathf.Lerp(bounds.min.x, bounds.max.x, existingSlots[i % existingSlots.Length]) : current.x;
            PositionEnemyAtGround(enemy, desiredX, isCrow);
        }

        if (GameObject.Find(RuntimeEnemyRootName) != null)
        {
            return;
        }

        GameObject wolfTemplate = FindEnemyTemplate("wolf");
        GameObject crowTemplate = FindEnemyTemplate("crow");
        if (wolfTemplate == null && crowTemplate == null)
        {
            return;
        }

        GameObject root = new GameObject(RuntimeEnemyRootName);
        float[] wolfSlots = { 0.18f, 0.32f, 0.48f, 0.66f, 0.84f };
        for (int i = 0; i < wolfSlots.Length; i++)
        {
            if (wolfTemplate == null)
            {
                break;
            }

            float desiredX = AvoidPlayerSpawnX(Mathf.Lerp(bounds.min.x, bounds.max.x, wolfSlots[i]), bounds, player);
            GameObject clone = Instantiate(wolfTemplate, root.transform);
            clone.name = "Wolf_Patrulha_" + (i + 1);
            ConfigureEnemy(clone);
            PositionEnemyAtGround(clone, desiredX, false);
        }

        float[] crowSlots = { 0.26f, 0.58f, 0.76f };
        for (int i = 0; i < crowSlots.Length; i++)
        {
            if (crowTemplate == null)
            {
                break;
            }

            float desiredX = AvoidPlayerSpawnX(Mathf.Lerp(bounds.min.x, bounds.max.x, crowSlots[i]), bounds, player);
            GameObject clone = Instantiate(crowTemplate, root.transform);
            clone.name = "Crow_Ronda_" + (i + 1);
            ConfigureEnemy(clone);
            PositionEnemyAtGround(clone, desiredX, true);
        }
    }

    private static void ConfigureEnemy(GameObject enemy)
    {
        SafeSetTag(enemy, "Inimigos");
        SetLayerIfExists(enemy, EnemyLayerName, true);
        SetSortingForObject(enemy, EnemySortingLayer, GameplayBalance.EnemySortingOrder);

        string lowerEnemyName = enemy.name.ToLowerInvariant();
        bool isCrow = lowerEnemyName.Contains("crow");
        bool isWolf = lowerEnemyName.Contains("wolf");
        Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
        if (body == null)
        {
            body = enemy.AddComponent<Rigidbody2D>();
        }

        body.freezeRotation = true;

        if (enemy.GetComponent<Collider2D>() == null)
        {
            enemy.AddComponent<CapsuleCollider2D>();
        }

        if (enemy.GetComponent<EnemyPresentation2D>() == null)
        {
            enemy.AddComponent<EnemyPresentation2D>();
        }

        if (enemy.GetComponent<EnemyHealth>() == null)
        {
            enemy.AddComponent<EnemyHealth>();
        }

        EnemyPatrol2D patrol = enemy.GetComponent<EnemyPatrol2D>();
        if (patrol == null)
        {
            patrol = enemy.AddComponent<EnemyPatrol2D>();
        }

        patrol.ApplyBalanceForEnemyType(isWolf, isCrow);
    }

    private static void PositionEnemyAtGround(GameObject enemy, float desiredX, bool isCrow)
    {
        if (enemy == null || !TryFindGroundPointNearX(desiredX, 0.75f, out float groundX, out float groundTop))
        {
            return;
        }

        float yOffset = isCrow ? 3.2f : 0.72f;
        Vector3 current = enemy.transform.position;
        enemy.transform.position = new Vector3(groundX, groundTop + yOffset, current.z);
    }

    private static float AvoidPlayerSpawnX(float desiredX, Bounds bounds, GameObject player)
    {
        if (player == null || Mathf.Abs(desiredX - player.transform.position.x) >= 4f)
        {
            return desiredX;
        }

        float shifted = desiredX + 5.5f;
        if (shifted > bounds.max.x - 1f)
        {
            shifted = desiredX - 5.5f;
        }

        return Mathf.Clamp(shifted, bounds.min.x + 1f, bounds.max.x - 1f);
    }

    private static bool IsTooCloseToPlayer(Vector3 position, GameObject player, float minimumDistance)
    {
        return player != null && Vector2.Distance(position, player.transform.position) < minimumDistance;
    }

    private static bool IsSupportedByMainGround(Vector3 position, float maxDistance)
    {
        if (!TryFindGroundPointNearX(position.x, 0.4f, out float groundX, out float groundTop))
        {
            return false;
        }

        if (Mathf.Abs(groundX - position.x) > 1.25f)
        {
            return false;
        }

        return position.y >= groundTop - 0.25f && position.y <= groundTop + maxDistance;
    }

    private static bool TryFindGroundPointNearX(float desiredX, float edgePadding, out float groundX, out float groundTop)
    {
        groundX = desiredX;
        groundTop = 0f;
        bool found = false;
        float bestDistance = float.MaxValue;
        float bestTop = float.MinValue;
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || !IsGroundName(renderer.gameObject.name))
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            float minX = bounds.min.x + edgePadding;
            float maxX = bounds.max.x - edgePadding;
            if (minX > maxX)
            {
                minX = maxX = bounds.center.x;
            }

            float clampedX = Mathf.Clamp(desiredX, minX, maxX);
            float distance = Mathf.Abs(desiredX - clampedX);
            bool better = distance < bestDistance || (Mathf.Approximately(distance, bestDistance) && bounds.max.y > bestTop);
            if (!better)
            {
                continue;
            }

            bestDistance = distance;
            bestTop = bounds.max.y;
            groundX = clampedX;
            groundTop = bounds.max.y;
            found = true;
        }

        return found;
    }

    private static List<GameObject> FindEnemyInstances(bool includeRuntimeRoot)
    {
        var enemies = new List<GameObject>();
        var seen = new HashSet<GameObject>();
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Transform item in transforms)
        {
            if (item == null || !IsEnemyName(item.name))
            {
                continue;
            }

            if (!includeRuntimeRoot && IsUnderNamedRoot(item, RuntimeEnemyRootName))
            {
                continue;
            }

            GameObject enemy = item.gameObject;
            if (seen.Add(enemy))
            {
                enemies.Add(enemy);
            }
        }

        return enemies;
    }

    private static GameObject FindPlayerCandidate()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer;
        }

        MovimentoJogador movimento = FindFirstObjectByType<MovimentoJogador>();
        if (movimento != null)
        {
            return movimento.gameObject;
        }

        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health != null)
        {
            return health.gameObject;
        }

        GameObject protagonist = GameObject.Find("Protagonista");
        if (protagonist != null)
        {
            return protagonist;
        }

        GameObject root = GameObject.Find("Personagem");
        if (root == null)
        {
            return null;
        }

        Rigidbody2D body = root.GetComponentInChildren<Rigidbody2D>();
        if (body != null)
        {
            return body.gameObject;
        }

        SpriteRenderer renderer = root.GetComponentInChildren<SpriteRenderer>();
        return renderer != null ? renderer.gameObject : root;
    }

    private static GameObject FindEnemyTemplate(string lowerToken)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Transform item in transforms)
        {
            if (item == null || IsUnderNamedRoot(item, RuntimeEnemyRootName))
            {
                continue;
            }

            if (item.name.ToLowerInvariant().Contains(lowerToken))
            {
                return item.gameObject;
            }
        }

        return null;
    }

    private static bool IsUnderNamedRoot(Transform item, string rootName)
    {
        Transform current = item;
        while (current != null)
        {
            if (current.name == rootName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsEnemyName(string name)
    {
        string lowerName = name.ToLowerInvariant();
        return lowerName.Contains("wolf") || lowerName.Contains("crow");
    }

    private static bool IsGroundName(string name)
    {
        string lowerName = name.ToLowerInvariant();
        if (lowerName.Contains("mato") || lowerName.Contains("tree") || lowerName.Contains("arvore") || lowerName.Contains("fundo") || lowerName.Contains("layer") || lowerName.Contains("lua"))
        {
            return false;
        }

        return lowerName.Contains("bloco") ||
               lowerName.Contains("chao") ||
               lowerName.Contains("ground") ||
               lowerName.Contains("bridge");
    }

    private static bool IsBackgroundName(string name)
    {
        string lowerName = name.ToLowerInvariant();
        return lowerName.Contains("fundo") ||
               lowerName.Contains("background") ||
               lowerName.Contains("lua") ||
               lowerName.Contains("nuvem") ||
               lowerName.Contains("montanha") ||
               lowerName.Contains("layer");
    }

    private static bool IsForegroundName(string name)
    {
        string lowerName = name.ToLowerInvariant();
        return lowerName.Contains("frente") || lowerName.Contains("foreground");
    }

    private static bool IsDecorationName(string name)
    {
        string lowerName = name.ToLowerInvariant();
        return lowerName.Contains("mato") ||
               lowerName.Contains("grama") ||
               lowerName.Contains("tree") ||
               lowerName.Contains("arvore") ||
               lowerName.Contains("tocha") ||
               lowerName.Contains("fogo");
    }

    private static string GuessBackgroundSortingLayer(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();
        if (lowerName.Contains("lua"))
        {
            return "Lua";
        }

        if (lowerName.Contains("nuvem"))
        {
            return "Nuvens";
        }

        return "backgound";
    }

    private static string GuessForegroundSortingLayer(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();
        if (lowerName.Contains("grama") || lowerName.Contains("mato"))
        {
            return "Grama";
        }

        if (lowerName.Contains("tocha") || lowerName.Contains("fogo"))
        {
            return "FogoTocha";
        }

        return "ArvoreFrente";
    }

    private static string GuessForegroundLayer(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();
        if (lowerName.Contains("tocha") || lowerName.Contains("fogo"))
        {
            return DecorationLayerName;
        }

        return ForegroundLayerName;
    }

    private static string GuessDecorationSortingLayer(string objectName)
    {
        string lowerName = objectName.ToLowerInvariant();
        if (lowerName.Contains("grama") || lowerName.Contains("mato"))
        {
            return "Grama";
        }

        if (lowerName.Contains("fogo"))
        {
            return "FogoTocha";
        }

        if (lowerName.Contains("tocha"))
        {
            return "Tocha";
        }

        return "arvore";
    }

    private static string GuessDecorationLayer(string objectName)
    {
        return DecorationLayerName;
    }

    private static void ApplyPlayerVisualScale(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        Vector3 scale = player.transform.localScale;
        float facing = scale.x < 0f ? -1f : 1f;
        // Escala visual e hitbox ficam separadas: o collider e configurado em unidades de mundo logo abaixo.
        player.transform.localScale = new Vector3(GameplayBalance.PlayerVisualScale * facing, GameplayBalance.PlayerVisualScale, scale.z);
    }

    private static void SetSortingForObject(GameObject target, string sortingLayerName, int sortingOrder)
    {
        if (target == null)
        {
            return;
        }

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            SetSorting(renderer, sortingLayerName, sortingOrder);
        }
    }

    private static void SetSorting(SpriteRenderer renderer, string sortingLayerName, int sortingOrder)
    {
        if (renderer == null)
        {
            return;
        }

        if (SortingLayerExists(sortingLayerName))
        {
            renderer.sortingLayerName = sortingLayerName;
        }

        renderer.sortingOrder = sortingOrder;
    }

    private static bool SortingLayerExists(string sortingLayerName)
    {
        return sortingLayerName == "Default" || SortingLayer.NameToID(sortingLayerName) != 0;
    }

    private static void SetLayerIfExists(GameObject target, string layerName, bool includeChildren)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (target == null || layer < 0)
        {
            return;
        }

        target.layer = layer;
        if (!includeChildren)
        {
            return;
        }

        Transform[] children = target.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            child.gameObject.layer = layer;
        }
    }

    private static Vector2 WorldToLocalSize(Transform target, Vector2 worldSize)
    {
        Vector3 lossyScale = target.lossyScale;
        return new Vector2(
            worldSize.x / Mathf.Max(0.01f, Mathf.Abs(lossyScale.x)),
            worldSize.y / Mathf.Max(0.01f, Mathf.Abs(lossyScale.y)));
    }

    private static void SafeSetTag(GameObject target, string tagName)
    {
        try
        {
            target.tag = tagName;
        }
        catch (UnityException)
        {
            // Some project scenes do not define every custom tag; gameplay checks components first.
        }
    }
}

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
    private const float GroundedWolfSpawnOffset = 0.58f;
    private const float FlyingEnemySpawnOffset = 3.2f;
    private const bool EnableFlyingEnemies = true;
    private const string LegacyPlayerSpriteNameFragment = "personagem-Photoroom";
    private const string SafeIdleSpriteResourcePath = "SpritsProtagoniista/PlayerIdleConsistent_v3/sheets/player_idle_sheet_416x288";
    private static readonly string[] PlayerSpriteNameFragments =
    {
        LegacyPlayerSpriteNameFragment,
        "player_",
        "PlayerAttack",
        "PlayerBody",
        "PlayerIdle",
        "Pulo10",
        "PuloSprite",
        "personagem"
    };

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
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        StartCoroutine(LogVisibleRendererProbe());
#endif
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
        DisableMisassignedBackgroundAnimators();
        DisableNonSolidMapColliders();
        SetupGroundColliders();
        // OrganizeMapLayers();
        GameObject player = SetupPlayer();
        RemoveLegacyPlayerSpriteGhosts(player);
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

    private static void DisableNonSolidMapColliders()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || !IsNonSolidMapName(renderer.gameObject.name))
            {
                continue;
            }

            Collider2D[] colliders = renderer.GetComponents<Collider2D>();
            foreach (Collider2D collider in colliders)
            {
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                collider.enabled = false;
            }
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

    //private static void OrganizeMapLayers()
    //{
        //SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        //foreach (SpriteRenderer renderer in renderers)
        //{
            //if (renderer == null)
           // {
              //  continue;
            //}

            //string objectName = renderer.gameObject.name;
            //if (IsGroundName(objectName))
            //{
                //SetLayerIfExists(renderer.gameObject, GroundLayerName, false);
              //  SetSorting(renderer, GroundSortingLayer, GameplayBalance.TerrainSortingOrder);
            //}
            //else if (IsBackgroundName(objectName))
            //{
                //SetLayerIfExists(renderer.gameObject, BackgroundLayerName, false);
              //  SetSorting(renderer, GuessBackgroundSortingLayer(objectName), GameplayBalance.BackgroundSortingOrder);
            //}
            //else if (IsForegroundName(objectName))
            //{
                //SetLayerIfExists(renderer.gameObject, GuessForegroundLayer(objectName), false);
              //  SetSorting(renderer, GuessForegroundSortingLayer(objectName), GameplayBalance.ForegroundSortingOrder);
            //}
            //else if (IsDecorationName(objectName))
            //{
           //     SetLayerIfExists(renderer.gameObject, GuessDecorationLayer(objectName), false);
         //       SetSorting(renderer, GuessDecorationSortingLayer(objectName), GameplayBalance.DecorationSortingOrder);
       //     }
      //  }
    //}

    private static void SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.orthographic = true;
        camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, 5.2f, 7.2f);
        camera.transform.localScale = Vector3.one;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.19215687f, 0.3019608f, 0.4745098f, 1f);
        camera.allowHDR = false;

        if (camera.GetComponent<CameraFollow2D>() == null)
        {
            camera.gameObject.AddComponent<CameraFollow2D>();
        }
    }

    private static void RemoveLegacyPlayerSpriteGhosts(GameObject player)
    {
        Sprite[] safeIdleSprites = null;
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || !UsesAnyPlayerCharacterSprite(renderer))
            {
                continue;
            }

            bool belongsToPlayer = player != null && (renderer.gameObject == player || renderer.transform.IsChildOf(player.transform));
            if (belongsToPlayer)
            {
                if (IsLegacyPlayerSprite(renderer.sprite))
                {
                    Sprite replacement = GetSafeIdleSprite(ref safeIdleSprites);
                    if (replacement != null)
                    {
                        renderer.sprite = replacement;
                        renderer.color = Color.white;
                        renderer.enabled = true;
                        Debug.Log($"Codex legacy player sprite replaced on player renderer: {GetHierarchyPath(renderer.transform)} -> {replacement.name}");
                    }
                }

                continue;
            }

            Debug.LogWarning($"Codex stray player sprite ghost disabled: {GetHierarchyPath(renderer.transform)} sprite={GetSpriteDebugName(renderer.sprite)}");
            renderer.enabled = false;
            renderer.gameObject.SetActive(false);
        }
    }

    private static void DisableMisassignedBackgroundAnimators()
    {
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Animator animator in animators)
        {
            if (animator == null || animator.runtimeAnimatorController == null || !LooksLikeCloudObject(animator.gameObject))
            {
                continue;
            }

            if (!UsesPlayerJumpAnimation(animator.runtimeAnimatorController))
            {
                continue;
            }

            Debug.LogWarning($"Codex misassigned background animator disabled: {GetHierarchyPath(animator.transform)} controller={animator.runtimeAnimatorController.name}");
            animator.enabled = false;
            animator.runtimeAnimatorController = null;
        }
    }

    private static bool LooksLikeCloudObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return false;
        }

        return ContainsIgnoreCase(gameObject.name, "Nuvens")
            || ContainsIgnoreCase(GetHierarchyPath(gameObject.transform), "AssetsMapa/Nuvens");
    }

    private static bool UsesPlayerJumpAnimation(RuntimeAnimatorController controller)
    {
        if (controller == null)
        {
            return false;
        }

        if (ContainsIgnoreCase(controller.name, "Pulo"))
        {
            return true;
        }

        AnimationClip[] clips = controller.animationClips;
        for (int index = 0; index < clips.Length; index++)
        {
            AnimationClip clip = clips[index];
            if (clip != null && ContainsIgnoreCase(clip.name, "Pulo"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool UsesAnyPlayerCharacterSprite(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        if (ContainsAnyPlayerSpriteFragment(renderer.sprite != null ? renderer.sprite.name : null))
        {
            return true;
        }

        Texture2D spriteTexture = renderer.sprite != null ? renderer.sprite.texture : null;
        if (spriteTexture != null && ContainsAnyPlayerSpriteFragment(spriteTexture.name))
        {
            return true;
        }

        Material material = renderer.sharedMaterial;
        Texture mainTexture = material != null ? material.mainTexture : null;
        return mainTexture != null && ContainsAnyPlayerSpriteFragment(mainTexture.name);
    }

    private static bool IsLegacyPlayerSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        if (ContainsIgnoreCase(sprite.name, LegacyPlayerSpriteNameFragment))
        {
            return true;
        }

        Texture2D texture = sprite.texture;
        return texture != null && ContainsIgnoreCase(texture.name, LegacyPlayerSpriteNameFragment);
    }

    private static Sprite GetSafeIdleSprite(ref Sprite[] safeIdleSprites)
    {
        if (safeIdleSprites == null)
        {
            safeIdleSprites = Resources.LoadAll<Sprite>(SafeIdleSpriteResourcePath);
        }

        if (safeIdleSprites == null || safeIdleSprites.Length == 0)
        {
            return null;
        }

        for (int index = 0; index < safeIdleSprites.Length; index++)
        {
            Sprite sprite = safeIdleSprites[index];
            if (sprite != null && ContainsIgnoreCase(sprite.name, "player_idle_01"))
            {
                return sprite;
            }
        }

        return safeIdleSprites[0];
    }

    private static bool ContainsIgnoreCase(string value, string fragment)
    {
        return !string.IsNullOrEmpty(value)
            && !string.IsNullOrEmpty(fragment)
            && value.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ContainsAnyPlayerSpriteFragment(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        for (int index = 0; index < PlayerSpriteNameFragments.Length; index++)
        {
            if (ContainsIgnoreCase(value, PlayerSpriteNameFragments[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetSpriteDebugName(Sprite sprite)
    {
        if (sprite == null)
        {
            return "<none>";
        }

        string textureName = sprite.texture != null ? sprite.texture.name : "<no texture>";
        return $"{sprite.name}/{textureName}";
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static System.Collections.IEnumerator LogVisibleRendererProbe()
    {
        yield return new WaitForSeconds(2f);

        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.Log("Codex renderer probe skipped: no main camera");
            yield break;
        }

        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || renderer.sprite == null)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            Vector3 viewportCenter = camera.WorldToViewportPoint(bounds.center);
            Rect viewportRect = CalculateViewportRect(camera, bounds);
            bool inTopRight = viewportRect.xMax > 0.55f && viewportRect.yMax > 0.55f && viewportRect.xMin < 1.08f && viewportRect.yMin < 1.08f;
            bool playerLike = IsLegacyPlayerSprite(renderer.sprite) || ContainsIgnoreCase(renderer.sprite.name, "player") || ContainsIgnoreCase(renderer.sprite.texture.name, "player");
            if (!inTopRight && !playerLike)
            {
                continue;
            }

            Material material = renderer.sharedMaterial;
            Texture mainTexture = material != null ? material.mainTexture : null;
            Debug.Log(
                $"Codex renderer probe: path={GetHierarchyPath(renderer.transform)} sprite={GetSpriteDebugName(renderer.sprite)} " +
                $"enabled={renderer.enabled} visible={renderer.isVisible} alpha={renderer.color.a:0.###} " +
                $"sorting={renderer.sortingLayerName}/{renderer.sortingOrder} worldCenter={bounds.center} worldSize={bounds.size} " +
                $"viewportCenter={viewportCenter} viewportRect={viewportRect} material={GetObjectName(material)} materialTexture={GetObjectName(mainTexture)}");
        }
    }

    private static Rect CalculateViewportRect(Camera camera, Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] points =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, max.z)
        };

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;
        bool hasPoint = false;

        for (int index = 0; index < points.Length; index++)
        {
            Vector3 viewportPoint = camera.WorldToViewportPoint(points[index]);
            if (viewportPoint.z <= 0f)
            {
                continue;
            }

            minX = Mathf.Min(minX, viewportPoint.x);
            minY = Mathf.Min(minY, viewportPoint.y);
            maxX = Mathf.Max(maxX, viewportPoint.x);
            maxY = Mathf.Max(maxY, viewportPoint.y);
            hasPoint = true;
        }

        if (!hasPoint)
        {
            return new Rect(-10f, -10f, 0f, 0f);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static string GetObjectName(Object target)
    {
        return target != null ? target.name : "<none>";
    }

    private static void SetupEnemies()
    {
        foreach (GameObject enemy in FindEnemyInstances(true))
        {
            if (!EnableFlyingEnemies && IsCrowEnemy(enemy))
            {
                enemy.SetActive(false);
                continue;
            }

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
            if (!EnableFlyingEnemies && isCrow)
            {
                enemy.SetActive(false);
                continue;
            }

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
        GameObject crowTemplate = EnableFlyingEnemies ? FindEnemyTemplate("crow") : null;
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

        float yOffset = isCrow ? FlyingEnemySpawnOffset : GroundedWolfSpawnOffset;
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

    private static bool IsCrowEnemy(GameObject enemy)
    {
        return enemy != null && enemy.name.ToLowerInvariant().Contains("crow");
    }

    private static bool IsGroundName(string name)
    {
        string lowerName = name.ToLowerInvariant();
        if (IsNonSolidMapName(lowerName))
        {
            return false;
        }

        return lowerName.Contains("bloco") ||
               lowerName.Contains("chao") ||
               lowerName.Contains("ground") ||
               (lowerName.Contains("bridge") && !lowerName.Contains("decoration"));
    }

    private static bool IsNonSolidMapName(string name)
    {
        string lowerName = name.ToLowerInvariant();
        return lowerName.Contains("mato") ||
               lowerName.Contains("grama") ||
               lowerName.Contains("tree") ||
               lowerName.Contains("arvore") ||
               lowerName.Contains("fundo") ||
               lowerName.Contains("background") ||
               lowerName.Contains("groundinbackground") ||
               lowerName.Contains("layer") ||
               lowerName.Contains("lua") ||
               lowerName.Contains("nuvem") ||
               lowerName.Contains("montanha") ||
               lowerName.Contains("liana") ||
               lowerName.Contains("rag") ||
               lowerName.Contains("berrie") ||
               lowerName.Contains("lamp") ||
               lowerName.Contains("tocha") ||
               lowerName.Contains("fogo") ||
               lowerName.Contains("decoration");
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
               lowerName.Contains("liana") ||
               lowerName.Contains("rag") ||
               lowerName.Contains("berrie") ||
               lowerName.Contains("lamp") ||
               lowerName.Contains("tocha") ||
               lowerName.Contains("fogo") ||
               lowerName.Contains("decoration");
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
        return lowerName;


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

        return lowerName;
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

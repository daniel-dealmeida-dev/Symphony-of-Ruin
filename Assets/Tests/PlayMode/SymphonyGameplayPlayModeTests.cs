using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SymphonyGameplayPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
    private const BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
    private string prefix;
    private Sprite testSprite;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        prefix = "QA_" + Guid.NewGuid().ToString("N") + "_";
        testSprite = CreateTestSprite();
        yield return DestroyQaObjects();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return DestroyQaObjects();
        if (testSprite != null)
        {
            UnityEngine.Object.Destroy(testSprite.texture);
            UnityEngine.Object.Destroy(testSprite);
        }
    }

    [UnityTest]
    public IEnumerator CameraSnapsFollowsAndFramesNearbyCombat()
    {
        CreateGround("BlocoGround", new Vector2(0f, -1f), new Vector2(70f, 1f));
        GameObject player = CreatePlayer(new Vector3(-10f, 0f, 0f));
        GameObject enemy = CreateEnemy(new Vector3(-7f, 0f, 0f), false);
        Camera camera = CreateCamera(new Vector3(-42f, -36f, -25f), 5.9f);

        camera.gameObject.AddComponent(GameType("CameraFollow2D"));
        yield return null;

        Assert.That(camera.transform.position.x, Is.EqualTo(player.transform.position.x).Within(0.25f));
        Assert.That(camera.transform.position.y, Is.EqualTo(player.transform.position.y + 1.25f).Within(0.25f));
        Assert.That(camera.transform.position.z, Is.EqualTo(-25f).Within(0.01f));

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        playerBody.velocity = new Vector2(7.5f, 0f);
        player.transform.position = new Vector3(8f, 0f, 0f);
        enemy.transform.position = new Vector3(10.5f, 0f, 0f);

        for (int i = 0; i < 20; i++)
        {
            yield return null;
        }

        Assert.That(camera.transform.position.x, Is.GreaterThan(0f));
        AssertInViewport(camera, player.transform.position, "jogador");
        AssertInViewport(camera, enemy.transform.position, "inimigo proximo");
    }

    [UnityTest]
    public IEnumerator CameraClampsAtPlayableBounds()
    {
        CreateGround("BlocoGround", new Vector2(0f, -1f), new Vector2(80f, 1f));
        GameObject player = CreatePlayer(new Vector3(0f, 0f, 0f));
        Camera camera = CreateCamera(new Vector3(0f, 0f, -25f), 5.9f);

        camera.gameObject.AddComponent(GameType("CameraFollow2D"));
        yield return null;

        player.transform.position = new Vector3(120f, 0f, 0f);
        for (int i = 0; i < 24; i++)
        {
            yield return null;
        }

        Bounds bounds = CalculatePlayableBounds();
        float halfWidth = camera.orthographicSize * camera.aspect;
        float expectedMaxX = bounds.max.x - halfWidth + 3.5f;
        Assert.That(camera.transform.position.x, Is.LessThanOrEqualTo(expectedMaxX + 0.2f));
    }

    [Test]
    public void DefaultKeyboardControlsUseArrowsAndDirectAttackKeys()
    {
        Type defaultsType = GameType("GameActionDefaults");
        Type actionType = GameType("GameAction");
        MethodInfo getDefaultKey = defaultsType.GetMethod("GetDefaultKey", PublicStatic);
        Assert.NotNull(getDefaultKey, "GameActionDefaults.GetDefaultKey nao encontrado.");

        Assert.That(GetDefaultKey(getDefaultKey, actionType, "MoveLeft"), Is.EqualTo(KeyCode.LeftArrow));
        Assert.That(GetDefaultKey(getDefaultKey, actionType, "MoveRight"), Is.EqualTo(KeyCode.RightArrow));
        Assert.That(GetDefaultKey(getDefaultKey, actionType, "Jump"), Is.EqualTo(KeyCode.Space));
        Assert.That(GetDefaultKey(getDefaultKey, actionType, "AttackLine1"), Is.EqualTo(KeyCode.Z));
        Assert.That(GetDefaultKey(getDefaultKey, actionType, "AttackLine2"), Is.EqualTo(KeyCode.X));
        Assert.That(GetDefaultKey(getDefaultKey, actionType, "AttackLine3"), Is.EqualTo(KeyCode.C));
        Assert.That(GetDefaultKey(getDefaultKey, actionType, "AttackLine4"), Is.EqualTo(KeyCode.V));
    }

    [Test]
    public void SpaceIsReservedForJump()
    {
        object settings = Activator.CreateInstance(GameType("SettingsService"));
        Type actionType = GameType("GameAction");
        object attackLine1 = Enum.Parse(actionType, "AttackLine1");
        MethodInfo tryRebind = settings.GetType().GetMethod("TryRebind", PublicInstance);
        Assert.NotNull(tryRebind, "SettingsService.TryRebind nao encontrado.");

        object[] args = { attackLine1, KeyCode.Space, null };
        bool accepted = (bool)tryRebind.Invoke(settings, args);
        string error = args[2] as string;

        Assert.IsFalse(accepted);
        Assert.That(error, Does.Contain("pular"));
    }

    [Test]
    public void MovementAcceleratesStopsAndClampsFallSpeed()
    {
        GameObject player = CreatePlayer(new Vector3(0f, 0f, 0f));
        Component movimento = GetGameComponent(player, "MovimentoJogador");
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        SetPrivateField(movimento, "noChao", true);
        SetPrivateField(movimento, "moveX", 1f);
        InvokePrivate(movimento, "FixedUpdate");
        Assert.That(body.velocity.x, Is.GreaterThan(0f));
        Assert.That(body.velocity.x, Is.LessThan(7.5f));

        for (int i = 0; i < 12; i++)
        {
            InvokePrivate(movimento, "FixedUpdate");
        }

        Assert.That(body.velocity.x, Is.EqualTo(7.5f).Within(0.35f));

        SetPrivateField(movimento, "moveX", 0f);
        float velocityBeforeStop = body.velocity.x;
        InvokePrivate(movimento, "FixedUpdate");
        Assert.That(body.velocity.x, Is.LessThan(velocityBeforeStop));

        SetPrivateField(movimento, "noChao", false);
        body.velocity = new Vector2(0f, -100f);
        InvokePrivate(movimento, "FixedUpdate");
        float maxVelocidadeQueda = (float)GetPrivateField(movimento, "maxVelocidadeQueda");
        Assert.That(body.velocity.y, Is.GreaterThanOrEqualTo(maxVelocidadeQueda - 0.01f));
    }

    [Test]
    public void JumpUsesMetroidvaniaHeightTimingShortHopAndForgivenessWindows()
    {
        GameObject player = CreatePlayer(new Vector3(0f, 0f, 0f));
        Component movimento = GetGameComponent(player, "MovimentoJogador");
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        SetPrivateField(movimento, "noChao", true);
        InvokePrivate(movimento, "Pular");
        float alturaPuloMaxima = (float)GetPrivateField(movimento, "alturaPuloMaxima");
        float tempoAteTopoPulo = (float)GetPrivateField(movimento, "tempoAteTopoPulo");
        float gravidadePulo = (2f * alturaPuloMaxima) / (tempoAteTopoPulo * tempoAteTopoPulo);
        float velocidadeInicialEsperada = gravidadePulo * tempoAteTopoPulo;
        Assert.That(body.velocity.y, Is.EqualTo(velocidadeInicialEsperada).Within(0.2f));

        body.velocity = new Vector2(0f, 10f);
        SetPrivateField(movimento, "noChao", false);
        SetPrivateField(movimento, "puloSegurado", false);
        InvokePrivate(movimento, "FixedUpdate");
        Assert.That(body.velocity.y, Is.LessThan(10f));

        body.velocity = new Vector2(0f, 20f);
        InvokePrivate(movimento, "CortarPuloSeNecessario");
        float alturaPuloMinima = (float)GetPrivateField(movimento, "alturaPuloMinima");
        float velocidadeMinimaEsperada = Mathf.Sqrt(2f * gravidadePulo * alturaPuloMinima);
        Assert.That(body.velocity.y, Is.EqualTo(velocidadeMinimaEsperada).Within(0.2f));

        Assert.That((float)GetPrivateField(movimento, "tempoCoyote"), Is.GreaterThan(0f));
        Assert.That((float)GetPrivateField(movimento, "tempoBufferPulo"), Is.GreaterThan(0f));
    }

    [UnityTest]
    public IEnumerator AttackUsesFacingDirectionAndDelayedActiveHitWindow()
    {
        GameObject player = CreatePlayer(new Vector3(0f, 0f, 0f));
        Component movimento = GetGameComponent(player, "MovimentoJogador");
        Component rightEnemy = GetGameComponent(CreateEnemy(new Vector3(1.2f, 0f, 0f), false), "EnemyHealth");
        Component leftEnemy = GetGameComponent(CreateEnemy(new Vector3(-1.2f, 0f, 0f), false), "EnemyHealth");

        SetPrivateField(movimento, "moveX", 1f);
        InvokePrivate(movimento, "AtualizarDirecaoPorEntrada");
        InvokePrivate(movimento, "Atacar");

        Assert.That(GetIntProperty(rightEnemy, "CurrentHealth"), Is.EqualTo(GetIntProperty(rightEnemy, "MaxHealth")));
        Assert.That(GetIntProperty(leftEnemy, "CurrentHealth"), Is.EqualTo(GetIntProperty(leftEnemy, "MaxHealth")));

        yield return new WaitForSeconds(0.14f);

        Assert.That(GetIntProperty(rightEnemy, "CurrentHealth"), Is.EqualTo(GetIntProperty(rightEnemy, "MaxHealth") - 1));
        Assert.That(GetIntProperty(leftEnemy, "CurrentHealth"), Is.EqualTo(GetIntProperty(leftEnemy, "MaxHealth")));
    }

    [UnityTest]
    public IEnumerator EnemyAttackHasWindupAndCooldownDamage()
    {
        CreateGround("BlocoGround", new Vector2(0f, -1f), new Vector2(20f, 1f));
        GameObject player = CreatePlayer(new Vector3(0f, 0f, 0f));
        Component playerHealth = GetGameComponent(player, "PlayerHealth");
        GameObject enemy = CreateEnemy(new Vector3(12f, 0f, 0f), false);
        Component patrol = GetGameComponent(enemy, "EnemyPatrol2D");

        SetPrivateField(patrol, "attackRange", 2f);
        SetPrivateField(patrol, "attackWindup", 0.08f);
        SetPrivateField(patrol, "attackCooldown", 0.6f);

        yield return new WaitForSeconds(GetGameplayBalanceFloat("PlayerSpawnDamageGraceSeconds") + 0.05f);
        player.transform.position = Vector3.zero;
        enemy.transform.position = new Vector3(0.9f, 0f, 0f);
        SetPrivateField(playerHealth, "nextDamageTime", 0f);
        SetPrivateField(patrol, "nextAttackTime", 0f);

        int before = GetIntProperty(playerHealth, "CurrentHealth");

        InvokePublic(patrol, "TryApplyContactDamage", playerHealth);
        Assert.That(GetIntProperty(playerHealth, "CurrentHealth"), Is.EqualTo(before));

        yield return new WaitForSeconds(0.12f);
        Assert.That(GetIntProperty(playerHealth, "CurrentHealth"), Is.EqualTo(before - 1));
    }

    [Test]
    public void EnemyGroundProbeDetectsLedgesBeforeChasing()
    {
        CreateGround("BlocoGround", new Vector2(0f, -1f), new Vector2(4f, 1f));
        GameObject enemy = CreateEnemy(new Vector3(1.95f, 0f, 0f), false);
        Component patrol = GetGameComponent(enemy, "EnemyPatrol2D");

        SetPrivateField(patrol, "direction", 1);
        bool shouldTurn = (bool)InvokePrivate(patrol, "ShouldTurnAround");

        Assert.IsTrue(shouldTurn);
    }

    private GameObject CreateGround(string name, Vector2 position, Vector2 size)
    {
        var ground = new GameObject(prefix + name);
        ground.transform.position = position;
        ground.transform.localScale = new Vector3(size.x, size.y, 1f);
        int groundLayer = LayerMask.NameToLayer("chao");
        if (groundLayer >= 0)
        {
            ground.layer = groundLayer;
        }

        var renderer = ground.AddComponent<SpriteRenderer>();
        renderer.sprite = testSprite;
        renderer.sortingLayerName = "chao";

        var collider = ground.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        return ground;
    }

    private GameObject CreatePlayer(Vector3 position)
    {
        var player = new GameObject(prefix + "Protagonista");
        TrySetTag(player, "Player");
        player.transform.position = position;
        float playerScale = GetGameplayBalanceFloat("PlayerVisualScale");
        player.transform.localScale = new Vector3(playerScale, playerScale, 1f);
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            player.layer = playerLayer;
        }

        var renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = testSprite;
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 1f;
        body.freezeRotation = true;
        var collider = player.AddComponent<CapsuleCollider2D>();
        collider.direction = CapsuleDirection2D.Vertical;
        collider.size = new Vector2(2.5f, 5.27f);

        player.AddComponent(GameType("MovimentoJogador"));
        if (GetGameComponent(player, "PlayerHealth") == null)
        {
            player.AddComponent(GameType("PlayerHealth"));
        }

        return player;
    }

    private GameObject CreateEnemy(Vector3 position, bool flying)
    {
        var enemy = new GameObject(prefix + (flying ? "Crow" : "Wolf"));
        TrySetTag(enemy, "Inimigos");
        enemy.transform.position = position;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            enemy.layer = enemyLayer;
        }

        var renderer = enemy.AddComponent<SpriteRenderer>();
        renderer.sprite = testSprite;
        var body = enemy.AddComponent<Rigidbody2D>();
        body.gravityScale = flying ? 0f : 1.6f;
        body.freezeRotation = true;
        enemy.AddComponent<BoxCollider2D>();
        enemy.AddComponent(GameType("EnemyPresentation2D"));
        enemy.AddComponent(GameType("EnemyHealth"));
        Component patrol = enemy.AddComponent(GameType("EnemyPatrol2D"));
        SetPublicProperty(patrol, "FlyingEnemy", flying);
        return enemy;
    }

    private Camera CreateCamera(Vector3 position, float orthographicSize)
    {
        var cameraObject = new GameObject(prefix + "Main Camera");
        TrySetTag(cameraObject, "MainCamera");
        cameraObject.transform.position = position;
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = orthographicSize;
        return camera;
    }

    private static Sprite CreateTestSprite()
    {
        var texture = new Texture2D(16, 16);
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
    }

    private static void AssertInViewport(Camera camera, Vector3 worldPosition, string label)
    {
        Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
        Assert.That(viewport.x, Is.InRange(0.05f, 0.95f), label + " fora do enquadramento horizontal");
        Assert.That(viewport.y, Is.InRange(0.05f, 0.95f), label + " fora do enquadramento vertical");
        Assert.That(viewport.z, Is.GreaterThan(0f), label + " atras da camera");
    }

    private static Type GameType(string typeName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        Assert.Fail("Tipo do jogo nao encontrado: " + typeName);
        return null;
    }

    private static Component GetGameComponent(GameObject target, string typeName)
    {
        return target.GetComponent(GameType(typeName));
    }

    private static Bounds CalculatePlayableBounds()
    {
        MethodInfo method = GameType("EnemySceneBootstrap").GetMethod("CalculatePlayableBounds", PublicStatic);
        Assert.NotNull(method, "EnemySceneBootstrap.CalculatePlayableBounds nao encontrado.");
        return (Bounds)method.Invoke(null, null);
    }

    private static float GetGameplayBalanceFloat(string fieldName)
    {
        FieldInfo field = GameType("GameplayBalance").GetField(fieldName, PublicStatic);
        Assert.NotNull(field, "GameplayBalance." + fieldName + " nao encontrado.");
        object value = field.GetRawConstantValue() ?? field.GetValue(null);
        return Convert.ToSingle(value);
    }

    private static KeyCode GetDefaultKey(MethodInfo getDefaultKey, Type actionType, string actionName)
    {
        object action = Enum.Parse(actionType, actionName);
        return (KeyCode)getDefaultKey.Invoke(null, new[] { action });
    }

    private static object InvokePrivate(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance);
        Assert.NotNull(method, "Metodo privado nao encontrado: " + methodName);
        return method.Invoke(target, args);
    }

    private static object InvokePublic(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, PublicInstance);
        Assert.NotNull(method, "Metodo publico nao encontrado: " + methodName);
        return method.Invoke(target, args);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.NotNull(field, "Campo privado nao encontrado: " + fieldName);
        field.SetValue(target, value);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
        Assert.NotNull(field, "Campo privado nao encontrado: " + fieldName);
        return field.GetValue(target);
    }

    private static int GetIntProperty(object target, string propertyName)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, PublicInstance);
        Assert.NotNull(property, "Propriedade publica nao encontrada: " + propertyName);
        return Convert.ToInt32(property.GetValue(target));
    }

    private static void SetPublicProperty(object target, string propertyName, object value)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, PublicInstance);
        Assert.NotNull(property, "Propriedade publica nao encontrada: " + propertyName);
        property.SetValue(target, value);
    }

    private static void TrySetTag(GameObject target, string tag)
    {
        try
        {
            target.tag = tag;
        }
        catch (UnityException)
        {
        }
    }

    private static IEnumerator DestroyQaObjects()
    {
        GameObject[] objects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject item in objects)
        {
            if (item == null)
            {
                continue;
            }

            bool isQaObject = item.name.StartsWith("QA_", StringComparison.Ordinal) ||
                              item.name == "__GameServices" ||
                              item.name == "GameManager" ||
                              item.name == "EnemySceneBootstrap" ||
                              item.name == "GameplayCanvas" ||
                              item.name == "EventSystem";
            if (isQaObject)
            {
                UnityEngine.Object.Destroy(item);
            }
        }

        yield return null;
    }
}

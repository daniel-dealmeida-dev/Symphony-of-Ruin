public static class GameplayBalance
{
    public const int PlayerMaxHealth = 10;
    public const int PlayerInitialHealth = PlayerMaxHealth;
    public const float PlayerInvulnerabilityAfterHitSeconds = 1.35f;
    public const float PlayerSpawnDamageGraceSeconds = 0.45f;
    public const float PlayerKnockbackForce = 23f;

    public const float PlayerVisualScale = 0.36f;
    public const float PlayerCollisionWidth = 0.9f;
    public const float PlayerCollisionHeight = 1.9f;

    public const int WolfDamage = 1;
    public const float WolfAttackCooldownSeconds = 1.35f;
    public const int DefaultEnemyDamage = 1;
    public const float DefaultEnemyAttackCooldownSeconds = 1.25f;
    public const float EnemyContactDamageCooldownSeconds = 1.25f;

    public const int BackgroundSortingOrder = -100;
    public const int TerrainSortingOrder = 0;
    public const int DecorationSortingOrder = 10;
    public const int EnemySortingOrder = 30;
    public const int PlayerSortingOrder = 40;
    public const int ForegroundSortingOrder = 80;
}

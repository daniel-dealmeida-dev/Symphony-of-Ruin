using UnityEngine;

/// <summary>
/// Multiplicador global de dificuldade baseado no tempo na fase (inimigos mais rápidos).
/// Mantém a mecânica original e apenas escala velocidade quando IA lê este valor.
/// </summary>
public static class DifficultyRuntime
{
    public static float EnemySpeedMultiplier { get; private set; } = 1f;

    private static float elapsed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDomain()
    {
        elapsed = 0f;
        EnemySpeedMultiplier = 1f;
    }

    /// <param name="deltaTime">Use <see cref="Time.deltaTime"/> quando o jogo não estiver pausado.</param>
    public static void Tick(float deltaTime)
    {
        elapsed += deltaTime;
        // Sobe devagar até ~1.45x em 5 minutos
        EnemySpeedMultiplier = 1f + Mathf.Clamp01(elapsed / 300f) * 0.45f;
    }
}

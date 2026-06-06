using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private const int StartingScore = 100;
    private const int WolfDefeatScore = 20;
    private const int ConvoDefeatScore = 30;

    private int currentScore = StartingScore;
    private int enemiesDefeated;
    private int elapsedSeconds;
    private float elapsedAccumulator;
    private bool scoringActive;
    private bool scoreFinalized;
    private DatabaseManager database;

    public int CurrentScore
    {
        get { return currentScore; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!scoringActive || scoreFinalized)
        {
            return;
        }

        elapsedAccumulator += Time.deltaTime;
        while (elapsedAccumulator >= 1f)
        {
            elapsedAccumulator -= 1f;
            elapsedSeconds++;
            ChangeScore(-1);
        }
    }

    public static ScoreManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        ScoreManager existing = FindFirstObjectByType<ScoreManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject scoreObject = new GameObject("__ScoreManager");
        return scoreObject.AddComponent<ScoreManager>();
    }

    public void StartRun()
    {
        database = DatabaseManager.EnsureInstance();
        currentScore = StartingScore;
        enemiesDefeated = 0;
        elapsedSeconds = 0;
        elapsedAccumulator = 0f;
        scoreFinalized = false;
        scoringActive = true;
        SaveCurrentScore();
    }

    public void RegistrarMorteMonstro()
    {
        RegisterEnemyDefeated(null);
    }

    public void RegisterEnemyDefeated(GameObject enemy)
    {
        if (!scoringActive || scoreFinalized)
        {
            return;
        }

        enemiesDefeated++;
        ChangeScore(GetEnemyScoreValue(enemy));

        if (AreAllEnemiesDefeated())
        {
            FinalizeScore();
        }
    }

    public void FinalizeScore()
    {
        if (scoreFinalized)
        {
            return;
        }

        scoringActive = false;
        scoreFinalized = true;
        SaveCurrentScore();
    }

    public void StopForLastPlatform()
    {
        FinalizeScore();
    }

    public int GetScore()
    {
        return currentScore;
    }

    public int GetMonstrosMortos()
    {
        return enemiesDefeated;
    }

    public int GetTempoVivo()
    {
        return elapsedSeconds;
    }

    public bool IsScoringActive()
    {
        return scoringActive;
    }

    private int GetEnemyScoreValue(GameObject enemy)
    {
        if (enemy == null)
        {
            return 0;
        }

        string enemyName = enemy.name.ToLowerInvariant();
        if (enemyName.Contains("wolf"))
        {
            return WolfDefeatScore;
        }

        if (enemyName.Contains("convo") || enemyName.Contains("crow"))
        {
            return ConvoDefeatScore;
        }

        return 0;
    }

    private bool AreAllEnemiesDefeated()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && !enemies[i].IsDead)
            {
                return false;
            }
        }

        Saude[] legacyHealth = FindObjectsByType<Saude>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < legacyHealth.Length; i++)
        {
            Saude health = legacyHealth[i];
            bool isLegacyEnemy = health != null
                && !health.morto
                && health.gameObject.tag != "Player"
                && health.GetComponent<EnemyHealth>() == null;

            if (isLegacyEnemy)
            {
                return false;
            }
        }

        return true;
    }

    private void ChangeScore(int amount)
    {
        currentScore += amount;
        SaveCurrentScore();
    }

    private void SaveCurrentScore()
    {
        if (database == null)
        {
            database = DatabaseManager.EnsureInstance();
        }

        database.SaveScoringData(currentScore);
    }
}

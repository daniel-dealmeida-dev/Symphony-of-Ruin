using System;
using System.Data;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;

[Serializable]
public class RankingEntry
{
    public string PlayerName;
    public int Score;
}

[Serializable]
public class ScoringData
{
    public int CurrentScore = 100;
    public int HighScore = 100;
}

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    private string dbPath;
    private bool isInitialized;

    public static DatabaseManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        DatabaseManager existing = FindFirstObjectByType<DatabaseManager>();
        if (existing != null)
        {
            Instance = existing;
            existing.InitializeDatabase();
            return Instance;
        }

        GameObject databaseObject = new GameObject("__DatabaseManager");
        return databaseObject.AddComponent<DatabaseManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeDatabase();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =========================
    // CREATE TABLE
    // =========================
    public void CreateSchema()
    {
        if (string.IsNullOrEmpty(dbPath))
        {
            dbPath = $"URI=file:{Application.persistentDataPath}/game.db";
        }

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS PlayerScores (
                            name TEXT PRIMARY KEY,
                            score INTEGER NOT NULL
                        );
                    ";

                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Scoring (
                            id INTEGER PRIMARY KEY CHECK (id = 1),
                            current_score INTEGER NOT NULL,
                            high_score INTEGER NOT NULL
                        );
                    ";

                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"
                        INSERT OR IGNORE INTO Scoring
                        (id, current_score, high_score)
                        VALUES
                        (1, 100, 100);
                    ";

                    cmd.ExecuteNonQuery();
                }
            }

            Debug.Log($"SQLite OK: {dbPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SQLite CreateSchema Error: {ex}");
        }
    }

    private void InitializeDatabase()
    {
        if (isInitialized)
        {
            return;
        }

        GameServices.EnsureInstance();
        DontDestroyOnLoad(gameObject);

        dbPath = $"URI=file:{Application.persistentDataPath}/game.db";

        CreateSchema();
        isInitialized = true;
    }

    public void SavePlayerDataPlayer1(int score)
    {
        string playerName = "Player1";

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT OR REPLACE INTO PlayerScores
                        (name, score)
                        VALUES
                        (@name, @score);
                    ";

                    cmd.Parameters.AddWithValue("@name", playerName);
                    cmd.Parameters.AddWithValue("@score", score);

                    cmd.ExecuteNonQuery();
                }
            }

            if (GameServices.Instance != null)
                GameServices.Instance.Settings.SetCoins(score);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SQLite Save Error: {ex}");
        }
    }

    // =========================
    // SAVE SCORE
    // =========================
    public void SavePlayerData(string playerName, int score)
    {
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT OR REPLACE INTO PlayerScores
                        (name, score)
                        VALUES
                        (@name, @score);
                    ";

                    cmd.Parameters.AddWithValue("@name", playerName);
                    cmd.Parameters.AddWithValue("@score", score);

                    cmd.ExecuteNonQuery();
                }
            }

            if (GameServices.Instance != null)
                GameServices.Instance.Settings.SetCoins(score);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SQLite Save Error: {ex}");
        }
    }

    // =========================
    // LOAD SCORE
    // =========================
    public int GetPlayerScore(string playerName)
    {
        int score = 0;

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT score FROM PlayerScores WHERE name = @name LIMIT 1";

                    cmd.Parameters.AddWithValue("@name", playerName);

                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                        score = Convert.ToInt32(result);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SQLite Load Error: {ex}");
        }

        return score;
    }

    public ScoringData GetScoringData()
    {
        ScoringData scoring = new ScoringData();

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT current_score, high_score FROM Scoring WHERE id = 1 LIMIT 1";

                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            scoring.CurrentScore = reader.GetInt32(0);
                            scoring.HighScore = reader.GetInt32(1);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SQLite Scoring Load Error: {ex}");
        }

        return scoring;
    }

    public void SaveScoringData(int currentScore)
    {
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"
                        INSERT OR IGNORE INTO Scoring
                        (id, current_score, high_score)
                        VALUES
                        (1, @currentScore, @currentScore);
                    ";

                    cmd.Parameters.AddWithValue("@currentScore", currentScore);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"
                        UPDATE Scoring SET
                            current_score = @currentScore,
                            high_score = CASE
                                WHEN @currentScore > high_score THEN @currentScore
                                ELSE high_score
                            END
                        WHERE id = 1;
                    ";

                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SQLite Scoring Save Error: {ex}");
        }
    }

    public void FinalizeScoringData(int currentScore)
    {
        SaveScoringData(currentScore);
    }

    // =========================
    // TOP 10 RANKING
    // =========================
    public List<RankingEntry> GetTop10()
    {
        List<RankingEntry> ranking = new List<RankingEntry>();

        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT name, score FROM PlayerScores ORDER BY score DESC LIMIT 10";

                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ranking.Add(new RankingEntry
                            {
                                PlayerName = reader.GetString(0),
                                Score = reader.GetInt32(1)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ranking Error: {ex}");
        }

        return ranking;
    }

    // =========================
    // CHECK PLAYER EXISTS
    // =========================
    public bool PlayerExists(string playerName)
    {
        try
        {
            using (var connection = new SqliteConnection(dbPath))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT COUNT(*) FROM PlayerScores WHERE name = @name";

                    cmd.Parameters.AddWithValue("@name", playerName);

                    long count = (long)cmd.ExecuteScalar();

                    return count > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SQLite Exists Error: {ex}");
            return false;
        }
    }
}

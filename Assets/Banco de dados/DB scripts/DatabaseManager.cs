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

public class DatabaseManager : MonoBehaviour
{
    private string dbPath;

    private void Awake()
    {
        GameServices.EnsureInstance();
        DontDestroyOnLoad(gameObject);

        dbPath = $"URI=file:{Application.persistentDataPath}/game.db";

        CreateSchema();
    }

    // =========================
    // CREATE TABLE
    // =========================
    public void CreateSchema()
    {
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
                }
            }

            Debug.Log($"SQLite OK: {dbPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SQLite CreateSchema Error: {ex}");
        }
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
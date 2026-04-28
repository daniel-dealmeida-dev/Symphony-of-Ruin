using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    private const string ScorePrefix = "score_";

    private void Awake()
    {
        GameServices.EnsureInstance();
        DontDestroyOnLoad(gameObject);
    }

    public void CreateSchema()
    {
        PlayerPrefs.Save();
    }

    public void SavePlayerData(string name, int score)
    {
        PlayerPrefs.SetInt(ScorePrefix + name, score);
        PlayerPrefs.Save();
        GameServices.Instance.Settings.SetCoins(score);
    }

    public int GetPlayerScore(string name)
    {
        return PlayerPrefs.GetInt(ScorePrefix + name, 0);
    }
}

using System.Data;
using Mono.Data.Sqlite; // Se der erro aqui, verifique se as DLLs estão na pasta Plugins
using System.IO;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    private string dbPath;

    void Start()
    {
        // vira um objeto permanente
        DontDestroyOnLoad(gameObject);
        // Define o caminho do banco de dados (no PC ou Celular)
        dbPath = "URI=file:" + Application.persistentDataPath + "/MyDatabase.db";
        Debug.Log("Caminho para o banco"+dbPath);

        // Agora a função existe abaixo e pode ser chamada!
        CreateSchema();

        // Teste rápido: Salvar e Carregar
        SavePlayerData("Player1", 100);
        int score = GetPlayerScore("Player1");
        Debug.Log("Score carregado do SQLite: " + score);
    }

    // 1. Cria a tabela se ela não existir
    public void CreateSchema()
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE IF NOT EXISTS Players (\r\n    id INTEGER PRIMARY KEY AUTOINCREMENT, \r\n    name TEXT, \r\n    score INTEGER\r\n);";
                command.ExecuteNonQuery();
            }
        }
    }

    // 2. Método para Salvar Dados
    public void SavePlayerData(string name, int score)
    {
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT INTO Players (name, score) VALUES ('{name}', {score});";
                command.ExecuteNonQuery();
            }
        }
    }

    // 3. Método para Ler Dados
    public int GetPlayerScore(string name)
    {
        int score = 0;
        using (var connection = new SqliteConnection(dbPath))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT score FROM Players WHERE name = '{name}' LIMIT 1;";
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        score = reader.GetInt32(0);
                    }
                }
            }
        }
        return score;
    }
}
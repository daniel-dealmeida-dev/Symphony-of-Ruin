using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    private int monstrosMortos;
    private float tempoInicio;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        tempoInicio = Time.time;
    }

    public void RegistrarMorteMonstro()
    {
        monstrosMortos++;
    }

    public int GetScore()
    {
        int tempoVivo = Mathf.FloorToInt(Time.time - tempoInicio);

        return (monstrosMortos * 100) + tempoVivo;
    }

    public int GetMonstrosMortos()
    {
        return monstrosMortos;
    }

    public int GetTempoVivo()
    {
        return Mathf.FloorToInt(Time.time - tempoInicio);
    }
}
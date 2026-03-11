using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigScript : MonoBehaviour
{
    [SerializeField]
    private int vidas = 0;
    [SerializeField]
    private int volumeMusica = 100;
    [SerializeField]
    private string[] fasesConcluidas;


    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

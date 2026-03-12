using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuPause : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P))
        {
            PausarJogo();

        }
    }
        private void PausarJogo()
    { 

        //estiver funcionado -> pause 
        if (Time.timeScale == 1)
        {
            Time.timeScale = 0;

        }//pausado  -> funciona 
        else if (Time.timeScale == 0)
        {
            Time.timeScale = 1;
        }
    }
}

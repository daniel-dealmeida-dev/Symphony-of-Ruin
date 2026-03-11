using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class MovimentoJogador : MonoBehaviour
{

    public int velocidade = 10;
    private float moveX;
    private bool direita = true;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        moveJogador();
    }

    private void LateUpdate()
    {
        viraJogador();
    }

    void moveJogador()
    {
        moveX = Input.GetAxis("Horizontal");
        gameObject.GetComponent<Rigidbody2D>().velocity = new Vector2(moveX * velocidade, gameObject.GetComponent<Rigidbody2D>().velocity.y);
    }
    void viraJogador()
    {
        if (moveX > 0)
        {
            direita = true;
        }
        else if (moveX < 0)
        {
            direita = false;
        }

        Vector2 escala = transform.localScale;
        if ((escala.x > 0 && !direita) || (escala.x < 0) && direita)
        {
            escala.x = escala.x * -1;
            transform.localScale = escala;


        }
    }
}

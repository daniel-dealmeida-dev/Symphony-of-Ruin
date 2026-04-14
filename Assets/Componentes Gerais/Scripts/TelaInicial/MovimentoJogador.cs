using UnityEngine;

public class MovimentoJogador : MonoBehaviour
{
    [SerializeField] private float velocidade = 10f;
    [SerializeField] private Rigidbody2D corpoRigido;

    private float moveX;
    private bool direita = true;

    private void Awake()
    {
        GameServices.EnsureInstance();
        if (GameManager.gm == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }

        if (corpoRigido == null)
        {
            corpoRigido = GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        MoveJogador();
    }

    private void LateUpdate()
    {
        ViraJogador();
    }

    private void MoveJogador()
    {
        moveX = GameServices.Instance.Settings.GetHorizontal();
        if (corpoRigido != null)
        {
            corpoRigido.velocity = new Vector2(moveX * velocidade, corpoRigido.velocity.y);
        }
    }

    private void ViraJogador()
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
        if ((escala.x > 0 && !direita) || (escala.x < 0 && direita))
        {
            escala.x *= -1;
            transform.localScale = escala;
        }
    }
}

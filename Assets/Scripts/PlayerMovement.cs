using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Referências")]
    public FixedJoystick joystick;
    public Rigidbody2D corpo;

    [Header("Movimento")]
    public float velocidade = 10f;
    public float deadZone = 0.1f;

    private float moveX;

    void Start()
    {
        if (corpo == null)
            corpo = GetComponent<Rigidbody2D>();

        if (joystick == null)
            joystick = FindFirstObjectByType<FixedJoystick>();
    }

    public void ProcessarMovimento()
    {
        moveX = 0f;

        // MOBILE
        if (joystick != null)
        {
            moveX = joystick.Horizontal;

            if (Mathf.Abs(moveX) < deadZone)
                moveX = 0f;
        }

        // PC fallback
        if (Mathf.Abs(moveX) < 0.01f)
        {
            moveX = Input.GetAxisRaw("Horizontal");
        }
    }

    public void AplicarMovimento()
    {
        if (corpo == null) return;

        corpo.velocity = new Vector2(
            moveX * velocidade,
            corpo.velocity.y
        );
    }

    public float GetMoveX()
    {
        return moveX;
    }
}
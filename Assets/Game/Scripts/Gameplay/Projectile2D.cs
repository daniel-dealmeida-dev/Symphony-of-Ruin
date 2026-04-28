using UnityEngine;

[DisallowMultipleComponent]
public class Projectile2D : MonoBehaviour
{
    [SerializeField] private float speed = 14f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifetime = 4f;

    private Vector2 direction = Vector2.right;

    public int Damage
    {
        get { return damage; }
    }

    public void Launch(Vector2 moveDirection, float projectileSpeed, int projectileDamage)
    {
        direction = moveDirection.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
    }

    private void Awake()
    {
        gameObject.tag = "Projetil";
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<EnemyHealth>() != null)
        {
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger && other.GetComponent<PlayerHealth>() == null)
        {
            Destroy(gameObject);
        }
    }
}

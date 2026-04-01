using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float changeDirectionInterval = 2f;  // 방향 바꾸는 주기

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private float timer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        PickRandomDirection();
    }

    void FixedUpdate()
    {
        timer -= Time.fixedDeltaTime;

        if (timer <= 0f)
        {
            PickRandomDirection();
        }

        rb.linearVelocity = moveDirection * moveSpeed;
    }

    void PickRandomDirection()
    {
        float angle = Random.Range(0f, 360f);
        moveDirection = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );
        timer = changeDirectionInterval;
    }

    // 벽에 부딪히면 반대 방향으로
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            PickRandomDirection();
            timer = changeDirectionInterval;
        }
    }
}
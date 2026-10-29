using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float deadY = -4.1f;

    private SpawnEnemy waveManager;
    private bool isDead = false;

    public void Initialize(float moveSpeed, SpawnEnemy manager)
    {
        speed = moveSpeed;
        waveManager = manager;
        isDead = false;
    }

    private void Awake()
    {
        if (waveManager == null)
        {
            waveManager = FindAnyObjectByType<SpawnEnemy>();
        }

        if (GetComponent<Collider2D>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        if (!gameObject.CompareTag("Zombie"))
        {
            gameObject.tag = "Zombie";
        }
    }

    private void Update()
    {
        if (isDead) return;

        transform.position += Vector3.down * (speed * Time.deltaTime);

        // If passes the wall line, trigger Game Over
        if (transform.position.y <= deadY)
        {
            HitWall();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        if (collision.CompareTag("DeadWall") || collision.gameObject.name.Contains("Wall"))
        {
            HitWall();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.collider.CompareTag("DeadWall") || collision.gameObject.name.Contains("Wall"))
        {
            HitWall();
        }
    }

    public void TakeHit()
    {
        if (isDead) return;
        isDead = true;

        if (waveManager != null)
        {
            waveManager.OnEnemyDefeated(this);
        }

        Destroy(gameObject);
    }

    private void HitWall()
    {
        if (isDead) return;
        isDead = true;

        if (waveManager != null)
        {
            waveManager.OnGameOver();
        }

        Destroy(gameObject);
    }
}

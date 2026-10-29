using UnityEngine;

public sealed class Bullet : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.25f;
    [SerializeField] private LayerMask targetLayer = ~0;

    private Vector3 velocity;
    private float remainingLifetime;
    private bool hasHit = false;

    private void Awake()
    {
        // Ensure collider & kinematic rigidbody are present for physics callbacks
        if (GetComponent<Collider2D>() == null)
        {
            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 4f; // Suitable for local scale ~0.025
        }

        if (GetComponent<Rigidbody2D>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    public void Launch(Vector2 direction, float speed, float lifetime)
    {
        direction.Normalize();
        velocity = direction * Mathf.Max(0.01f, speed);
        remainingLifetime = Mathf.Max(0.01f, lifetime);
        hasHit = false;

        // The supplied bullet sprite points along +X.
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        // Instantiate preserves the prefab's active state. Activate only after initialization.
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (hasHit) return;

        Vector3 step = velocity * Time.deltaTime;
        float dist = step.magnitude;

        // Sweep / CircleCast along velocity vector each frame to prevent fast-moving tunneling
        if (dist > 0.0001f)
        {
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, hitRadius, step.normalized, dist, targetLayer);
            if (hit.collider != null && CheckHit(hit.collider))
            {
                return;
            }
        }

        // Direct overlap check at current position as safety
        Collider2D overlap = Physics2D.OverlapCircle(transform.position, hitRadius, targetLayer);
        if (overlap != null && CheckHit(overlap))
        {
            return;
        }

        transform.position += step;
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckHit(collision.collider);
    }

    private bool CheckHit(Collider2D col)
    {
        if (hasHit || col == null) return false;

        // Ignore self or another bullet
        if (col.gameObject == gameObject || col.GetComponent<Bullet>() != null) return false;

        // 1. Check Upgrade Item
        UpgradeItem upgrade = col.GetComponent<UpgradeItem>();
        if (upgrade == null)
        {
            upgrade = col.GetComponentInParent<UpgradeItem>();
        }

        if (upgrade != null)
        {
            hasHit = true;
            upgrade.Collect();
            Destroy(gameObject);
            return true;
        }

        // 2. Check Enemy
        Enemy enemy = col.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = col.GetComponentInParent<Enemy>();
        }

        if (enemy != null || col.CompareTag("Zombie"))
        {
            hasHit = true;

            if (enemy != null)
            {
                enemy.TakeHit();
            }
            else
            {
                Destroy(col.gameObject);
            }

            Destroy(gameObject);
            return true;
        }

        return false;
    }
}

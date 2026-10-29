using UnityEngine;

public class UpgradeItem : MonoBehaviour
{
    [SerializeField] private int bulletLevel = 2; // 2 (+2 bullets) or 3 (+3 bullets)
    [SerializeField] private float fallSpeed = 1.2f;
    [SerializeField] private float duration = 10f;
    [SerializeField] private float deadY = -4.5f;

    private bool isCollected = false;

    public void Initialize(int level, float speed, float buffDuration = 10f)
    {
        bulletLevel = level;
        fallSpeed = speed;
        duration = buffDuration;
        isCollected = false;
    }

    private void Awake()
    {
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.5f;
        }

        if (GetComponent<Rigidbody2D>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void Update()
    {
        if (isCollected) return;

        // Fall downwards
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);

        // Gentle floating wobble for better visibility
        transform.position += Vector3.right * (Mathf.Sin(Time.time * 3f) * 0.35f * Time.deltaTime);

        if (transform.position.y <= deadY)
        {
            Destroy(gameObject);
        }
    }

    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        GunController gun = FindAnyObjectByType<GunController>();
        if (gun != null)
        {
            gun.UpgradeGun(bulletLevel, duration);
        }

        Destroy(gameObject);
    }
}


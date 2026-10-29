using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GunController : MonoBehaviour
{
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Transform gunPivot;
    [SerializeField] private Transform firePos;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField, Range(0f, 90f)] private float maxAngle = 90f;
    [SerializeField, Min(0.01f)] private float fireInterval = 0.3f;
    [SerializeField, Min(0.01f)] private float bulletSpeed = 8f;
    [SerializeField, Min(0.01f)] private float bulletLifetime = 4f;

    [Header("Upgrade Settings")]
    [SerializeField] private float spreadAnglePerBullet = 9f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioSource audioSource;

    private int currentBulletLevel = 1; // 1: single, 2: double, 3: triple
    private float buffRemainingTime = 0f;
    private float nextFireTime;

    public int CurrentBulletLevel => currentBulletLevel;
    public float BuffRemainingTime => buffRemainingTime;

    private void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        if (aimCamera == null || gunPivot == null || firePos == null || bulletPrefab == null)
        {
            Debug.LogError("GunController: assign Camera, Gun Pivot, Fire Pos and Bullet Prefab.", this);
            enabled = false;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D Sound

        if (shootSound == null)
        {
            shootSound = Resources.Load<AudioClip>("laserShoot");
        }
    }

    private void OnEnable()
    {
        nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval);
    }

    private void Update()
    {
        // Don't aim or shoot if game is paused / game over / won
        if (Time.timeScale <= 0f) return;

        AimAtMouse();

        // Count down buff duration
        if (currentBulletLevel > 1)
        {
            buffRemainingTime -= Time.deltaTime;
            if (buffRemainingTime <= 0f)
            {
                currentBulletLevel = 1;
                buffRemainingTime = 0f;
            }
        }

        if (Time.time < nextFireTime) return;

        FireBullets();
        nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval);
    }

    private void FireBullets()
    {
        if (currentBulletLevel <= 1)
        {
            FireSingleBullet(firePos.up);
        }
        else
        {
            float totalSpread = (currentBulletLevel - 1) * spreadAnglePerBullet;
            float startAngle = -totalSpread * 0.5f;

            for (int i = 0; i < currentBulletLevel; i++)
            {
                float angle = startAngle + (i * spreadAnglePerBullet);
                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * firePos.up;
                FireSingleBullet(dir);
            }
        }

        // Play shoot audio on each shot burst
        PlayShootAudio();
    }

    private void FireSingleBullet(Vector2 dir)
    {
        Bullet bullet = Instantiate(bulletPrefab, firePos.position, Quaternion.identity);
        bullet.Launch(dir, bulletSpeed, bulletLifetime);
    }

    private void PlayShootAudio()
    {
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    public void UpgradeGun(int targetLevel, float duration = 10f)
    {
        if (targetLevel > currentBulletLevel)
        {
            currentBulletLevel = targetLevel;
            buffRemainingTime = duration;
        }
        else if (targetLevel == currentBulletLevel)
        {
            buffRemainingTime = duration;
        }
        else
        {
            buffRemainingTime = duration;
        }
    }

    public void ResetGun()
    {
        currentBulletLevel = 1;
        buffRemainingTime = 0f;
        nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval);
    }

    private void AimAtMouse()
    {
        if (Mouse.current == null) return;
        Ray ray = aimCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane gamePlane = new Plane(Vector3.forward, gunPivot.position);
        if (!gamePlane.Raycast(ray, out float distance)) return;

        Vector2 direction = ray.GetPoint(distance) - gunPivot.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(-direction.x, direction.y) * Mathf.Rad2Deg;
        float limit = Mathf.Clamp(maxAngle, 0f, 90f);
        gunPivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(angle, -limit, limit));
    }

    private void OnGUI()
    {
        if (Time.timeScale <= 0f) return;

        // Display active buff HUD if multi-bullet is active
        if (currentBulletLevel > 1 && buffRemainingTime > 0f)
        {
            GUIStyle buffStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buffStyle.normal.textColor = currentBulletLevel >= 3 ? new Color(1f, 0.4f, 1f) : new Color(0.2f, 1f, 1f);

            string buffText = $"BUFF: {currentBulletLevel}X SHOT ({buffRemainingTime:F1}s)";
            GUI.Box(new Rect(20, 55, 230, 32), buffText, buffStyle);
        }
    }
}

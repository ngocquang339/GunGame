using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SpawnEnemy : MonoBehaviour
{
    [System.Serializable]
    public class WaveConfig
    {
        public string waveName = "Wave 1";
        [Min(1)] public int enemyCount = 5;
        [Min(0.1f)] public float spawnInterval = 1.8f;
        [Min(0.1f)] public float enemySpeed = 1.2f;
        [Min(0.5f)] public float delayBeforeWave = 2.0f;
    }

    [Header("Wave Configuration (4 Waves)")]
    [SerializeField] private WaveConfig[] waves = new WaveConfig[]
    {
        new WaveConfig { waveName = "Wave 1: First Encounter", enemyCount = 5,  spawnInterval = 1.8f, enemySpeed = 1.2f, delayBeforeWave = 2.0f },
        new WaveConfig { waveName = "Wave 2: Rising Threat",   enemyCount = 8,  spawnInterval = 1.4f, enemySpeed = 1.6f, delayBeforeWave = 2.5f },
        new WaveConfig { waveName = "Wave 3: Zombie Swarm",    enemyCount = 12, spawnInterval = 1.0f, enemySpeed = 2.0f, delayBeforeWave = 2.5f },
        new WaveConfig { waveName = "Wave 4: Final Assault",   enemyCount = 16, spawnInterval = 0.75f, enemySpeed = 2.5f, delayBeforeWave = 3.0f }
    };

    [Header("References")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private TextMeshProUGUI zombieCountText;
    [SerializeField] private Camera aimCamera;

    [Header("Background Music")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f;

    [Header("Upgrade Items (Drop from above)")]
    [SerializeField] private GameObject doubleBulletItemPrefab;
    [SerializeField] private GameObject tripleBulletItemPrefab;
    [SerializeField] private float minItemInterval = 7f;
    [SerializeField] private float maxItemInterval = 13f;

    [Header("UI Panels (Optional Canvas Links)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winGamePanel;
    [SerializeField] private TextMeshProUGUI gameOverKillsText;
    [SerializeField] private TextMeshProUGUI winGameKillsText;
    [SerializeField] private UnityEngine.UI.Button gameOverRestartBtn;
    [SerializeField] private UnityEngine.UI.Button winGameRestartBtn;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnY = 5.6f;
    [SerializeField] private float defaultMinX = -3.5f;
    [SerializeField] private float defaultMaxX = 3.5f;

    // Runtime state
    private int currentWaveIndex = 0;
    private int enemiesSpawnedThisWave = 0;
    private int aliveEnemiesThisWave = 0;
    private bool isWaveActive = false;
    private bool isGameOver = false;
    private bool isWinGame = false;
    private float nextSpawnTime = 0f;
    private float waveCountdown = 0f;
    private string statusBannerText = "";
    private float bannerDisplayTimer = 0f;
    private int totalKills = 0;
    private float nextItemSpawnTime = 0f;

    public static SpawnEnemy Instance { get; private set; }
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => waves != null ? waves.Length : 0;
    public bool IsGameOver => isGameOver;
    public bool IsWinGame => isWinGame;
    public int TotalKills => totalKills;

    private void Awake()
    {
        Instance = this;

        if (aimCamera == null) aimCamera = Camera.main;

        // Auto-detect UI text if not assigned
        if (zombieCountText == null)
        {
            var allTMP = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var t in allTMP)
            {
                if (t.gameObject.name.Contains("ZombieCount") || t.text.StartsWith("x"))
                {
                    zombieCountText = t;
                    break;
                }
            }
        }

        // Background Music setup
        if (bgmAudioSource == null)
        {
            bgmAudioSource = GetComponent<AudioSource>();
            if (bgmAudioSource == null)
            {
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (bgmClip == null)
        {
            bgmClip = Resources.Load<AudioClip>("bgm");
        }

        if (bgmAudioSource != null)
        {
            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
            bgmAudioSource.spatialBlend = 0f; // 2D Sound
            bgmAudioSource.volume = bgmVolume;
            if (bgmClip != null)
            {
                bgmAudioSource.clip = bgmClip;
            }
        }

        // If enemyPrefab is not assigned, look for an existing Zombie in the scene to use as template
        if (enemyPrefab == null)
        {
            var sceneZombie = GameObject.FindWithTag("Zombie");
            if (sceneZombie != null)
            {
                if (sceneZombie.GetComponent<Enemy>() == null)
                {
                    sceneZombie.AddComponent<Enemy>();
                }
                enemyPrefab = sceneZombie;
                sceneZombie.SetActive(false);
            }
        }

        if (gameOverRestartBtn != null) gameOverRestartBtn.onClick.AddListener(RestartGame);
        if (winGameRestartBtn != null) winGameRestartBtn.onClick.AddListener(RestartGame);
    }

    private void Start()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("SpawnEnemy: No waves configured!");
            return;
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winGamePanel != null) winGamePanel.SetActive(false);

        Time.timeScale = 1f;
        totalKills = 0;
        UpdateUI();
        PlayBGM();
        StartWave(0);
    }

    private void Update()
    {
        // Allow restarting with R key when game over or won
        if (isGameOver || isWinGame)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartGame();
            }
            return;
        }

        if (bannerDisplayTimer > 0f)
        {
            bannerDisplayTimer -= Time.deltaTime;
        }

        // Countdown before wave begins
        if (!isWaveActive)
        {
            waveCountdown -= Time.deltaTime;
            if (waveCountdown <= 0f)
            {
                isWaveActive = true;
                nextSpawnTime = Time.time;
                nextItemSpawnTime = Time.time + Random.Range(minItemInterval, maxItemInterval);
                ShowBanner($"{waves[currentWaveIndex].waveName}", 2.0f);
            }
            return;
        }

        // Spawning Upgrade Items randomly during active wave
        if (Time.time >= nextItemSpawnTime)
        {
            SpawnRandomUpgradeItem();
            nextItemSpawnTime = Time.time + Random.Range(minItemInterval, maxItemInterval);
        }

        // Spawning enemies in active wave
        WaveConfig currentWave = waves[currentWaveIndex];
        if (enemiesSpawnedThisWave < currentWave.enemyCount)
        {
            if (Time.time >= nextSpawnTime)
            {
                SpawnSingleEnemy(currentWave);
                enemiesSpawnedThisWave++;
                aliveEnemiesThisWave++;
                nextSpawnTime = Time.time + currentWave.spawnInterval;
            }
        }
        else
        {
            // All spawned, check if all alive enemies of this wave are defeated
            if (aliveEnemiesThisWave <= 0)
            {
                OnWaveCompleted();
            }
        }
    }

    private void StartWave(int waveIndex)
    {
        currentWaveIndex = waveIndex;
        enemiesSpawnedThisWave = 0;
        aliveEnemiesThisWave = 0;
        isWaveActive = false;

        WaveConfig config = waves[currentWaveIndex];
        waveCountdown = config.delayBeforeWave;

        ShowBanner($"PREPARE: {config.waveName}", config.delayBeforeWave);
    }

    private void SpawnSingleEnemy(WaveConfig config)
    {
        if (enemyPrefab == null) return;

        float minX = defaultMinX;
        float maxX = defaultMaxX;

        if (aimCamera != null)
        {
            Vector3 leftEdge = aimCamera.ViewportToWorldPoint(new Vector3(0.08f, 1f, 0f));
            Vector3 rightEdge = aimCamera.ViewportToWorldPoint(new Vector3(0.92f, 1f, 0f));
            minX = leftEdge.x;
            maxX = rightEdge.x;
        }

        float spawnX = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

        GameObject newEnemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        newEnemyObj.name = $"Enemy_W{currentWaveIndex + 1}_{enemiesSpawnedThisWave + 1}";
        newEnemyObj.SetActive(true);

        Enemy enemyComponent = newEnemyObj.GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            enemyComponent = newEnemyObj.AddComponent<Enemy>();
        }

        enemyComponent.Initialize(config.enemySpeed, this);
    }

    private void SpawnRandomUpgradeItem()
    {
        bool spawnTriple = Random.value > 0.55f;
        GameObject targetPrefab = spawnTriple ? tripleBulletItemPrefab : doubleBulletItemPrefab;

        if (targetPrefab == null)
        {
            targetPrefab = doubleBulletItemPrefab != null ? doubleBulletItemPrefab : tripleBulletItemPrefab;
        }

        if (targetPrefab == null) return;

        float minX = defaultMinX;
        float maxX = defaultMaxX;

        if (aimCamera != null)
        {
            Vector3 leftEdge = aimCamera.ViewportToWorldPoint(new Vector3(0.12f, 1f, 0f));
            Vector3 rightEdge = aimCamera.ViewportToWorldPoint(new Vector3(0.88f, 1f, 0f));
            minX = leftEdge.x;
            maxX = rightEdge.x;
        }

        float spawnX = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

        GameObject itemObj = Instantiate(targetPrefab, spawnPos, Quaternion.identity);
        itemObj.SetActive(true);
    }

    public void OnEnemyDefeated(Enemy enemy)
    {
        aliveEnemiesThisWave = Mathf.Max(0, aliveEnemiesThisWave - 1);
        totalKills++;
        UpdateUI();
    }

    public void OnGameOver()
    {
        if (isGameOver || isWinGame) return;

        isGameOver = true;
        isWaveActive = false;
        Time.timeScale = 0f;
        PauseBGM();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverKillsText != null)
            {
                gameOverKillsText.text = $"Zombie đã tiêu diệt: {totalKills}";
            }
        }
    }

    private void OnWaveCompleted()
    {
        isWaveActive = false;

        if (currentWaveIndex + 1 < waves.Length)
        {
            ShowBanner($"{waves[currentWaveIndex].waveName} CLEARED!", 2.0f);
            StartWave(currentWaveIndex + 1);
        }
        else
        {
            // All 4 waves defeated: WIN GAME!
            isWinGame = true;
            Time.timeScale = 0f;
            PauseBGM();

            if (winGamePanel != null)
            {
                winGamePanel.SetActive(true);
                if (winGameKillsText != null)
                {
                    winGameKillsText.text = $"Tổng số zombie đã tiêu diệt: {totalKills}";
                }
            }
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        isWinGame = false;
        totalKills = 0;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winGamePanel != null) winGamePanel.SetActive(false);

        // Clean up remaining enemies
        Enemy[] remainingEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in remainingEnemies)
        {
            if (e.gameObject != enemyPrefab)
            {
                Destroy(e.gameObject);
            }
        }

        // Clean up remaining bullets
        Bullet[] remainingBullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);
        foreach (var b in remainingBullets)
        {
            Destroy(b.gameObject);
        }

        // Clean up remaining items
        UpgradeItem[] remainingItems = FindObjectsByType<UpgradeItem>(FindObjectsSortMode.None);
        foreach (var it in remainingItems)
        {
            if (it.gameObject != doubleBulletItemPrefab && it.gameObject != tripleBulletItemPrefab)
            {
                Destroy(it.gameObject);
            }
        }

        // Reset cannon
        GunController gun = FindAnyObjectByType<GunController>();
        if (gun != null)
        {
            gun.ResetGun();
        }

        UpdateUI();
        PlayBGM();
        StartWave(0);
    }

    public void PlayBGM()
    {
        if (bgmAudioSource != null)
        {
            if (bgmClip != null) bgmAudioSource.clip = bgmClip;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bgmVolume;
            if (!bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Play();
            }
        }
    }

    public void PauseBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Pause();
        }
    }

    private void UpdateUI()
    {
        if (zombieCountText != null)
        {
            zombieCountText.text = $"x{totalKills}";
        }
    }

    private void ShowBanner(string text, float duration)
    {
        statusBannerText = text;
        bannerDisplayTimer = duration;
    }

    private void OnGUI()
    {
        if (isGameOver)
        {
            DrawGameOverWindow();
            return;
        }

        if (isWinGame)
        {
            DrawWinGameWindow();
            return;
        }

        DrawGameplayHUD();
    }

    private void DrawGameplayHUD()
    {
        // Top-left wave counter HUD
        GUIStyle hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold
        };
        hudStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(20, 20, 300, 35), $"WAVE: {currentWaveIndex + 1} / {waves.Length}", hudStyle);

        // Center announcement banner during gameplay
        if (bannerDisplayTimer > 0f && !string.IsNullOrEmpty(statusBannerText))
        {
            GUIStyle bannerStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            bannerStyle.normal.textColor = Color.yellow;

            float bannerWidth = 460f;
            float bannerHeight = 65f;
            float x = (Screen.width - bannerWidth) * 0.5f;
            float y = Screen.height * 0.22f;

            GUI.Box(new Rect(x, y, bannerWidth, bannerHeight), statusBannerText, bannerStyle);
        }
    }

    private void DrawGameOverWindow()
    {
        // Dark semi-transparent background overlay
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = 480f;
        float height = 320f;
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;

        // Popup background
        GUI.Box(new Rect(x, y, width, height), GUIContent.none);

        // Title: GAME OVER
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.25f, 0.25f);
        GUI.Label(new Rect(x, y + 25, width, 55), "GAME OVER", titleStyle);

        // Subtitle
        GUIStyle msgStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };
        msgStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y + 85, width, 30), "Zombie đã va chạm vào tường phòng thủ!", msgStyle);

        // Stats
        GUIStyle statStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        statStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(x, y + 120, width, 30), $"Zombie đã tiêu diệt: {totalKills}", statStyle);
        GUI.Label(new Rect(x, y + 150, width, 30), $"Dừng lại tại: Wave {currentWaveIndex + 1} / {waves.Length}", statStyle);

        // Restart button
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        btnStyle.normal.textColor = Color.white;

        float btnW = 240f;
        float btnH = 55f;
        float btnX = x + (width - btnW) * 0.5f;
        float btnY = y + 205f;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "CHƠI LẠI", btnStyle))
        {
            RestartGame();
        }

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        hintStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(x, y + 270, width, 25), "(hoặc nhấn phím 'R')", hintStyle);
    }

    private void DrawWinGameWindow()
    {
        // Dark semi-transparent background overlay
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float width = 480f;
        float height = 320f;
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;

        // Popup background
        GUI.Box(new Rect(x, y, width, height), GUIContent.none);

        // Title: CHIẾN THẮNG!
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(0.2f, 1f, 0.4f);
        GUI.Label(new Rect(x, y + 25, width, 55), "CHIẾN THẮNG!", titleStyle);

        // Subtitle
        GUIStyle msgStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };
        msgStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y + 85, width, 30), "Bạn đã bắn hạ toàn bộ 4 Wave Zombie!", msgStyle);

        // Stats
        GUIStyle statStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        statStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(x, y + 130, width, 35), $"Tổng số Zombie tiêu diệt: {totalKills}", statStyle);

        // Restart button
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        btnStyle.normal.textColor = Color.white;

        float btnW = 240f;
        float btnH = 55f;
        float btnX = x + (width - btnW) * 0.5f;
        float btnY = y + 205f;

        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), "CHƠI LẠI", btnStyle))
        {
            RestartGame();
        }

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        hintStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(x, y + 270, width, 25), "(hoặc nhấn phím 'R')", hintStyle);
    }
}

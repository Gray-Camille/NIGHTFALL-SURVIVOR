using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public float spawnInterval = 2f;
    public int enemiesPerWave = 5;
    private const int EliteStartWave = 6;
    private const int BossStartWave = 15;
    private const int BossWaveInterval = 15;
    private float _spawnTimer;
    private int _spawnedThisWave;
    private int _wave = 1;
    private int _nextEliteWave;

    void Start()
    {
        _spawnTimer = 1f;
        _spawnedThisWave = 0;
        _nextEliteWave = EliteStartWave + (Random.value < 0.5f ? 3 : 5);
        UIManager.Instance?.UpdateWaveUI(_wave);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0)
        {
            GameManager.Instance.SpawnEnemy(_wave);
            _spawnedThisWave++;
            _spawnTimer = spawnInterval;
            if (_spawnedThisWave >= enemiesPerWave)
            {
                _wave++; _spawnedThisWave = 0;
                TrySpawnSpecialEnemies();
                enemiesPerWave += 1;
                spawnInterval = Mathf.Max(0.35f, spawnInterval * 0.95f);
                MapManager.Instance?.UpdateForWave(_wave);
                UIManager.Instance?.UpdateWaveUI(_wave);
            }
        }
    }

    public int GetWave() => _wave;

    private void TrySpawnSpecialEnemies()
    {
        if (_wave >= EliteStartWave && _wave == _nextEliteWave)
        {
            GameManager.Instance.SpawnElite(_wave);
            _nextEliteWave = _wave + (Random.value < 0.5f ? 3 : 5);
        }

        if (_wave >= BossStartWave && (_wave - BossStartWave) % BossWaveInterval == 0)
        {
            GameManager.Instance.SpawnBoss(_wave);
        }
    }
}

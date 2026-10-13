using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyType
    {
        Slime, Bat, Brute, Shooter, Skeleton, Crawler,
        EliteKnight, EliteWarlock, StoneGolem, ShadowAssassin,
        SlimeKing, DemonOverlord
    }

    public enum EnemyCategory { Normal, Elite, Boss }

    public EnemyType type = EnemyType.Slime;
    public EnemyCategory category = EnemyCategory.Normal;
    public int maxHealth = 30;
    public int currentHealth;
    public float moveSpeed = 2.5f;
    public int contactDamage = 10;
    public int armor = 0;
    public float attackCooldown = 1.6f;
    public int xpReward = 5;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private float _damageCooldown;
    private const float DamageInterval = 0.5f;
    private float _flashTimer;
    private float _attackTimer;
    private float _specialTimer;
    private bool _specialPattern;
    private float _dashTimer;
    private Vector2 _dashDirection;
    private float _dashSpeed;
    private Transform _healthBarFill;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        _sr.sprite = PixelSpriteGenerator.GetEnemySprite(type);
        _sr.sortingOrder = 5;
        float scale = category == EnemyCategory.Boss ? 4f : category == EnemyCategory.Elite ? 3f : 2f;
        transform.localScale = Vector3.one * scale;
        currentHealth = maxHealth;
        _specialTimer = Random.Range(1.5f, 3f);
        if (category != EnemyCategory.Normal) CreateHealthBar();
        UpdateHealthBar();
    }

    void Update()
    {
        if (_flashTimer > 0) _flashTimer -= Time.deltaTime;
        if (_damageCooldown > 0) _damageCooldown -= Time.deltaTime;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver &&
            (UIManager.Instance == null || !UIManager.Instance.IsModalOpen))
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                if (IsRanged())
                {
                    Vector2 toPlayer = player.transform.position - transform.position;
                    float distance = toPlayer.magnitude;
                    Vector2 dir = toPlayer.normalized;
                    float desiredDistance = 7f;
                    _rb.velocity = distance > desiredDistance + 0.5f ? dir * moveSpeed :
                        distance < desiredDistance - 0.5f ? -dir * moveSpeed * 0.6f : Vector2.zero;
                    _attackTimer -= Time.deltaTime;
                    if (_attackTimer <= 0 && distance <= 11f)
                    {
                        _attackTimer = attackCooldown;
                        GameManager.Instance.SpawnEnemyProjectile(transform.position, player.transform.position, contactDamage);
                    }
                }
                else
                {
                    Vector2 dir = (player.transform.position - transform.position).normalized;
                    if (type == EnemyType.Bat)
                    {
                        Vector2 sway = new Vector2(Mathf.Sin(Time.time * 3.2f), Mathf.Cos(Time.time * 4.1f)) * 0.7f;
                        _rb.velocity = (dir + sway).normalized * moveSpeed;
                    }
                    else
                    {
                        _rb.velocity = dir * moveSpeed;
                    }
                }
                if (_sr.sprite != null) _sr.flipX = (player.transform.position - transform.position).x < 0;

                if (_dashTimer > 0)
                {
                    _dashTimer -= Time.deltaTime;
                    _rb.velocity = _dashDirection * _dashSpeed;
                }

                _specialTimer -= Time.deltaTime;
                if (_specialTimer <= 0)
                {
                    _specialTimer = GetSpecialAttackInterval();
                    PerformSpecialAttack(player);
                }
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= Mathf.Max(1, damage - armor);
        _flashTimer = 0.1f;
        _sr.color = Color.red;
        UpdateHealthBar();
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyDied(this);
        }
        Destroy(gameObject);
    }

    public bool IsRanged() => type == EnemyType.Shooter || type == EnemyType.Skeleton || type == EnemyType.Crawler ||
                              type == EnemyType.EliteWarlock || type == EnemyType.DemonOverlord;

    private float GetSpecialAttackInterval()
    {
        switch (type)
        {
            case EnemyType.Shooter: return 2.6f;
            case EnemyType.Skeleton: return 2.8f;
            case EnemyType.Crawler: return 3f;
            case EnemyType.Brute: return 4f;
            case EnemyType.EliteKnight: return 2.9f;
            case EnemyType.EliteWarlock: return 2.2f;
            case EnemyType.StoneGolem: return 3.1f;
            case EnemyType.ShadowAssassin: return 2.4f;
            case EnemyType.SlimeKing: return 2.5f;
            case EnemyType.DemonOverlord: return 2.1f;
            default: return 3.5f;
        }
    }

    private void PerformSpecialAttack(PlayerController player)
    {
        Vector2 toPlayer = player.transform.position - transform.position;
        switch (type)
        {
            case EnemyType.Shooter:
                GameManager.Instance.SpawnEnemyProjectile(transform.position, player.transform.position, contactDamage);
                break;
            case EnemyType.Skeleton:
                GameManager.Instance.SpawnEnemyProjectile(transform.position, player.transform.position, contactDamage);
                break;
            case EnemyType.Crawler:
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 direction = Rotate(toPlayer, i * 12f);
                    GameManager.Instance.SpawnRadialEnemyProjectile(transform.position, direction, contactDamage, 6.5f);
                }
                break;
            case EnemyType.Brute:
                SpawnRadialBurst(6, 5.5f);
                break;
            case EnemyType.Slime:
            case EnemyType.EliteKnight:
            case EnemyType.SlimeKing:
                StartDash(toPlayer.normalized, 2.8f, 0.45f);
                break;
            case EnemyType.Bat:
            case EnemyType.ShadowAssassin:
                StartDash(toPlayer.normalized, 3.4f, 0.35f);
                break;
            case EnemyType.EliteWarlock:
                if (_specialPattern) SpawnRadialBurst(5, 6f);
                else SpawnFan(toPlayer, 3, 14f);
                break;
            case EnemyType.StoneGolem:
                SpawnRadialBurst(6, 5f);
                break;
            case EnemyType.DemonOverlord:
                if (_specialPattern) SpawnRadialBurst(8, 6.5f);
                else SpawnFan(toPlayer, 3, 12f);
                break;
        }

        _specialPattern = !_specialPattern;
    }

    private void StartDash(Vector2 direction, float speedMultiplier, float duration)
    {
        _dashDirection = direction;
        _dashSpeed = moveSpeed * speedMultiplier;
        _dashTimer = duration;
    }

    private void SpawnRadialBurst(int count, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            GameManager.Instance.SpawnRadialEnemyProjectile(transform.position, direction, contactDamage, speed);
        }
    }

    private void SpawnFan(Vector2 toPlayer, int count, float spread)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = Rotate(toPlayer, (i - (count - 1) / 2f) * spread);
            GameManager.Instance.SpawnRadialEnemyProjectile(transform.position, direction, contactDamage, 7f);
        }
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians); float sin = Mathf.Sin(radians);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
    }

    private void CreateHealthBar()
    {
        float enemyScale = transform.localScale.x;
        var root = new GameObject("HealthBarRoot");
        root.transform.SetParent(transform, false);
        root.transform.localScale = Vector3.one / enemyScale;
        root.transform.localPosition = new Vector3(0, category == EnemyCategory.Boss ? 1.35f : 1.1f, 0);

        var background = new GameObject("HealthBarBackground");
        background.transform.SetParent(root.transform, false);
        var backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = PixelSpriteGenerator.GetSprite("hp_bar");
        backgroundRenderer.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        backgroundRenderer.sortingOrder = 6;
        background.transform.localScale = new Vector3(category == EnemyCategory.Boss ? 2.8f : 1.9f, 0.22f, 1f);

        var fill = new GameObject("HealthBarFill");
        fill.transform.SetParent(background.transform, false);
        var fillRenderer = fill.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = PixelSpriteGenerator.GetSprite("hp_bar");
        fillRenderer.color = new Color(0.9f, 0.25f, 0.25f);
        fillRenderer.sortingOrder = 7;
        fill.transform.localPosition = Vector3.zero;
        _healthBarFill = fill.transform;
    }

    private void UpdateHealthBar()
    {
        if (_healthBarFill == null || maxHealth <= 0) return;
        float pct = Mathf.Clamp01((float)currentHealth / maxHealth);
        _healthBarFill.localScale = new Vector3(pct, 0.8f, 1f);
        _healthBarFill.localPosition = new Vector3(-0.5f + 0.5f * pct, 0f, 0f);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (_damageCooldown > 0) return;
        var player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(contactDamage);
            _damageCooldown = DamageInterval;
        }
    }
}

public class EnemyProjectile : MonoBehaviour
{
    public int damage = 6;
    public float speed = 7f;
    private Vector2 _direction;
    private Rigidbody2D _rb;

    public void Launch(Vector2 direction)
    {
        _direction = direction.normalized;
        _rb = GetComponent<Rigidbody2D>();
        _rb.velocity = _direction * speed;
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}


using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    public int damage = 10;
    private Transform _target;
    private Rigidbody2D _rb;
    private Vector2 _direction;
    private bool _homing;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite("projectile");
        sr.sortingOrder = 8;
        transform.localScale = Vector3.one * 1.5f;
        Destroy(gameObject, 3f);
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        _homing = true;
        _direction = target != null ? (target.position - transform.position).normalized : Vector2.up;
    }

    public void SetDirection(Vector2 direction)
    {
        _target = null;
        _homing = false;
        _direction = direction.normalized;
    }

    void Update()
    {
        if (_homing && _target != null)
        {
            _direction = (_target.position - transform.position).normalized;
        }
        if (_direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
        }
        _rb.velocity = _direction * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}

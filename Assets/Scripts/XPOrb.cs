using UnityEngine;

public class XPOrb : MonoBehaviour
{
    public int xpAmount = 5;
    public float collectSpeed = 6f;
    public float collectRange = 3f;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private float _timer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        _sr.sprite = PixelSpriteGenerator.GetSprite("xp_orb");
        _sr.sortingOrder = 7;
        transform.localScale = Vector3.one * 1.5f;
        Destroy(gameObject, 15f);
    }

    void Update()
    {
        _timer += Time.deltaTime;
        _sr.color = new Color(1, 1, 1, 0.6f + Mathf.Sin(_timer * 4f) * 0.4f);
        var player = PlayerController.Instance;
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist <= collectRange)
            {
                Vector2 dir = (player.transform.position - transform.position).normalized;
                _rb.velocity = dir * collectSpeed;
                if (dist < 0.5f)
                {
                    player.AddXP(xpAmount);
                    Destroy(gameObject);
                }
            }
            else _rb.velocity = Vector2.zero;
        }
    }
}
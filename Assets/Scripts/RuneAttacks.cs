using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 符文解锁的额外攻击方式：环绕刀刃、灼烧光环、连锁闪电。
/// 数值全部来自 PlayerController 上的符文叠加层数。
/// </summary>
public class RuneAttacks : MonoBehaviour
{
    private PlayerController _player;
    private readonly List<Transform> _blades = new List<Transform>();
    private SpriteRenderer _auraVisual;
    private float _bladeAngle;
    private float _bladeDamageTimer;
    private float _auraTimer;
    private float _boltTimer;

    private const float OrbitRadius = 2.8f;
    private const float BladeHitRadius = 0.95f;
    private const float AuraBaseRadius = 2.2f;
    private const float AuraRadiusPerLevel = 0.6f;
    private const float LightningRange = 7f;
    private const float LightningChainRange = 4.5f;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    // 父物体（玩家）带缩放，子物体需要反向补偿，保证世界尺寸与数值一致
    private float ParentScale
    {
        get { float s = transform.localScale.x; return Mathf.Approximately(s, 0f) ? 1f : s; }
    }

    void Update()
    {
        if (_player == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (UIManager.Instance != null && UIManager.Instance.IsModalOpen) return;

        float dt = Time.deltaTime;
        UpdateOrbit(dt);
        UpdateAura(dt);
        UpdateLightning(dt);
    }

    private void UpdateOrbit(float dt)
    {
        int want = _player.orbitCount;
        while (_blades.Count < want)
        {
            var blade = new GameObject("OrbitBlade");
            blade.transform.SetParent(transform, false);
            var sr = blade.AddComponent<SpriteRenderer>();
            sr.sprite = PixelSpriteGenerator.GetSprite("orbit_blade");
            sr.sortingOrder = 9;
            blade.transform.localScale = Vector3.one * (1.2f / ParentScale);
            _blades.Add(blade.transform);
        }
        while (_blades.Count > want)
        {
            int last = _blades.Count - 1;
            if (_blades[last] != null) Destroy(_blades[last].gameObject);
            _blades.RemoveAt(last);
        }
        if (_blades.Count == 0) return;

        _bladeAngle += dt * 200f;
        if (_bladeAngle > 360f) _bladeAngle -= 360f;
        for (int i = 0; i < _blades.Count; i++)
        {
            float angle = (_bladeAngle + i * (360f / _blades.Count)) * Mathf.Deg2Rad;
            _blades[i].localPosition = new Vector3(Mathf.Cos(angle) * OrbitRadius, Mathf.Sin(angle) * OrbitRadius, 0f) / ParentScale;
            _blades[i].localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
        }

        _bladeDamageTimer -= dt;
        if (_bladeDamageTimer > 0f) return;
        _bladeDamageTimer = 0.3f;
        int damage = Mathf.Max(1, Mathf.RoundToInt(8f * _player.damageMultiplier * (1f + _player.orbitDamageBonus)));
        var enemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            for (int i = 0; i < _blades.Count; i++)
            {
                if (_blades[i] == null) continue;
                if (Vector2.Distance(_blades[i].position, enemy.transform.position) <= BladeHitRadius)
                {
                    enemy.TakeDamage(damage);
                    break;
                }
            }
        }
    }

    private void UpdateAura(float dt)
    {
        if (_player.auraLevel <= 0)
        {
            if (_auraVisual != null) { Destroy(_auraVisual.gameObject); _auraVisual = null; }
            return;
        }

        float radius = AuraBaseRadius + _player.auraLevel * AuraRadiusPerLevel;
        if (_auraVisual == null)
        {
            var aura = new GameObject("AuraVisual");
            aura.transform.SetParent(transform, false);
            _auraVisual = aura.AddComponent<SpriteRenderer>();
            _auraVisual.sprite = PixelSpriteGenerator.GetSprite("aura_ring");
            _auraVisual.sortingOrder = 4;
        }
        _auraVisual.transform.localPosition = Vector3.zero;
        // aura_ring 精灵为 16x16，PPU 16 => 缩放 1 时为 1 世界单位直径
        _auraVisual.transform.localScale = Vector3.one * (radius * 2f / ParentScale);
        float pulse = 0.16f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f);
        _auraVisual.color = new Color(1f, 0.55f, 0.2f, pulse);

        _auraTimer -= dt;
        if (_auraTimer > 0f) return;
        _auraTimer = 0.5f;
        int damage = Mathf.Max(1, Mathf.RoundToInt((3f + _player.auraLevel * 2f) * _player.damageMultiplier * (1f + _player.auraDamageBonus)));
        foreach (var enemy in FindObjectsOfType<Enemy>())
        {
            if (enemy == null) continue;
            if (Vector2.Distance(transform.position, enemy.transform.position) <= radius)
                enemy.TakeDamage(damage);
        }
    }

    private void UpdateLightning(float dt)
    {
        if (_player.chainLevel <= 0) return;
        _boltTimer -= dt;
        if (_boltTimer > 0f) return;
        _boltTimer = 1.6f;

        Enemy current = FindNearestEnemy(transform.position, LightningRange, null);
        if (current == null) return;

        int damage = Mathf.Max(1, Mathf.RoundToInt(12f * _player.damageMultiplier * (1f + _player.chainDamageBonus)));
        var hit = new HashSet<Enemy>();
        Vector2 from = transform.position;
        for (int jump = 0; jump <= _player.chainLevel && current != null; jump++)
        {
            SpawnBolt(from, current.transform.position);
            current.TakeDamage(damage);
            hit.Add(current);
            from = current.transform.position;
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.8f));
            current = FindNearestEnemy(from, LightningChainRange, hit);
        }
    }

    private Enemy FindNearestEnemy(Vector2 from, float maxRange, HashSet<Enemy> exclude)
    {
        Enemy nearest = null;
        float best = maxRange;
        foreach (var enemy in FindObjectsOfType<Enemy>())
        {
            if (enemy == null) continue;
            if (exclude != null && exclude.Contains(enemy)) continue;
            float distance = Vector2.Distance(from, enemy.transform.position);
            if (distance < best) { best = distance; nearest = enemy; }
        }
        return nearest;
    }

    private void SpawnBolt(Vector2 from, Vector2 to)
    {
        var bolt = new GameObject("ChainBolt");
        var sr = bolt.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSpriteGenerator.GetSprite("hp_bar");
        sr.color = new Color(0.75f, 0.9f, 1f, 0.95f);
        sr.sortingOrder = 11;
        Vector2 delta = to - from;
        float length = delta.magnitude;
        bolt.transform.position = (from + to) * 0.5f;
        // hp_bar 精灵为 1x0.25 世界单位（16x4 像素 / PPU 16）
        bolt.transform.localScale = new Vector3(Mathf.Max(0.1f, length), 0.5f, 1f);
        bolt.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        Destroy(bolt, 0.12f);
    }
}

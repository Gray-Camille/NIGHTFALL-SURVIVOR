using System.Collections.Generic;
using UnityEngine;

public enum RuneId
{
    AttackPower,
    AttackRange,
    MoveSpeed,
    Defense,
    MaxHealth,
    Multishot,
    Orbit,
    ChainLightning,
    DamageAura,
    Regen,
    XpFlow,
    Revive,
    Invincible
}

public class RuneDefinition
{
    public RuneId id;
    public string name;
    public string description;
    public Color color;

    public RuneDefinition(RuneId id, string name, string description, Color color)
    {
        this.id = id;
        this.name = name;
        this.description = description;
        this.color = color;
    }
}

public static class RuneSystem
{
    // 无敌符文参数：描述文案由这些常量拼出，保证文案与数值始终一致
    public const float InvincibleFirstDuration = 2.5f;   // 首次获取时立即获得的无敌时长
    public const float InvinciblePulseInterval = 6f;     // 此后每隔多少秒触发一次
    public const float InvinciblePulseDuration = 1.5f;   // 每次触发获得的无敌时长

    // 攻击类型符文重复获取时的额外伤害（“小额增加”）
    public const float ScatterDamageBonusPerStack = 0.12f;    // 散射：作用于主角弹幕
    public const float AttackTypeDamageBonusPerStack = 0.2f;  // 环绕 / 雷霆 / 灼烧光环

    private static readonly List<RuneDefinition> Pool = new List<RuneDefinition>
    {
        new RuneDefinition(RuneId.AttackPower, "力量符文", "攻击力 +25%", new Color(1f, 0.5f, 0.3f)),
        new RuneDefinition(RuneId.AttackRange, "鹰眼符文", "攻击范围 +2", new Color(0.6f, 0.85f, 1f)),
        new RuneDefinition(RuneId.MoveSpeed, "疾风符文", "移动速度 +15%", new Color(0.5f, 1f, 0.7f)),
        new RuneDefinition(RuneId.Defense, "坚壁符文", "防御力 +3，降低受到的伤害", new Color(0.7f, 0.75f, 1f)),
        new RuneDefinition(RuneId.MaxHealth, "生命符文", "最大生命 +30 并回复等量生命", new Color(1f, 0.4f, 0.5f)),
        new RuneDefinition(RuneId.Multishot, "散射符文", "主攻击额外发射 1 枚弹幕", new Color(1f, 0.9f, 0.4f)),
        new RuneDefinition(RuneId.Orbit, "环绕符文", "召唤 1 把环绕玩家的刀刃", new Color(0.8f, 0.9f, 1f)),
        new RuneDefinition(RuneId.ChainLightning, "雷霆符文", "解锁连锁闪电，跳跃次数 +1", new Color(0.7f, 0.85f, 1f)),
        new RuneDefinition(RuneId.DamageAura, "灼烧光环", "周期性灼烧周围敌人，范围与伤害提升", new Color(1f, 0.6f, 0.2f)),
        new RuneDefinition(RuneId.Regen, "生机符文", "每秒回复 1.5 点生命", new Color(0.5f, 1f, 0.6f)),
        new RuneDefinition(RuneId.XpFlow, "智慧符文", "每秒获得 1 点经验", new Color(0.7f, 0.7f, 1f)),
        new RuneDefinition(RuneId.Revive, "不屈符文", "死亡时原地满血复活", new Color(1f, 0.85f, 0.3f)),
        new RuneDefinition(RuneId.Invincible, "无敌符文",
            string.Format("立刻获得 {0} 秒无敌；此后每 {1} 秒获得 {2} 秒无敌", InvincibleFirstDuration, InvinciblePulseInterval, InvinciblePulseDuration),
            new Color(0.6f, 1f, 1f))
    };

    /// <summary>只能获取一次的符文：已拥有后不再出现在候选列表，也无法再次叠加。</summary>
    public static bool IsOneTimeOnly(RuneId id)
    {
        return id == RuneId.Revive || id == RuneId.Invincible;
    }

    /// <summary>“新增一种攻击方式”的符文：重复获取时额外提升该攻击类型的伤害。</summary>
    public static bool IsAttackType(RuneId id)
    {
        return id == RuneId.Multishot || id == RuneId.Orbit ||
               id == RuneId.ChainLightning || id == RuneId.DamageAura;
    }

    public static List<RuneDefinition> RollOptions(PlayerController player, int count)
    {
        var candidates = new List<RuneDefinition>(Pool);
        if (player != null)
        {
            // 一次性符文在已拥有后从池中移除
            if (player.revivesLeft >= 1)
                candidates.RemoveAll(r => r.id == RuneId.Revive);
            if (player.invincibleRuneTaken)
                candidates.RemoveAll(r => r.id == RuneId.Invincible);
        }

        var result = new List<RuneDefinition>();
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            int index = Random.Range(0, candidates.Count);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return result;
    }

    public static void Apply(PlayerController player, RuneId id)
    {
        if (player == null) return;
        int already = player.GetRuneStacks(id); // 本次获取之前已叠加的层数
        player.AddRuneStack(id);

        switch (id)
        {
            case RuneId.AttackPower: player.damageMultiplier += 0.25f; break;
            case RuneId.AttackRange: player.attackRange += 2f; break;
            case RuneId.MoveSpeed: player.moveSpeed *= 1.15f; break;
            case RuneId.Defense: player.armor += 3; UIManager.Instance?.UpdateDefenseUI(player.EffectiveArmor); break;
            case RuneId.MaxHealth: player.maxHealth += 30; player.Heal(30); break;

            case RuneId.Multishot:
                if (already >= 1) player.scatterDamageBonus += ScatterDamageBonusPerStack;
                player.multishotCount++;
                break;
            case RuneId.Orbit:
                if (already >= 1) player.orbitDamageBonus += AttackTypeDamageBonusPerStack;
                player.orbitCount++;
                break;
            case RuneId.ChainLightning:
                if (already >= 1) player.chainDamageBonus += AttackTypeDamageBonusPerStack;
                player.chainLevel++;
                break;
            case RuneId.DamageAura:
                if (already >= 1) player.auraDamageBonus += AttackTypeDamageBonusPerStack;
                player.auraLevel++;
                break;

            case RuneId.Regen: player.regenPerSecond += 1.5f; break;
            case RuneId.XpFlow: player.xpPerSecond += 1f; break;
            case RuneId.Revive: player.revivesLeft = Mathf.Min(1, player.revivesLeft + 1); break;
            case RuneId.Invincible:
                player.invincibleRuneTaken = true;
                player.GrantInvincibility(InvincibleFirstDuration);
                player.ResetInvinciblePulseTimer();
                break;
        }
    }
}
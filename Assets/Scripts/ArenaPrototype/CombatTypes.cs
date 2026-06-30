using System;
using UnityEngine;

namespace ArenaPrototype
{
    public enum Team
    {
        Player,
        Enemy
    }

    public enum DamageTag
    {
        Basic,
        Dash,
        Area,
        Support,
        Ally,
        Enemy
    }

    public enum EnemyArchetype
    {
        Striker,
        Skirmisher,
        Bulwark
    }

    public enum EquipmentSlot
    {
        Weapon,
        Armor,
        Charm
    }

    public enum Rarity
    {
        Common,
        Rare,
        Epic
    }

    [Serializable]
    public class CombatTuning
    {
        public float enemyHealthMultiplier = 1f;
        public float enemyDamageMultiplier = 1f;
        public float playerDamageMultiplier = 1f;
        public float spawnRateMultiplier = 1f;
        public float basicCooldown = 0.55f;
        public float dashCooldown = 4.2f;
        public float areaCooldown = 7f;
        public float wardCooldown = 10f;
        public float projectileHitboxPadding = 0.28f;
        public bool showProjectileHitboxes;
    }

    [Serializable]
    public struct StatusPayload
    {
        public int sparkStacks;
        public float sparkDuration;
        public bool mark;
        public float markDuration;

        public bool HasAnyStatus
        {
            get { return sparkStacks > 0 || mark; }
        }
    }

    public struct DamageInfo
    {
        public float amount;
        public Team sourceTeam;
        public Transform source;
        public Vector3 point;
        public Vector3 direction;
        public float knockback;
        public Color color;
        public DamageTag tag;
        public StatusPayload status;
    }

    public interface IDamageModifier
    {
        float ModifyIncomingDamage(DamageInfo info);
    }

    public interface IStatusReceiver
    {
        void ApplyStatus(StatusPayload payload);
    }
}

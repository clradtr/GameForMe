using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaPrototype
{
    [Serializable]
    public struct StatBlock
    {
        public float maxHealth;
        public float attack;
        public float defense;
        public float moveSpeed;
        public float cooldownReduction;
        public int projectileCount;
        public int pierceCount;
        public int splitCount;
        public float projectileSize;
        public float areaRadius;
        public float dashRadius;

        public bool IsEmpty
        {
            get
            {
                return Mathf.Approximately(maxHealth, 0f)
                    && Mathf.Approximately(attack, 0f)
                    && Mathf.Approximately(defense, 0f)
                    && Mathf.Approximately(moveSpeed, 0f)
                    && Mathf.Approximately(cooldownReduction, 0f)
                    && projectileCount == 0
                    && pierceCount == 0
                    && splitCount == 0
                    && Mathf.Approximately(projectileSize, 0f)
                    && Mathf.Approximately(areaRadius, 0f)
                    && Mathf.Approximately(dashRadius, 0f);
            }
        }

        public static StatBlock operator +(StatBlock a, StatBlock b)
        {
            return new StatBlock
            {
                maxHealth = a.maxHealth + b.maxHealth,
                attack = a.attack + b.attack,
                defense = a.defense + b.defense,
                moveSpeed = a.moveSpeed + b.moveSpeed,
                cooldownReduction = a.cooldownReduction + b.cooldownReduction,
                projectileCount = a.projectileCount + b.projectileCount,
                pierceCount = a.pierceCount + b.pierceCount,
                splitCount = a.splitCount + b.splitCount,
                projectileSize = a.projectileSize + b.projectileSize,
                areaRadius = a.areaRadius + b.areaRadius,
                dashRadius = a.dashRadius + b.dashRadius
            };
        }

        public string Describe()
        {
            List<string> parts = new List<string>();
            if (!Mathf.Approximately(maxHealth, 0f))
            {
                parts.Add(string.Format("+{0:0} HP", maxHealth));
            }

            if (!Mathf.Approximately(attack, 0f))
            {
                parts.Add(string.Format("+{0:0.#} Attack", attack));
            }

            if (!Mathf.Approximately(defense, 0f))
            {
                parts.Add(string.Format("+{0:0.#} Defense", defense));
            }

            if (!Mathf.Approximately(moveSpeed, 0f))
            {
                parts.Add(string.Format("+{0:0.##} Speed", moveSpeed));
            }

            if (!Mathf.Approximately(cooldownReduction, 0f))
            {
                parts.Add(string.Format("+{0:0.#}% Cooldown", cooldownReduction * 100f));
            }

            if (projectileCount != 0)
            {
                parts.Add(string.Format("+{0} Projectile", projectileCount));
            }

            if (pierceCount != 0)
            {
                parts.Add(string.Format("+{0} Pierce", pierceCount));
            }

            if (splitCount != 0)
            {
                parts.Add(string.Format("+{0} Split bolts", splitCount));
            }

            if (!Mathf.Approximately(projectileSize, 0f))
            {
                parts.Add(string.Format("+{0:0.##} Projectile size", projectileSize));
            }

            if (!Mathf.Approximately(areaRadius, 0f))
            {
                parts.Add(string.Format("+{0:0.##} Area radius", areaRadius));
            }

            if (!Mathf.Approximately(dashRadius, 0f))
            {
                parts.Add(string.Format("+{0:0.##} Dash hitbox", dashRadius));
            }

            return parts.Count == 0 ? "No stat change" : string.Join(", ", parts.ToArray());
        }
    }

    [Serializable]
    public class ArenaStats
    {
        public float maxHealth = 120f;
        public float attack = 18f;
        public float defense = 2f;
        public float moveSpeed = 6.4f;
        public float cooldownReduction;
        public int projectileCount;
        public int pierceCount;
        public int splitCount;
        public float projectileSize;
        public float areaRadius;
        public float dashRadius;

        public float CooldownScale
        {
            get { return Mathf.Clamp(1f - cooldownReduction, 0.35f, 1.5f); }
        }

        public ArenaStats Clone()
        {
            return new ArenaStats
            {
                maxHealth = maxHealth,
                attack = attack,
                defense = defense,
                moveSpeed = moveSpeed,
                cooldownReduction = cooldownReduction,
                projectileCount = projectileCount,
                pierceCount = pierceCount,
                splitCount = splitCount,
                projectileSize = projectileSize,
                areaRadius = areaRadius,
                dashRadius = dashRadius
            };
        }

        public void Apply(StatBlock block)
        {
            maxHealth += block.maxHealth;
            attack += block.attack;
            defense += block.defense;
            moveSpeed += block.moveSpeed;
            cooldownReduction += block.cooldownReduction;
            projectileCount += block.projectileCount;
            pierceCount += block.pierceCount;
            splitCount += block.splitCount;
            projectileSize += block.projectileSize;
            areaRadius += block.areaRadius;
            dashRadius += block.dashRadius;
        }

        public StatBlock ToStatBlock()
        {
            return new StatBlock
            {
                maxHealth = maxHealth,
                attack = attack,
                defense = defense,
                moveSpeed = moveSpeed,
                cooldownReduction = cooldownReduction,
                projectileCount = projectileCount,
                pierceCount = pierceCount,
                splitCount = splitCount,
                projectileSize = projectileSize,
                areaRadius = areaRadius,
                dashRadius = dashRadius
            };
        }
    }

    [Serializable]
    public class EquipmentItem
    {
        public string name;
        public string iconId;
        public EquipmentSlot slot;
        public Rarity rarity;
        public StatBlock stats;
        public Color tint = Color.white;

        public string Summary()
        {
            return string.Format("{0} {1}\n{2}", rarity, name, stats.Describe());
        }

        public float Score()
        {
            return stats.maxHealth * 0.08f
                + stats.attack
                + stats.defense * 1.7f
                + stats.moveSpeed * 7f
                + stats.cooldownReduction * 100f
                + stats.projectileCount * 14f
                + stats.pierceCount * 9f
                + stats.splitCount * 7f
                + stats.projectileSize * 30f
                + stats.areaRadius * 8f
                + stats.dashRadius * 7f;
        }
    }
}

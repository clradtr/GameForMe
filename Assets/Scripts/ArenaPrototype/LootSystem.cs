using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ArenaPrototype
{
    public class RewardChoice
    {
        public string title;
        public string description;
        public string iconId;
        public Color color;
        public Action apply;
    }

    public class EquipmentSystem : MonoBehaviour
    {
        private readonly Dictionary<EquipmentSlot, EquipmentItem> equipped = new Dictionary<EquipmentSlot, EquipmentItem>();
        private ArenaGame game;
        private PlayerController player;

        public void Setup(ArenaGame owner, PlayerController controller)
        {
            game = owner;
            player = controller;
        }

        public void Equip(EquipmentItem item)
        {
            equipped[item.slot] = item;
            player.SetEquipmentStats(GetTotalStats());
            game.Notify("Equipped: " + item.Summary().Replace("\n", " - "), 2.4f);
            game.Audio.PlayPickup();
        }

        public StatBlock GetTotalStats()
        {
            StatBlock total = new StatBlock();
            foreach (KeyValuePair<EquipmentSlot, EquipmentItem> pair in equipped)
            {
                total += pair.Value.stats;
            }

            return total;
        }

        public string Summary()
        {
            if (equipped.Count == 0)
            {
                return "Equipment: none";
            }

            List<string> parts = new List<string>();
            foreach (KeyValuePair<EquipmentSlot, EquipmentItem> pair in equipped)
            {
                parts.Add(string.Format("{0}: {1}", pair.Key, pair.Value.name));
            }

            return "Equipment: " + string.Join(" | ", parts.ToArray());
        }

        public EquipmentItem GetEquipped(EquipmentSlot slot)
        {
            EquipmentItem item;
            return equipped.TryGetValue(slot, out item) ? item : null;
        }
    }

    public class LootSystem : MonoBehaviour
    {
        private static readonly string[] WeaponNames = { "Pulse Wand", "Arc Blade", "Vector Lens", "Static Knuckle" };
        private static readonly string[] ArmorNames = { "Signal Coat", "Guard Plate", "Circuit Vest", "Impact Mantle" };
        private static readonly string[] CharmNames = { "Tempo Charm", "Charge Loop", "Ward Sigil", "Spark Core" };

        private ArenaGame game;

        public void Setup(ArenaGame owner)
        {
            game = owner;
        }

        public void TryDropEnemyLoot(EnemyController enemy, int wave)
        {
            float chance = Mathf.Clamp(0.2f + wave * 0.025f, 0.2f, 0.42f);
            if (Random.value > chance)
            {
                return;
            }

            EquipmentItem item = GenerateEquipment(wave, false);
            SpawnPickup(enemy.transform.position + new Vector3(Random.Range(-0.4f, 0.4f), 0.6f, Random.Range(-0.4f, 0.4f)), item);
        }

        public List<RewardChoice> GenerateRewardChoices(int wave)
        {
            List<RewardChoice> choices = new List<RewardChoice>();
            choices.Add(CreateStatReward("Sharpen", new StatBlock { attack = 5f + wave * 1.2f }, new Color(1f, 0.48f, 0.22f)));
            choices.Add(CreateBuildReward(wave));

            EquipmentItem item = GenerateEquipment(wave + 1, true);
            choices.Add(new RewardChoice
            {
                title = item.rarity + " " + item.name,
                description = item.slot + "\n" + item.stats.Describe(),
                iconId = item.iconId,
                color = item.tint,
                apply = () => game.Equipment.Equip(item)
            });

            for (int i = 0; i < choices.Count; i++)
            {
                int swapIndex = Random.Range(i, choices.Count);
                RewardChoice temp = choices[i];
                choices[i] = choices[swapIndex];
                choices[swapIndex] = temp;
            }

            return choices;
        }

        public EquipmentItem GenerateEquipment(int wave, bool reward)
        {
            EquipmentSlot slot = (EquipmentSlot)Random.Range(0, 3);
            Rarity rarity = PickRarity(wave, reward);
            float rarityScale = rarity == Rarity.Epic ? 1.85f : rarity == Rarity.Rare ? 1.35f : 1f;
            float waveScale = 1f + wave * 0.12f;
            StatBlock stats = new StatBlock();
            string itemName;
            string iconId;
            Color tint;

            switch (slot)
            {
                case EquipmentSlot.Armor:
                    itemName = ArmorNames[Random.Range(0, ArmorNames.Length)];
                    iconId = "guard_plate";
                    stats.maxHealth = Mathf.Round(18f * waveScale * rarityScale);
                    stats.defense = 1.3f * rarityScale + wave * 0.12f;
                    tint = rarity == Rarity.Epic ? new Color(0.95f, 0.35f, 1f) : new Color(0.45f, 0.85f, 1f);
                    break;
                case EquipmentSlot.Charm:
                    itemName = CharmNames[Random.Range(0, CharmNames.Length)];
                    iconId = GetCharmIconId(itemName);
                    stats.cooldownReduction = 0.018f * rarityScale + wave * 0.0015f;
                    stats.moveSpeed = 0.08f * rarityScale;
                    tint = rarity == Rarity.Epic ? new Color(1f, 0.85f, 0.22f) : new Color(0.55f, 1f, 0.55f);
                    break;
                default:
                    itemName = WeaponNames[Random.Range(0, WeaponNames.Length)];
                    iconId = "ember_blade";
                    stats.attack = (4f + wave * 0.7f) * rarityScale;
                    tint = rarity == Rarity.Epic ? new Color(1f, 0.35f, 0.35f) : new Color(1f, 0.65f, 0.28f);
                    break;
            }

            ApplyItemAffix(itemName, rarity, ref stats);

            return new EquipmentItem
            {
                name = itemName,
                iconId = iconId,
                slot = slot,
                rarity = rarity,
                stats = stats,
                tint = tint
            };
        }

        private void ApplyItemAffix(string itemName, Rarity rarity, ref StatBlock stats)
        {
            int rarityBonus = rarity == Rarity.Epic ? 2 : rarity == Rarity.Rare ? 1 : 0;
            switch (itemName)
            {
                case "Pulse Wand":
                    stats.pierceCount += 1 + rarityBonus;
                    break;
                case "Arc Blade":
                    stats.splitCount += 2 + rarityBonus;
                    break;
                case "Vector Lens":
                    stats.projectileCount += rarity == Rarity.Epic ? 2 : 1;
                    break;
                case "Static Knuckle":
                    stats.projectileSize += 0.08f + rarityBonus * 0.04f;
                    break;
                case "Signal Coat":
                    stats.dashRadius += 0.22f + rarityBonus * 0.08f;
                    break;
                case "Guard Plate":
                    stats.areaRadius += 0.28f + rarityBonus * 0.12f;
                    break;
                case "Circuit Vest":
                    stats.cooldownReduction += 0.012f + rarityBonus * 0.006f;
                    break;
                case "Impact Mantle":
                    stats.dashRadius += 0.18f + rarityBonus * 0.08f;
                    stats.areaRadius += 0.18f + rarityBonus * 0.08f;
                    break;
                case "Tempo Charm":
                    stats.cooldownReduction += 0.014f + rarityBonus * 0.006f;
                    break;
                case "Charge Loop":
                    stats.pierceCount += 1 + rarityBonus;
                    break;
                case "Ward Sigil":
                    stats.areaRadius += 0.32f + rarityBonus * 0.12f;
                    break;
                case "Spark Core":
                    stats.splitCount += 2 + rarityBonus;
                    stats.projectileSize += 0.04f;
                    break;
            }
        }

        private RewardChoice CreateStatReward(string title, StatBlock stats, Color color)
        {
            return new RewardChoice
            {
                title = title,
                description = stats.Describe(),
                iconId = GetRewardIconId(title),
                color = color,
                apply = () => game.Progression.ApplyPermanentStats(stats, title)
            };
        }

        private string GetRewardIconId(string title)
        {
            switch (title)
            {
                case "Sharpen":
                case "Needle Path":
                case "Blade Drift":
                    return "ember_blade";
                case "Forked Casting":
                case "Heavy Bolt":
                case "Splinter Hex":
                    return "spark_core";
                case "Expanding Nova":
                    return "guard_plate";
                default:
                    return "tempo_charm";
            }
        }

        private RewardChoice CreateBuildReward(int wave)
        {
            int roll = Random.Range(0, 6);
            switch (roll)
            {
                case 0:
                    return CreateStatReward("Forked Casting", new StatBlock { projectileCount = 1 }, new Color(0.25f, 0.95f, 1f));
                case 1:
                    return CreateStatReward("Needle Path", new StatBlock { pierceCount = 1 + wave / 4 }, new Color(1f, 0.8f, 0.25f));
                case 2:
                    return CreateStatReward("Splinter Hex", new StatBlock { splitCount = 2 }, new Color(0.95f, 0.35f, 1f));
                case 3:
                    return CreateStatReward("Expanding Nova", new StatBlock { areaRadius = 0.55f }, new Color(0.45f, 0.85f, 1f));
                case 4:
                    return CreateStatReward("Heavy Bolt", new StatBlock { projectileSize = 0.08f, attack = 2f + wave * 0.4f }, new Color(1f, 0.45f, 0.25f));
                default:
                    return CreateStatReward("Blade Drift", new StatBlock { dashRadius = 0.35f, moveSpeed = 0.18f }, new Color(0.65f, 1f, 0.55f));
            }
        }

        private string GetCharmIconId(string itemName)
        {
            if (itemName == "Tempo Charm")
            {
                return "tempo_charm";
            }

            if (itemName == "Spark Core")
            {
                return "spark_core";
            }

            return "hunter_boots";
        }

        private Rarity PickRarity(int wave, bool reward)
        {
            float roll = Random.value + wave * 0.015f + (reward ? 0.08f : 0f);
            if (roll > 0.92f)
            {
                return Rarity.Epic;
            }

            if (roll > 0.62f)
            {
                return Rarity.Rare;
            }

            return Rarity.Common;
        }

        private void SpawnPickup(Vector3 position, EquipmentItem item)
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = "Loot - " + item.name;
            pickup.transform.position = position;
            pickup.transform.localScale = Vector3.one * 0.55f;
            Renderer renderer = pickup.GetComponent<Renderer>();
            renderer.material = game.CreateMaterial(item.tint, false);
            Collider collider = pickup.GetComponent<Collider>();
            collider.isTrigger = true;
            EquipmentPickup equipmentPickup = pickup.AddComponent<EquipmentPickup>();
            equipmentPickup.Setup(game, item);
        }
    }
}

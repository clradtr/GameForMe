using UnityEngine;

namespace ArenaPrototype
{
    public class PlayerProgression : MonoBehaviour
    {
        private ArenaGame game;
        private PlayerController player;
        private StatBlock permanentStats;

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int NextLevelExperience { get; private set; } = 65;

        public StatBlock PermanentStats
        {
            get { return permanentStats; }
        }

        public void Setup(ArenaGame owner, PlayerController controller)
        {
            game = owner;
            player = controller;
            ApplyToPlayer();
        }

        public void AddExperience(int amount)
        {
            Experience += Mathf.Max(0, amount);
            while (Experience >= NextLevelExperience)
            {
                Experience -= NextLevelExperience;
                Level++;
                NextLevelExperience = Mathf.RoundToInt(NextLevelExperience * 1.22f + 18f);
                StatBlock levelStats = new StatBlock
                {
                    maxHealth = 10f,
                    attack = 2.5f,
                    defense = 0.45f,
                    cooldownReduction = 0.006f
                };
                StatBlock perkStats = GetLevelPerk(Level);
                permanentStats += levelStats + perkStats;
                game.Notify(string.Format("Level {0}: {1} | {2}", Level, levelStats.Describe(), perkStats.Describe()), 2.8f);
            }

            ApplyToPlayer();
        }

        public void ApplyPermanentStats(StatBlock stats, string source)
        {
            permanentStats += stats;
            ApplyToPlayer();
            game.Notify(source + ": " + stats.Describe(), 2.4f);
            game.Audio.PlayReward();
        }

        private void ApplyToPlayer()
        {
            if (player != null)
            {
                player.SetProgressionStats(permanentStats);
            }
        }

        private StatBlock GetLevelPerk(int level)
        {
            switch (level % 5)
            {
                case 0:
                    return new StatBlock { splitCount = 2 };
                case 1:
                    return new StatBlock { areaRadius = 0.45f };
                case 2:
                    return new StatBlock { projectileCount = 1 };
                case 3:
                    return new StatBlock { pierceCount = 1 };
                default:
                    return new StatBlock { projectileSize = 0.08f, dashRadius = 0.18f };
            }
        }
    }
}

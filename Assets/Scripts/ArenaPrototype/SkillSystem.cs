using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaPrototype
{
    public class SkillRuntime
    {
        private readonly Func<CombatTuning, float> cooldownGetter;
        private readonly Func<bool> cast;

        public SkillRuntime(string displayName, string iconId, string inputLabel, string description, Color color, Func<CombatTuning, float> getCooldown, Func<bool> castAction)
        {
            DisplayName = displayName;
            IconId = iconId;
            InputLabel = inputLabel;
            Description = description;
            Color = color;
            cooldownGetter = getCooldown;
            cast = castAction;
        }

        public string DisplayName { get; private set; }
        public string IconId { get; private set; }
        public string InputLabel { get; private set; }
        public string Description { get; private set; }
        public Color Color { get; private set; }
        public float CooldownRemaining { get; private set; }

        public float GetEffectiveCooldown(CombatTuning tuning, ArenaStats stats)
        {
            return Mathf.Max(0.08f, cooldownGetter(tuning) * stats.CooldownScale);
        }

        public float GetCooldownFraction(CombatTuning tuning, ArenaStats stats)
        {
            float effectiveCooldown = GetEffectiveCooldown(tuning, stats);
            return effectiveCooldown <= 0f ? 0f : Mathf.Clamp01(CooldownRemaining / effectiveCooldown);
        }

        public void Tick(float deltaTime)
        {
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - deltaTime);
        }

        public bool TryCast(CombatTuning tuning, ArenaStats stats)
        {
            if (CooldownRemaining > 0f)
            {
                return false;
            }

            if (!cast())
            {
                return false;
            }

            CooldownRemaining = GetEffectiveCooldown(tuning, stats);
            return true;
        }

        public void Refund(float seconds)
        {
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - seconds);
        }
    }

    public class SkillSystem : MonoBehaviour
    {
        private readonly List<SkillRuntime> skills = new List<SkillRuntime>();
        private ArenaGame game;
        private PlayerController player;

        public IReadOnlyList<SkillRuntime> Skills
        {
            get { return skills; }
        }

        public void Configure(ArenaGame owner, PlayerController controller)
        {
            game = owner;
            player = controller;
            skills.Clear();
            skills.Add(new SkillRuntime("Pulse Bolt", "pulse_bolt", "Mouse / 1", "Fast skillshot. Adds Spark stacks.", new Color(0.25f, 0.9f, 1f), t => t.basicCooldown, player.CastBasic));
            skills.Add(new SkillRuntime("Vector Dash", "vector_dash", "Space / 2", "Mobility burst with brief invulnerability.", new Color(1f, 0.85f, 0.25f), t => t.dashCooldown, player.CastDash));
            skills.Add(new SkillRuntime("Circuit Nova", "circuit_nova", "Q / 3", "Area damage. Consumes Spark for bonus damage.", new Color(0.95f, 0.35f, 1f), t => t.areaCooldown, player.CastArea));
            skills.Add(new SkillRuntime("Resonance Ward", "resonance_ward", "E / 4", "Shield, attack buff, and ally overclock.", new Color(0.35f, 1f, 0.45f), t => t.wardCooldown, player.CastWard));
        }

        public void ManualUpdate(float deltaTime)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                skills[i].Tick(deltaTime);
            }
        }

        public bool TryCast(int index)
        {
            if (index < 0 || index >= skills.Count || game == null || player == null)
            {
                return false;
            }

            bool casted = skills[index].TryCast(game.Tuning, player.CurrentStats);
            if (casted)
            {
                game.Audio.PlaySkill();
            }

            return casted;
        }

        public void RefundAll(float seconds)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                skills[i].Refund(seconds);
            }
        }
    }
}

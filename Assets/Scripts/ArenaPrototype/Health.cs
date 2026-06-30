using System;
using UnityEngine;

namespace ArenaPrototype
{
    public class Health : MonoBehaviour
    {
        public event Action<Health, DamageInfo, float> Damaged;
        public event Action<Health> Died;

        private ArenaGame game;
        private HitFlash hitFlash;
        private float currentHealth;
        private float maxHealth;
        private bool died;

        public Team Team { get; private set; }
        public float Defense { get; set; }
        public bool IsInvulnerable { get; set; }

        public bool IsAlive
        {
            get { return !died && currentHealth > 0f; }
        }

        public float CurrentHealth
        {
            get { return currentHealth; }
        }

        public float MaxHealth
        {
            get { return maxHealth; }
        }

        public float Normalized
        {
            get { return maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth); }
        }

        public void Setup(Team team, float health, ArenaGame owner)
        {
            Team = team;
            game = owner;
            maxHealth = Mathf.Max(1f, health);
            currentHealth = maxHealth;
            hitFlash = GetComponent<HitFlash>();
            if (hitFlash == null)
            {
                hitFlash = gameObject.AddComponent<HitFlash>();
            }
        }

        public void SetMaxHealth(float value, bool keepPercent)
        {
            value = Mathf.Max(1f, value);
            float percent = maxHealth <= 0f ? 1f : currentHealth / maxHealth;
            float previousMax = maxHealth;
            maxHealth = value;

            if (keepPercent)
            {
                currentHealth = Mathf.Clamp(maxHealth * percent, 1f, maxHealth);
            }
            else if (maxHealth > previousMax)
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + (maxHealth - previousMax));
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0f, amount));
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || IsInvulnerable)
            {
                return;
            }

            float modifiedAmount = Mathf.Max(0f, info.amount);
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                IDamageModifier modifier = behaviours[i] as IDamageModifier;
                if (modifier != null)
                {
                    DamageInfo modifierInfo = info;
                    modifierInfo.amount = modifiedAmount;
                    modifiedAmount = modifier.ModifyIncomingDamage(modifierInfo);
                }
            }

            float finalAmount = Mathf.Max(1f, modifiedAmount - Defense);
            currentHealth -= finalAmount;

            if (hitFlash != null)
            {
                hitFlash.Flash(info.color, 0.1f);
            }

            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null && info.knockback > 0f)
            {
                Vector3 force = info.direction.sqrMagnitude > 0.001f ? info.direction.normalized : Vector3.zero;
                body.AddForce(force * info.knockback, ForceMode.VelocityChange);
            }

            if (game != null)
            {
                game.SpawnDamageNumber(transform.position + Vector3.up * 1.8f, finalAmount.ToString("0"), info.color, 1f);
                Vector3 hitPoint = info.point.sqrMagnitude > 0.001f ? info.point : transform.position;
                game.SpawnVfx("hit_spark", hitPoint + Vector3.up * 1.05f, 0.55f, 1.35f, 0.24f, new Color(info.color.r, info.color.g, info.color.b, 0.95f), false, 0f);
                game.Audio.PlayHit();
            }

            if (info.status.HasAnyStatus)
            {
                for (int i = 0; i < behaviours.Length; i++)
                {
                    IStatusReceiver receiver = behaviours[i] as IStatusReceiver;
                    if (receiver != null)
                    {
                        receiver.ApplyStatus(info.status);
                    }
                }
            }

            if (Damaged != null)
            {
                Damaged(this, info, finalAmount);
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (died)
            {
                return;
            }

            died = true;
            currentHealth = 0f;

            if (Died != null)
            {
                Died(this);
            }
        }
    }
}

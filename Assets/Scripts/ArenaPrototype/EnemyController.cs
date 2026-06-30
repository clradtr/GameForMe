using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaPrototype
{
    public class EnemyController : MonoBehaviour, IDamageModifier, IStatusReceiver
    {
        private ArenaGame game;
        private Rigidbody body;
        private Renderer bodyRenderer;
        private Color baseColor;
        private bool usingGeneratedSprite;
        private float moveSpeed;
        private float damage;
        private float attackRange;
        private float nextAttackTime;
        private float nextSpecialTime;
        private bool dead;
        private bool locked;
        private int sparkStacks;
        private float sparkExpiresAt;
        private float markedUntil;

        public EnemyArchetype Archetype { get; private set; }
        public Health Health { get; private set; }
        public int ExperienceValue { get; private set; }

        public void Setup(ArenaGame owner, EnemyArchetype type, int wave)
        {
            game = owner;
            Archetype = type;

            body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            float maxHealth = 42f + wave * 7f;
            moveSpeed = 3.2f + wave * 0.04f;
            damage = 9f + wave * 1.2f;
            attackRange = 1.55f;
            ExperienceValue = 15 + wave * 3;

            switch (type)
            {
                case EnemyArchetype.Skirmisher:
                    maxHealth = 30f + wave * 5f;
                    moveSpeed = 3.55f;
                    damage = 7f + wave * 1.05f;
                    attackRange = 7.5f;
                    ExperienceValue += 3;
                    transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                    baseColor = new Color(1f, 0.45f, 0.25f);
                    break;
                case EnemyArchetype.Bulwark:
                    maxHealth = 92f + wave * 13f;
                    moveSpeed = 2.05f;
                    damage = 15f + wave * 1.6f;
                    attackRange = 2.2f;
                    ExperienceValue += 8;
                    transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
                    baseColor = new Color(0.82f, 0.25f, 0.2f);
                    break;
                default:
                    baseColor = new Color(0.95f, 0.18f, 0.22f);
                    break;
            }

            bodyRenderer = game.AttachEnemySprite(gameObject, type);
            usingGeneratedSprite = bodyRenderer != null;
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (bodyRenderer != null)
            {
                if (usingGeneratedSprite)
                {
                    bodyRenderer.material.color = Color.white;
                }
                else
                {
                    bodyRenderer.material = game.CreateMaterial(baseColor, false);
                }
            }

            Health = gameObject.AddComponent<Health>();
            Health.Setup(Team.Enemy, maxHealth * game.Tuning.enemyHealthMultiplier, game);
            Health.Defense = type == EnemyArchetype.Bulwark ? 2.5f + wave * 0.2f : 0.5f + wave * 0.08f;
            Health.Died += OnDied;
        }

        private void Update()
        {
            if (dead || Health == null || !Health.IsAlive || game == null)
            {
                return;
            }

            if (Time.time > sparkExpiresAt)
            {
                sparkStacks = 0;
            }

            UpdateVisualStatus();

            if (locked)
            {
                return;
            }

            Transform target = game.GetPreferredEnemyTarget(transform.position);
            if (target == null)
            {
                body.linearVelocity = Vector3.zero;
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 direction = distance > 0.01f ? toTarget / distance : Vector3.zero;

            switch (Archetype)
            {
                case EnemyArchetype.Skirmisher:
                    UpdateSkirmisher(target, direction, distance);
                    break;
                case EnemyArchetype.Bulwark:
                    UpdateBulwark(direction, distance);
                    break;
                default:
                    UpdateStriker(direction, distance);
                    break;
            }

            transform.position = game.ClampToArena(transform.position, 0.9f);
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
            }
        }

        public float ModifyIncomingDamage(DamageInfo info)
        {
            float multiplier = 1f;
            if (Time.time < markedUntil)
            {
                multiplier += 0.15f;
            }

            if (sparkStacks > 0 && Time.time < sparkExpiresAt)
            {
                if (info.tag == DamageTag.Area)
                {
                    multiplier += sparkStacks * 0.22f;
                    if (game != null)
                    {
                        game.SpawnDamageNumber(transform.position + Vector3.up * 2.25f, "SPARK", new Color(0.25f, 0.95f, 1f), 0.85f);
                    }

                    sparkStacks = 0;
                }
                else
                {
                    multiplier += sparkStacks * 0.04f;
                }
            }

            return info.amount * multiplier;
        }

        public void ApplyStatus(StatusPayload payload)
        {
            if (payload.sparkStacks > 0)
            {
                sparkStacks = Mathf.Clamp(sparkStacks + payload.sparkStacks, 0, 5);
                sparkExpiresAt = Time.time + Mathf.Max(0.1f, payload.sparkDuration);
            }

            if (payload.mark)
            {
                markedUntil = Time.time + Mathf.Max(0.1f, payload.markDuration);
            }
        }

        private void UpdateStriker(Vector3 direction, float distance)
        {
            body.linearVelocity = direction * moveSpeed;
            if (distance <= attackRange && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + 1.05f;
                MeleeAttack(direction, 1.35f);
            }
        }

        private void UpdateSkirmisher(Transform target, Vector3 direction, float distance)
        {
            Vector3 desired = Vector3.zero;
            if (distance < 4.3f)
            {
                desired = -direction;
            }
            else if (distance > 6.6f)
            {
                desired = direction;
            }
            else
            {
                desired = Vector3.Cross(Vector3.up, direction).normalized * Mathf.Sin(Time.time * 1.4f);
            }

            body.linearVelocity = desired * moveSpeed;

            if (distance <= attackRange && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + 1.7f;
                DamageInfo info = new DamageInfo
                {
                    amount = damage * game.Tuning.enemyDamageMultiplier,
                    sourceTeam = Team.Enemy,
                    source = transform,
                    color = new Color(1f, 0.35f, 0.2f),
                    tag = DamageTag.Enemy,
                    knockback = 2.5f
                };
                game.SpawnProjectile(transform.position + Vector3.up * 0.75f + direction * 0.7f, direction, 0.24f, 11.5f, 12f, false, info);
            }
        }

        private void UpdateBulwark(Vector3 direction, float distance)
        {
            body.linearVelocity = direction * moveSpeed;

            if (distance <= attackRange && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + 2.15f;
                StartCoroutine(SlamRoutine(direction));
            }
            else if (distance > 3.2f && distance < 8.5f && Time.time >= nextSpecialTime)
            {
                nextSpecialTime = Time.time + 5.2f;
                StartCoroutine(ChargeRoutine(direction));
            }
        }

        private void MeleeAttack(Vector3 direction, float radius)
        {
            DamageInfo info = new DamageInfo
            {
                amount = damage * game.Tuning.enemyDamageMultiplier,
                sourceTeam = Team.Enemy,
                source = transform,
                direction = direction,
                color = new Color(1f, 0.2f, 0.15f),
                tag = DamageTag.Enemy,
                knockback = 3f
            };
            game.DamageTeamInRadius(transform.position + direction * 0.9f, radius, Team.Player, info);
        }

        private IEnumerator SlamRoutine(Vector3 direction)
        {
            locked = true;
            body.linearVelocity = Vector3.zero;
            Vector3 slamCenter = transform.position + direction * 0.85f;
            game.SpawnVfx("telegraph_marker", new Vector3(slamCenter.x, 0.1f, slamCenter.z), 2.1f, 2.8f, 0.45f, new Color(1f, 0.12f, 0.08f, 0.88f), true, 0f);
            yield return new WaitForSeconds(0.45f);

            DamageInfo info = new DamageInfo
            {
                amount = damage * 1.35f * game.Tuning.enemyDamageMultiplier,
                sourceTeam = Team.Enemy,
                source = transform,
                direction = direction,
                color = new Color(1f, 0.15f, 0.1f),
                tag = DamageTag.Enemy,
                knockback = 5f
            };
            game.SpawnAreaPulse(slamCenter, 2.45f, 0.02f, 0.35f, info, new Color(1f, 0.18f, 0.12f));

            yield return new WaitForSeconds(0.15f);
            locked = false;
        }

        private IEnumerator ChargeRoutine(Vector3 direction)
        {
            locked = true;
            float end = Time.time + 0.45f;
            HashSet<Health> hitTargets = new HashSet<Health>();
            DamageInfo info = new DamageInfo
            {
                amount = damage * 1.15f * game.Tuning.enemyDamageMultiplier,
                sourceTeam = Team.Enemy,
                source = transform,
                direction = direction,
                color = new Color(1f, 0.22f, 0.12f),
                tag = DamageTag.Enemy,
                knockback = 6f
            };

            while (Time.time < end)
            {
                body.linearVelocity = direction * 8.5f;
                DamageChargeTargets(transform.position + direction * 0.9f, 1.05f, hitTargets, info);
                yield return null;
            }

            body.linearVelocity = Vector3.zero;
            locked = false;
        }

        private void DamageChargeTargets(Vector3 center, float radius, HashSet<Health> hitTargets, DamageInfo info)
        {
            Collider[] colliders = Physics.OverlapSphere(center, radius);
            for (int i = 0; i < colliders.Length; i++)
            {
                Health health = colliders[i].GetComponentInParent<Health>();
                if (health == null || !health.IsAlive || health.Team != Team.Player || hitTargets.Contains(health))
                {
                    continue;
                }

                hitTargets.Add(health);
                Vector3 direction = health.transform.position - center;
                DamageInfo hit = info;
                hit.point = health.transform.position;
                hit.direction = direction.sqrMagnitude > 0.001f ? direction.normalized : info.direction;
                health.TakeDamage(hit);
            }
        }

        private void UpdateVisualStatus()
        {
            if (bodyRenderer == null)
            {
                return;
            }

            Color color = usingGeneratedSprite ? Color.white : baseColor;
            if (sparkStacks > 0 && Time.time < sparkExpiresAt)
            {
                color = Color.Lerp(color, new Color(0.2f, 0.95f, 1f), 0.18f + sparkStacks * 0.08f);
            }

            if (Time.time < markedUntil)
            {
                color = Color.Lerp(color, new Color(1f, 1f, 0.45f), 0.25f);
            }

            bodyRenderer.material.color = color;
        }

        private void OnDied(Health health)
        {
            if (dead)
            {
                return;
            }

            dead = true;
            body.linearVelocity = Vector3.zero;
            game.WaveSpawner.NotifyEnemyKilled(this);
            Destroy(gameObject, 0.1f);
        }
    }
}

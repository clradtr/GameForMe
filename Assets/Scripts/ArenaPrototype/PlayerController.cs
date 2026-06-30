using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaPrototype
{
    public class PlayerController : MonoBehaviour
    {
        private ArenaGame game;
        private Rigidbody body;
        private Vector3 moveInput;
        private Vector3 aimDirection = Vector3.forward;
        private bool dashing;
        private float dashUntil;
        private StatBlock progressionStats;
        private StatBlock equipmentStats;
        private StatBlock temporaryStats;
        private Coroutine wardRoutine;

        public ArenaStats BaseStats { get; private set; }
        public ArenaStats CurrentStats { get; private set; }
        public Health Health { get; private set; }
        public SkillSystem Skills { get; private set; }

        public void Setup(ArenaGame owner)
        {
            game = owner;
            BaseStats = new ArenaStats();
            CurrentStats = BaseStats.Clone();

            body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            Health = gameObject.AddComponent<Health>();
            Health.Setup(Team.Player, CurrentStats.maxHealth, game);
            Health.Defense = CurrentStats.defense;
            Health.Died += OnDied;

            Skills = gameObject.AddComponent<SkillSystem>();
            Skills.Configure(game, this);
        }

        private void Update()
        {
            if (game == null || Health == null || !Health.IsAlive)
            {
                return;
            }

            ReadMovement();
            UpdateAim();
            Skills.ManualUpdate(Time.deltaTime);

            if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Alpha1))
            {
                Skills.TryCast(0);
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha2))
            {
                Skills.TryCast(1);
            }

            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Alpha3))
            {
                Skills.TryCast(2);
            }

            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Alpha4))
            {
                Skills.TryCast(3);
            }
        }

        private void FixedUpdate()
        {
            if (body == null || Health == null || !Health.IsAlive)
            {
                return;
            }

            if (dashing)
            {
                body.linearVelocity = aimDirection.normalized * 17f;
                if (Time.time >= dashUntil)
                {
                    dashing = false;
                    Health.IsInvulnerable = false;
                }
            }
            else
            {
                Vector3 desired = moveInput * CurrentStats.moveSpeed;
                body.linearVelocity = new Vector3(desired.x, 0f, desired.z);
            }

            transform.position = game.ClampToArena(transform.position, 0.8f);

            if (aimDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(aimDirection), 16f * Time.fixedDeltaTime);
            }
        }

        public void SetProgressionStats(StatBlock stats)
        {
            progressionStats = stats;
            RecalculateStats(true);
        }

        public void SetEquipmentStats(StatBlock stats)
        {
            equipmentStats = stats;
            RecalculateStats(false);
        }

        public void RecalculateStats(bool keepHealthPercent)
        {
            CurrentStats = BaseStats.Clone();
            CurrentStats.Apply(progressionStats);
            CurrentStats.Apply(equipmentStats);
            CurrentStats.Apply(temporaryStats);
            CurrentStats.moveSpeed = Mathf.Clamp(CurrentStats.moveSpeed, 3f, 11f);
            CurrentStats.cooldownReduction = Mathf.Clamp(CurrentStats.cooldownReduction, 0f, 0.65f);
            CurrentStats.projectileCount = Mathf.Clamp(CurrentStats.projectileCount, 0, 4);
            CurrentStats.pierceCount = Mathf.Clamp(CurrentStats.pierceCount, 0, 5);
            CurrentStats.splitCount = Mathf.Clamp(CurrentStats.splitCount, 0, 6);
            CurrentStats.projectileSize = Mathf.Clamp(CurrentStats.projectileSize, 0f, 0.38f);
            CurrentStats.areaRadius = Mathf.Clamp(CurrentStats.areaRadius, 0f, 2.6f);
            CurrentStats.dashRadius = Mathf.Clamp(CurrentStats.dashRadius, 0f, 1.3f);

            if (Health != null)
            {
                Health.SetMaxHealth(CurrentStats.maxHealth, keepHealthPercent);
                Health.Defense = CurrentStats.defense;
            }
        }

        public bool CastBasic()
        {
            int projectileTotal = 1 + CurrentStats.projectileCount;
            float projectileRadius = 0.28f + CurrentStats.projectileSize;
            float fanAngle = projectileTotal <= 1 ? 0f : Mathf.Clamp(10f + projectileTotal * 7f, 18f, 42f);
            float damageScale = projectileTotal <= 1 ? 1f : Mathf.Lerp(0.9f, 0.68f, Mathf.Clamp01((projectileTotal - 2f) / 3f));
            DamageInfo info = new DamageInfo
            {
                amount = (8f + CurrentStats.attack * 0.68f) * game.Tuning.playerDamageMultiplier * damageScale,
                sourceTeam = Team.Player,
                source = transform,
                color = new Color(0.25f, 0.95f, 1f),
                tag = DamageTag.Basic,
                knockback = 1.4f,
                status = new StatusPayload { sparkStacks = 1, sparkDuration = 5f }
            };

            game.SpawnVfx("cast_circle", new Vector3(transform.position.x, 0.08f, transform.position.z), 0.85f, 1.45f + CurrentStats.projectileSize * 1.4f, 0.24f, new Color(0.25f, 0.95f, 1f, 0.92f), true, GetAimYaw());
            for (int i = 0; i < projectileTotal; i++)
            {
                float t = projectileTotal == 1 ? 0.5f : i / (float)(projectileTotal - 1);
                float angle = Mathf.Lerp(-fanAngle * 0.5f, fanAngle * 0.5f, t);
                Vector3 shotDirection = Quaternion.Euler(0f, angle, 0f) * aimDirection;
                Vector3 origin = transform.position + Vector3.up * 0.8f + shotDirection * 0.75f;
                game.SpawnProjectile(origin, shotDirection, projectileRadius, 18f, 14f, CurrentStats.pierceCount, CurrentStats.splitCount, 0.58f, info);
            }

            return true;
        }

        public bool CastDash()
        {
            if (aimDirection.sqrMagnitude < 0.001f)
            {
                aimDirection = transform.forward;
            }

            dashing = true;
            dashUntil = Time.time + 0.18f;
            Health.IsInvulnerable = true;
            Vector3 vfxPosition = transform.position + aimDirection * 0.75f;
            float dashRadius = GetDashDamageRadius();
            game.SpawnSkillEffect("vector_dash_slash", new Vector3(vfxPosition.x, 0.1f, vfxPosition.z), dashRadius * 0.95f, dashRadius * 2.45f, 0.28f, new Color(1f, 1f, 1f, 0.95f), true, GetAimYaw());
            StartCoroutine(DashDamageRoutine());
            return true;
        }

        public bool CastArea()
        {
            DamageInfo info = new DamageInfo
            {
                amount = (15f + CurrentStats.attack * 0.9f) * game.Tuning.playerDamageMultiplier,
                sourceTeam = Team.Player,
                source = transform,
                color = new Color(1f, 0.35f, 1f),
                tag = DamageTag.Area,
                knockback = 4.2f
            };

            float radius = 3.5f + CurrentStats.areaRadius;
            game.SpawnSkillEffect("circuit_nova_burst", new Vector3(transform.position.x, 0.09f, transform.position.z), 1.2f, radius * 2.15f, 0.48f, new Color(1f, 1f, 1f, 0.9f), true, 0f);
            game.SpawnAreaPulse(transform.position, radius, 0.08f, 0.45f, info, new Color(1f, 0.35f, 1f));
            return true;
        }

        public bool CastWard()
        {
            if (wardRoutine != null)
            {
                StopCoroutine(wardRoutine);
            }

            wardRoutine = StartCoroutine(WardRoutine());
            return true;
        }

        private IEnumerator DashDamageRoutine()
        {
            HashSet<Health> hit = new HashSet<Health>();
            Vector3 previousPosition = transform.position;
            while (dashing)
            {
                Vector3 currentPosition = transform.position;
                Vector3 pathStart = previousPosition - aimDirection * 0.25f;
                Vector3 pathEnd = currentPosition + aimDirection * 0.95f;
                DamageInfo info = new DamageInfo
                {
                    amount = (10f + CurrentStats.attack * 0.45f) * game.Tuning.playerDamageMultiplier,
                    sourceTeam = Team.Player,
                    source = transform,
                    direction = aimDirection,
                    color = new Color(1f, 0.85f, 0.25f),
                    tag = DamageTag.Dash,
                    knockback = 5.5f
                };
                game.DamageTeamInCapsule(pathStart, pathEnd, GetDashDamageRadius(), Team.Enemy, info, hit);

                previousPosition = currentPosition;
                yield return null;
            }
        }

        private IEnumerator WardRoutine()
        {
            temporaryStats = new StatBlock
            {
                attack = 8f,
                defense = 8f,
                cooldownReduction = 0.06f
            };
            RecalculateStats(true);
            Health.Heal(18f);
            game.BoostAlly(5f);
            game.SpawnSkillEffect("resonance_ward_aura", new Vector3(transform.position.x, 0.1f, transform.position.z), 1.4f, 3.4f, 0.55f, new Color(1f, 1f, 1f, 0.92f), true, 0f);
            game.SpawnAreaPulse(transform.position, 2.2f, 0.02f, 0.25f, new DamageInfo
            {
                amount = 0f,
                sourceTeam = Team.Player,
                source = transform,
                color = new Color(0.35f, 1f, 0.45f),
                tag = DamageTag.Support
            }, new Color(0.35f, 1f, 0.45f));
            game.Notify("Resonance Ward: shielded, attack up, ally overclocked", 1.8f);

            yield return new WaitForSeconds(5f);

            temporaryStats = new StatBlock();
            RecalculateStats(true);
            wardRoutine = null;
        }

        private void ReadMovement()
        {
            float horizontal = 0f;
            float vertical = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                horizontal -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                horizontal += 1f;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                vertical += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                vertical -= 1f;
            }

            moveInput = new Vector3(horizontal, 0f, vertical);
            moveInput = Vector3.ClampMagnitude(moveInput, 1f);
            if (moveInput.sqrMagnitude > 0.001f && !Input.GetMouseButton(1))
            {
                aimDirection = moveInput.normalized;
            }
        }

        private void UpdateAim()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            Plane floor = new Plane(Vector3.up, Vector3.zero);
            float enter;
            if (floor.Raycast(ray, out enter))
            {
                Vector3 point = ray.GetPoint(enter);
                Vector3 direction = point - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.05f)
                {
                    aimDirection = direction.normalized;
                }
            }
        }

        private float GetAimYaw()
        {
            Vector3 direction = aimDirection.sqrMagnitude > 0.001f ? aimDirection.normalized : transform.forward;
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private float GetDashDamageRadius()
        {
            return 1.25f + CurrentStats.dashRadius;
        }

        private void OnDied(Health health)
        {
            body.linearVelocity = Vector3.zero;
            game.OnPlayerDied();
        }
    }
}

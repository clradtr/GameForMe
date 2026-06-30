using UnityEngine;

namespace ArenaPrototype
{
    public class AllyUnit : MonoBehaviour
    {
        private ArenaGame game;
        private Rigidbody body;
        private Health health;
        private float nextShotTime;
        private float boostedUntil;

        public Health Health
        {
            get { return health; }
        }

        public void Setup(ArenaGame owner)
        {
            game = owner;
            body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            health = gameObject.AddComponent<Health>();
            health.Setup(Team.Player, 90f, game);
            health.Defense = 1f;
            health.Died += OnDied;
        }

        public void Boost(float duration)
        {
            boostedUntil = Time.time + duration;
            if (health != null && health.IsAlive)
            {
                health.Heal(20f);
            }
        }

        private void Update()
        {
            if (game == null || game.Player == null || health == null || !health.IsAlive)
            {
                return;
            }

            Vector3 anchor = game.Player.transform.position + new Vector3(-1.6f, 0f, -1.2f);
            Vector3 toAnchor = anchor - transform.position;
            toAnchor.y = 0f;
            body.linearVelocity = Vector3.ClampMagnitude(toAnchor, 1f) * 4.4f;

            Health target = game.FindClosestHealth(transform.position, Team.Enemy, 8.5f);
            float cooldown = Time.time < boostedUntil ? 0.55f : 1.25f;
            if (target != null && Time.time >= nextShotTime)
            {
                nextShotTime = Time.time + cooldown;
                Fire(target);
            }
        }

        private void Fire(Health target)
        {
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            float boosted = Time.time < boostedUntil ? 1.65f : 1f;
            DamageInfo info = new DamageInfo
            {
                amount = 7f * boosted,
                sourceTeam = Team.Player,
                source = transform,
                color = new Color(1f, 1f, 0.35f),
                tag = DamageTag.Ally,
                knockback = 0.5f,
                status = new StatusPayload { mark = true, markDuration = 3.5f }
            };
            game.SpawnProjectile(transform.position + Vector3.up * 0.65f, direction.normalized, 0.2f, 13f, 9f, false, info);
        }

        private void OnDied(Health allyHealth)
        {
            game.Notify("Ally down. Ward can protect it earlier next run.", 2.5f);
            body.linearVelocity = Vector3.zero;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ArenaPrototype
{
    public class Projectile : MonoBehaviour
    {
        private readonly HashSet<Health> hitTargets = new HashSet<Health>();
        private readonly HashSet<Health> checkedTargets = new HashSet<Health>();

        private ArenaGame game;
        private DamageInfo damage;
        private Vector3 direction;
        private float hitRadius;
        private float speed;
        private float maxDistance;
        private float traveled;
        private int pierceRemaining;
        private int splitCount;
        private float splitDamageMultiplier = 0.58f;
        private bool destroyed;
        private GameObject debugHitboxDisc;
        private Material debugHitboxMaterial;

        public void Setup(ArenaGame owner, DamageInfo info, Vector3 moveDirection, float radius, float projectileSpeed, float distance, int pierces, int splits, float splitMultiplier)
        {
            game = owner;
            damage = info;
            direction = moveDirection.sqrMagnitude > 0.001f ? moveDirection.normalized : Vector3.forward;
            hitRadius = Mathf.Max(0.05f, radius);
            speed = projectileSpeed;
            maxDistance = distance;
            pierceRemaining = Mathf.Max(0, pierces);
            splitCount = Mathf.Max(0, splits);
            splitDamageMultiplier = Mathf.Clamp(splitMultiplier, 0.15f, 1f);
            transform.forward = direction;
        }

        private void Update()
        {
            if (destroyed)
            {
                return;
            }

            Vector3 previousPosition = transform.position;
            float step = speed * Time.deltaTime;
            Vector3 nextPosition = previousPosition + direction * step;
            HitAlongPath(previousPosition, nextPosition, step);
            transform.position = nextPosition;
            UpdateDebugHitboxVisual();
            traveled += step;

            if (traveled >= maxDistance || !game.IsInsideArena(transform.position, 1f))
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHitCollider(other, transform.position);
        }

        private void OnDestroy()
        {
            DestroyDebugHitboxVisual();
        }

        private void HitAlongPath(Vector3 start, Vector3 end, float distance)
        {
            checkedTargets.Clear();
            float searchRadius = distance * 0.5f + GetPlanarHitRadius() + 1.8f;
            Vector3 midpoint = (start + end) * 0.5f;
            Collider[] colliders = Physics.OverlapSphere(new Vector3(midpoint.x, 1f, midpoint.z), searchRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                Health health = colliders[i].GetComponentInParent<Health>();
                if (health == null || checkedTargets.Contains(health))
                {
                    continue;
                }

                checkedTargets.Add(health);
                if (TryHitHealthOnPlanarPath(health, start, end) && destroyed)
                {
                    return;
                }
            }
        }

        private bool TryHitCollider(Collider other, Vector3 hitPoint)
        {
            if (destroyed)
            {
                return false;
            }

            Health health = other.GetComponentInParent<Health>();
            if (health == null)
            {
                return false;
            }

            return TryHitHealth(health, hitPoint);
        }

        private bool TryHitHealthOnPlanarPath(Health health, Vector3 start, Vector3 end)
        {
            if (destroyed || health == null || !health.IsAlive || health.Team == damage.sourceTeam || hitTargets.Contains(health))
            {
                return false;
            }

            Vector3 target = health.transform.position;
            Vector2 closest = ClosestPointOnSegmentXZ(start, end, target);
            Vector2 targetPoint = new Vector2(target.x, target.z);
            float allowedDistance = GetPlanarHitRadius() + GetTargetPlanarRadius(health);
            if ((targetPoint - closest).sqrMagnitude > allowedDistance * allowedDistance)
            {
                return false;
            }

            Vector3 hitPoint = new Vector3(closest.x, transform.position.y, closest.y);
            return TryHitHealth(health, hitPoint);
        }

        private bool TryHitHealth(Health health, Vector3 hitPoint)
        {
            if (destroyed || health == null || !health.IsAlive || health.Team == damage.sourceTeam || hitTargets.Contains(health))
            {
                return false;
            }

            hitTargets.Add(health);
            DamageInfo hitInfo = damage;
            hitInfo.point = hitPoint;
            hitInfo.direction = direction;
            health.TakeDamage(hitInfo);
            SpawnSplitProjectiles(hitPoint);

            if (pierceRemaining > 0)
            {
                pierceRemaining--;
                return true;
            }

            destroyed = true;
            Destroy(gameObject);
            return true;
        }

        private float GetPlanarHitRadius()
        {
            float padding = game != null && game.Tuning != null ? game.Tuning.projectileHitboxPadding : 0.28f;
            return hitRadius + padding;
        }

        private float GetTargetPlanarRadius(Health health)
        {
            Collider[] colliders = health.GetComponentsInChildren<Collider>();
            float radius = 0.42f;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                Bounds bounds = collider.bounds;
                radius = Mathf.Max(radius, Mathf.Max(bounds.extents.x, bounds.extents.z));
            }

            return radius;
        }

        private Vector2 ClosestPointOnSegmentXZ(Vector3 start, Vector3 end, Vector3 point)
        {
            Vector2 a = new Vector2(start.x, start.z);
            Vector2 b = new Vector2(end.x, end.z);
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 ab = b - a;
            float lengthSquared = ab.sqrMagnitude;
            if (lengthSquared <= 0.0001f)
            {
                return a;
            }

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared);
            return a + ab * t;
        }

        private void UpdateDebugHitboxVisual()
        {
            bool shouldShow = game != null && game.Tuning != null && game.Tuning.showProjectileHitboxes;
            if (!shouldShow)
            {
                DestroyDebugHitboxVisual();
                return;
            }

            if (debugHitboxDisc == null)
            {
                CreateDebugHitboxVisual();
            }

            if (debugHitboxDisc == null)
            {
                return;
            }

            float radius = GetPlanarHitRadius();
            debugHitboxDisc.transform.position = new Vector3(transform.position.x, 0.075f, transform.position.z);
            debugHitboxDisc.transform.localScale = new Vector3(radius * 2f, 0.018f, radius * 2f);
        }

        private void CreateDebugHitboxVisual()
        {
            debugHitboxDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            debugHitboxDisc.name = "Projectile Hitbox Preview";

            Collider previewCollider = debugHitboxDisc.GetComponent<Collider>();
            if (previewCollider != null)
            {
                Destroy(previewCollider);
            }

            Renderer renderer = debugHitboxDisc.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Color color = damage.sourceTeam == Team.Player
                ? new Color(0.22f, 0.9f, 1f, 0.24f)
                : new Color(1f, 0.2f, 0.12f, 0.18f);
            debugHitboxMaterial = CreateTransparentDebugMaterial(color);
            renderer.sharedMaterial = debugHitboxMaterial;
        }

        private Material CreateTransparentDebugMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader);
            material.name = "Projectile Hitbox Preview Material";
            material.color = color;
            return material;
        }

        private void DestroyDebugHitboxVisual()
        {
            if (debugHitboxDisc != null)
            {
                Destroy(debugHitboxDisc);
                debugHitboxDisc = null;
            }

            if (debugHitboxMaterial != null)
            {
                Destroy(debugHitboxMaterial);
                debugHitboxMaterial = null;
            }
        }

        private void SpawnSplitProjectiles(Vector3 hitPoint)
        {
            if (splitCount <= 0 || game == null || damage.sourceTeam != Team.Player)
            {
                return;
            }

            int count = Mathf.Clamp(splitCount, 1, 6);
            float spread = Mathf.Clamp(22f + count * 8f, 30f, 70f);
            float startAngle = -spread * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float angle = Mathf.Lerp(startAngle, spread * 0.5f, t);
                Vector3 splitDirection = Quaternion.Euler(0f, angle, 0f) * direction;
                DamageInfo splitDamage = damage;
                splitDamage.amount *= splitDamageMultiplier;
                splitDamage.status.sparkStacks = Mathf.Max(0, splitDamage.status.sparkStacks);
                Vector3 origin = hitPoint + splitDirection.normalized * (hitRadius + 0.18f);
                game.SpawnProjectile(origin, splitDirection, hitRadius * 0.78f, speed * 0.92f, maxDistance * 0.42f, 0, 0, splitDamageMultiplier, splitDamage);
            }
        }
    }
}

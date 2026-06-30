using UnityEngine;

namespace ArenaPrototype
{
    public class AreaPulse : MonoBehaviour
    {
        private ArenaGame game;
        private DamageInfo damage;
        private float radius;
        private float delay;
        private float lifetime;
        private float age;
        private bool detonated;
        private Renderer visualRenderer;
        private Color baseColor;

        public void Setup(ArenaGame owner, DamageInfo info, float pulseRadius, float pulseDelay, float visualLifetime, Color color)
        {
            game = owner;
            damage = info;
            radius = pulseRadius;
            delay = pulseDelay;
            lifetime = visualLifetime;
            visualRenderer = GetComponentInChildren<Renderer>();
            baseColor = color;
            if (visualRenderer != null)
            {
                visualRenderer.material.color = new Color(color.r, color.g, color.b, 0.32f);
            }
        }

        private void Update()
        {
            age += Time.deltaTime;
            float scaleT = Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime));
            float visualScale = Mathf.Lerp(0.15f, radius * 2f, scaleT);
            transform.localScale = new Vector3(visualScale, 0.08f, visualScale);

            if (!detonated && age >= delay)
            {
                Detonate();
            }

            if (visualRenderer != null)
            {
                float alpha = Mathf.Clamp01(1f - age / lifetime) * 0.32f;
                visualRenderer.material.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void Detonate()
        {
            detonated = true;
            if (damage.amount <= 0f)
            {
                return;
            }

            Team targetTeam = damage.sourceTeam == Team.Player ? Team.Enemy : Team.Player;
            game.DamageTeamInRadius(transform.position, radius, targetTeam, damage);
        }
    }
}

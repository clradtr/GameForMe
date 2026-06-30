using UnityEngine;

namespace ArenaPrototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GeneratedSpriteVfx : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color baseColor = Color.white;
        private float lifetime = 0.35f;
        private float age;
        private float startWorldSize = 1f;
        private float endWorldSize = 1f;
        private bool groundAligned;
        private float rollDegrees;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Setup(float duration, float startSize, float endSize, Color tint, bool alignToGround, float roll)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            lifetime = Mathf.Max(0.05f, duration);
            startWorldSize = Mathf.Max(0.05f, startSize);
            endWorldSize = Mathf.Max(0.05f, endSize);
            groundAligned = alignToGround;
            rollDegrees = roll;
            baseColor = tint;
            baseColor.a = Mathf.Clamp01(tint.a <= 0f ? 1f : tint.a);
            age = 0f;

            if (groundAligned)
            {
                transform.rotation = Quaternion.Euler(90f, rollDegrees, 0f);
            }

            ApplyVisuals(0f);
        }

        private void LateUpdate()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / lifetime);

            if (!groundAligned)
            {
                Camera camera = Camera.main;
                if (camera != null)
                {
                    transform.rotation = camera.transform.rotation * Quaternion.Euler(0f, 0f, rollDegrees);
                }
            }

            ApplyVisuals(t);

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void ApplyVisuals(float t)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            float worldSize = Mathf.Lerp(startWorldSize, endWorldSize, t);
            Vector2 boundsSize = spriteRenderer.sprite.bounds.size;
            float spriteSize = Mathf.Max(0.01f, Mathf.Max(boundsSize.x, boundsSize.y));
            float scale = worldSize / spriteSize;
            transform.localScale = new Vector3(scale, scale, scale);

            Color color = baseColor;
            color.a *= 1f - t;
            spriteRenderer.color = color;
        }
    }
}

using UnityEngine;

namespace ArenaPrototype
{
    public class DamageNumber : MonoBehaviour
    {
        private TextMesh textMesh;
        private Vector3 velocity;
        private float lifetime;
        private float age;
        private Color baseColor;

        public void Setup(string text, Color color, float scale)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.characterSize = 0.24f * scale;
            textMesh.fontSize = 46;
            textMesh.color = color;
            baseColor = color;
            lifetime = 0.75f;
            velocity = new Vector3(Random.Range(-0.35f, 0.35f), 1.9f, Random.Range(-0.15f, 0.15f));
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            velocity += Vector3.down * (1.2f * Time.deltaTime);

            Camera camera = Camera.main;
            if (camera != null)
            {
                transform.forward = camera.transform.forward;
            }

            if (textMesh != null)
            {
                float alpha = Mathf.Clamp01(1f - age / lifetime);
                textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;

namespace ArenaPrototype
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteBillboardVisual : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float worldSize = 1f;
        private bool faceCamera = true;
        private bool rotateTowardParentForward;
        private float rollOffsetDegrees;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Setup(float desiredWorldSize, bool shouldFaceCamera, bool shouldRotateTowardParentForward, float rollOffset)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            worldSize = Mathf.Max(0.05f, desiredWorldSize);
            faceCamera = shouldFaceCamera;
            rotateTowardParentForward = shouldRotateTowardParentForward;
            rollOffsetDegrees = rollOffset;
            ApplyScale();
        }

        public void SetWorldSize(float desiredWorldSize)
        {
            worldSize = Mathf.Max(0.05f, desiredWorldSize);
            ApplyScale();
        }

        private void LateUpdate()
        {
            ApplyRotation();
            ApplyScale();
        }

        private void ApplyRotation()
        {
            Camera camera = Camera.main;
            if (faceCamera && camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }

            if (!rotateTowardParentForward || camera == null || transform.parent == null)
            {
                if (rollOffsetDegrees != 0f)
                {
                    transform.Rotate(0f, 0f, rollOffsetDegrees, Space.Self);
                }

                return;
            }

            Vector3 origin = transform.position;
            Vector3 forward = transform.parent.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 screenOrigin = camera.WorldToScreenPoint(origin);
            Vector3 screenAhead = camera.WorldToScreenPoint(origin + forward.normalized);
            Vector2 delta = new Vector2(screenAhead.x - screenOrigin.x, screenAhead.y - screenOrigin.y);
            if (delta.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + rollOffsetDegrees;
            transform.Rotate(0f, 0f, angle, Space.Self);
        }

        private void ApplyScale()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            Vector2 boundsSize = spriteRenderer.sprite.bounds.size;
            float spriteSize = Mathf.Max(0.01f, Mathf.Max(boundsSize.x, boundsSize.y));
            float parentScale = 1f;
            if (transform.parent != null)
            {
                Vector3 lossy = transform.parent.lossyScale;
                parentScale = Mathf.Max(0.01f, Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z)));
            }

            float scale = worldSize / spriteSize / parentScale;
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}

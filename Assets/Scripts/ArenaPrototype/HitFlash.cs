using System.Collections;
using UnityEngine;

namespace ArenaPrototype
{
    public class HitFlash : MonoBehaviour
    {
        private Renderer[] renderers;
        private Color[] baseColors;
        private Coroutine flashRoutine;

        private void Awake()
        {
            CaptureRenderers();
        }

        public void CaptureRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                baseColors[i] = renderers[i].material.color;
            }
        }

        public void Flash(Color color, float duration)
        {
            if (renderers == null || renderers.Length == 0)
            {
                CaptureRenderers();
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine(color, duration));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.color = Color.Lerp(baseColors[i], color, 0.75f);
            }

            yield return new WaitForSeconds(duration);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = baseColors[i];
                }
            }
        }
    }
}

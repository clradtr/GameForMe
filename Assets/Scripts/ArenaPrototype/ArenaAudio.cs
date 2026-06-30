using UnityEngine;

namespace ArenaPrototype
{
    public class ArenaAudio : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip hitClip;
        private AudioClip skillClip;
        private AudioClip pickupClip;
        private AudioClip rewardClip;

        public void Setup()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            hitClip = CreateTone("Hit", 190f, 0.055f, 0.25f);
            skillClip = CreateTone("Skill", 520f, 0.09f, 0.18f);
            pickupClip = CreateTone("Pickup", 860f, 0.12f, 0.16f);
            rewardClip = CreateTone("Reward", 680f, 0.18f, 0.18f);
        }

        public void PlayHit()
        {
            Play(hitClip, 0.75f);
        }

        public void PlaySkill()
        {
            Play(skillClip, 0.9f);
        }

        public void PlayPickup()
        {
            Play(pickupClip, 1f);
        }

        public void PlayReward()
        {
            Play(rewardClip, 1f);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (source != null && clip != null)
            {
                source.pitch = Random.Range(0.96f, 1.04f);
                source.PlayOneShot(clip, volume);
            }
        }

        private AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

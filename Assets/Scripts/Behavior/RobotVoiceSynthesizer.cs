using UnityEngine;

namespace RoboterLego.Behavior
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class RobotVoiceSynthesizer : MonoBehaviour
    {
        [SerializeField] private int sampleRate = 22050;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        public bool CanPlay => audioSource != null;

        public void PlayPattern(string singStyle, float durationSeconds, float energy)
        {
            if (!CanPlay)
            {
                return;
            }

            var clip = BuildClip(singStyle, durationSeconds, Mathf.Clamp01(energy));
            if (clip == null)
            {
                return;
            }

            audioSource.clip = clip;
            audioSource.Play();
        }

        private AudioClip BuildClip(string singStyle, float durationSeconds, float energy)
        {
            int samples = Mathf.Max(1024, Mathf.RoundToInt(durationSeconds * sampleRate));
            float[] buffer = new float[samples];

            var motif = GetMotif(singStyle);
            float toneDuration = Mathf.Max(0.08f, durationSeconds / motif.Length);
            float amplitude = Mathf.Lerp(0.08f, 0.22f, energy);

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                int motifIndex = Mathf.Clamp(Mathf.FloorToInt(t / toneDuration), 0, motif.Length - 1);
                float freq = motif[motifIndex];
                float envelope = Mathf.Clamp01(1f - Mathf.Repeat(t, toneDuration) / toneDuration);
                float carrier = Mathf.Sin(2f * Mathf.PI * freq * t);
                float mod = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t);
                buffer[i] = (carrier * 0.8f + mod * 0.2f) * amplitude * envelope;
            }

            var clip = AudioClip.Create("RobotSing", samples, 1, sampleRate, false);
            clip.SetData(buffer, 0);
            return clip;
        }

        private static float[] GetMotif(string singStyle)
        {
            switch (singStyle)
            {
                case "low_chords":
                    return new[] { 180f, 220f, 180f, 260f, 200f, 160f };
                case "chirp_arps":
                    return new[] { 500f, 700f, 900f, 700f, 500f, 650f };
                case "long_tones":
                    return new[] { 250f, 250f, 320f, 320f, 280f, 280f };
                case "warble_riff":
                    return new[] { 400f, 540f, 420f, 620f, 380f, 690f };
                case "warm_beeps":
                    return new[] { 320f, 380f, 440f, 380f, 320f, 410f };
                default:
                    return new[] { 300f, 420f, 300f, 520f, 300f, 420f };
            }
        }
    }
}

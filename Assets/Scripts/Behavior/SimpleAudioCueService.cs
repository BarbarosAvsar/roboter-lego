using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Behavior
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SimpleAudioCueService : MonoBehaviour, IAudioCueService
    {
        [SerializeField] private AudioClip createCue;
        [SerializeField] private AudioClip generateCue;
        [SerializeField] private AudioClip playCue;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        public void PlayCreateCue()
        {
            Play(createCue, 420f);
        }

        public void PlayGenerateCue()
        {
            Play(generateCue, 560f);
        }

        public void PlayPlayCue()
        {
            Play(playCue, 680f);
        }

        private void Play(AudioClip preferredClip, float fallbackFreq)
        {
            if (audioSource == null)
            {
                return;
            }

            if (preferredClip != null)
            {
                audioSource.PlayOneShot(preferredClip);
                return;
            }

            var clip = BuildFallbackTone(fallbackFreq, 0.12f);
            audioSource.PlayOneShot(clip);
        }

        private static AudioClip BuildFallbackTone(float freq, float duration)
        {
            const int sampleRate = 22050;
            int samples = Mathf.RoundToInt(duration * sampleRate);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.2f;
            }

            var clip = AudioClip.Create("CueTone", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

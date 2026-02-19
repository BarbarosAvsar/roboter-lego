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
        [SerializeField] private float masterVolume = 0.85f;

        private AudioSource cueSource;
        private AudioSource movementLoopSource;
        private AudioSource danceLoopSource;
        private string movementClipKey;
        private string danceClipKey;

        private void Awake()
        {
            cueSource = GetComponent<AudioSource>();
            cueSource.playOnAwake = false;
            cueSource.loop = false;
            cueSource.spatialBlend = 0f;
            cueSource.volume = masterVolume;

            movementLoopSource = CreateLoopSource("MovementLoopSource");
            danceLoopSource = CreateLoopSource("DanceLoopSource");
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

        public void PlayBuildSequence(float intensity)
        {
            if (cueSource == null)
            {
                return;
            }

            cueSource.PlayOneShot(CreateBuildSequenceClip(intensity), masterVolume);
        }

        public void SetMovementLoop(bool isMoving, float speedNormalized)
        {
            if (movementLoopSource == null)
            {
                return;
            }

            if (!isMoving)
            {
                if (movementLoopSource.isPlaying)
                {
                    movementLoopSource.Stop();
                }

                return;
            }

            float clampedSpeed = Mathf.Clamp01(speedNormalized);
            string key = $"move_{Mathf.RoundToInt(clampedSpeed * 5f)}";
            if (movementLoopSource.clip == null || key != movementClipKey)
            {
                movementLoopSource.clip = CreateMovementLoopClip(clampedSpeed);
                movementClipKey = key;
            }

            movementLoopSource.volume = Mathf.Lerp(0.08f, 0.18f, clampedSpeed) * masterVolume;
            movementLoopSource.pitch = Mathf.Lerp(0.88f, 1.28f, clampedSpeed);
            if (!movementLoopSource.isPlaying)
            {
                movementLoopSource.Play();
            }
        }

        public void PlayDanceMusic(string danceStyle, float energy)
        {
            if (danceLoopSource == null)
            {
                return;
            }

            float clampedEnergy = Mathf.Clamp01(energy);
            string normalizedStyle = string.IsNullOrWhiteSpace(danceStyle) ? "spin_bounce" : danceStyle.ToLowerInvariant();
            string key = $"{normalizedStyle}_{Mathf.RoundToInt(clampedEnergy * 5f)}";
            if (danceLoopSource.clip == null || key != danceClipKey)
            {
                danceLoopSource.clip = CreateDanceLoopClip(normalizedStyle, clampedEnergy);
                danceClipKey = key;
            }

            danceLoopSource.volume = Mathf.Lerp(0.08f, 0.22f, clampedEnergy) * masterVolume;
            if (!danceLoopSource.isPlaying)
            {
                danceLoopSource.Play();
            }
        }

        public void StopDanceMusic()
        {
            if (danceLoopSource != null && danceLoopSource.isPlaying)
            {
                danceLoopSource.Stop();
            }
        }

        public AudioClip CreateBuildSequenceClip(float intensity)
        {
            float clampedIntensity = Mathf.Clamp01(intensity);
            return BuildClip(1.1f, (sampleIndex, t) =>
            {
                float hitPattern = Mathf.Repeat(t * 6.5f, 1f);
                float gate = hitPattern < 0.22f ? 1f - (hitPattern / 0.22f) : 0f;
                float drill = Mathf.Sin(2f * Mathf.PI * (45f + clampedIntensity * 35f) * t);
                float metal = Mathf.Sin(2f * Mathf.PI * (420f + Mathf.Sin(t * 21f) * 70f) * t);
                float piston = Mathf.Sin(2f * Mathf.PI * (190f + clampedIntensity * 90f) * t);
                float envelope = Mathf.Clamp01(1f - (t / 1.1f));
                return (metal * 0.42f + piston * 0.35f + drill * 0.23f) * gate * envelope * 0.45f;
            });
        }

        public AudioClip CreateMovementLoopClip(float speedNormalized)
        {
            float speed = Mathf.Clamp01(speedNormalized);
            return BuildClip(0.55f, (sampleIndex, t) =>
            {
                float servo = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 230f, speed) * t);
                float clickPhase = Mathf.Repeat(t * Mathf.Lerp(3f, 8f, speed), 1f);
                float click = clickPhase < 0.08f ? 1f - (clickPhase / 0.08f) : 0f;
                float buzz = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(52f, 95f, speed) * t);
                return (servo * 0.32f + buzz * 0.24f + click * 0.5f) * 0.28f;
            });
        }

        public AudioClip CreateDanceLoopClip(string danceStyle, float energy)
        {
            float[] motif = GetDanceMotif(danceStyle);
            float clampedEnergy = Mathf.Clamp01(energy);
            float stepCount = motif.Length;

            return BuildClip(1.6f, (sampleIndex, t) =>
            {
                float normalized = Mathf.Repeat(t / 1.6f, 1f);
                int step = Mathf.Clamp(Mathf.FloorToInt(normalized * stepCount), 0, motif.Length - 1);
                float freq = motif[step];
                float beat = Mathf.Repeat(normalized * 8f, 1f);
                float kick = beat < 0.10f ? 1f - (beat / 0.10f) : 0f;
                float lead = Mathf.Sin(2f * Mathf.PI * freq * t);
                float sub = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t);
                float shimmer = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.2f;
                return (lead * 0.45f + sub * 0.25f + shimmer + kick * 0.28f) * Mathf.Lerp(0.18f, 0.3f, clampedEnergy);
            });
        }

        private void Play(AudioClip preferredClip, float fallbackFreq)
        {
            if (cueSource == null)
            {
                return;
            }

            if (preferredClip != null)
            {
                cueSource.PlayOneShot(preferredClip, masterVolume);
                return;
            }

            var clip = BuildFallbackTone(fallbackFreq, 0.12f);
            cueSource.PlayOneShot(clip, masterVolume);
        }

        private AudioSource CreateLoopSource(string objectName)
        {
            var sourceObject = new GameObject(objectName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = masterVolume * 0.16f;
            return source;
        }

        private static AudioClip BuildClip(float durationSeconds, System.Func<int, float, float> sampleGenerator)
        {
            const int sampleRate = 22050;
            int samples = Mathf.Max(1024, Mathf.RoundToInt(durationSeconds * sampleRate));
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                data[i] = Mathf.Clamp(sampleGenerator(i, t), -1f, 1f);
            }

            var clip = AudioClip.Create("RobotProcedural", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
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

        private static float[] GetDanceMotif(string danceStyle)
        {
            switch (danceStyle)
            {
                case "stomp":
                    return new[] { 130f, 130f, 180f, 130f, 160f, 210f, 160f, 190f };
                case "jab_step":
                    return new[] { 300f, 460f, 300f, 540f, 360f, 620f, 380f, 540f };
                case "lean_shimmy":
                    return new[] { 220f, 250f, 280f, 250f, 220f, 300f, 270f, 240f };
                case "twirl":
                    return new[] { 360f, 420f, 500f, 580f, 500f, 620f, 700f, 620f };
                default:
                    return new[] { 250f, 330f, 390f, 330f, 280f, 410f, 350f, 300f };
            }
        }
    }
}

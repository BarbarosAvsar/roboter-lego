using System.Collections;
using UnityEngine;

namespace RoboterLego.Behavior
{
    public sealed class VisualPulseController : MonoBehaviour
    {
        [SerializeField] private Transform pulseTarget;
        [SerializeField] private float scaleMultiplier = 1.12f;

        private Coroutine pulseRoutine;

        private void Awake()
        {
            if (pulseTarget == null)
            {
                pulseTarget = transform;
            }
        }

        public void Pulse(float durationSeconds = 0.6f)
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseRoutine(durationSeconds));
        }

        private IEnumerator PulseRoutine(float durationSeconds)
        {
            float half = Mathf.Max(0.05f, durationSeconds * 0.5f);
            Vector3 original = pulseTarget.localScale;
            Vector3 target = original * scaleMultiplier;

            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                pulseTarget.localScale = Vector3.Lerp(original, target, t / half);
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                pulseTarget.localScale = Vector3.Lerp(target, original, t / half);
                yield return null;
            }

            pulseTarget.localScale = original;
            pulseRoutine = null;
        }
    }
}

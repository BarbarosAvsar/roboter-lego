using System.Collections;
using RoboterLego.Assembly;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Behavior
{
    public sealed class RobotBehaviorController : MonoBehaviour, IBehaviorController
    {
        [SerializeField] private float moveSpeed = 1.2f;
        [SerializeField] private float turnSpeed = 120f;
        [SerializeField] private float tiltDeadZone = 0.08f;
        [SerializeField] private RobotVoiceSynthesizer voiceSynthesizer;
        [SerializeField] private VisualPulseController visualPulse;
        [SerializeField] private RobotAssembler robotAssembler;

        private GameObject robotInstance;
        private RobotBlueprint blueprint;
        private Coroutine danceRoutine;
        private Coroutine singRoutine;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsCheckTimer;

        public void BindRobot(GameObject robotInstance, RobotBlueprint blueprint)
        {
            this.robotInstance = robotInstance;
            this.blueprint = blueprint;
            StopAllActions();
        }

        public void TickMovement(Vector2 directionalInput, float deltaTime)
        {
            if (robotInstance == null)
            {
                return;
            }

            if (directionalInput.magnitude < tiltDeadZone)
            {
                SampleFrameRate(deltaTime);
                return;
            }

            Vector2 clamped = Vector2.ClampMagnitude(directionalInput, 1f);
            Vector3 move = new Vector3(clamped.x, 0f, clamped.y) * (moveSpeed * deltaTime);
            robotInstance.transform.position += move;

            if (move.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(move.normalized, Vector3.up);
                robotInstance.transform.rotation = Quaternion.RotateTowards(
                    robotInstance.transform.rotation,
                    target,
                    turnSpeed * deltaTime);
            }

            SampleFrameRate(deltaTime);
        }

        public void PlayDance()
        {
            if (danceRoutine != null)
            {
                StopCoroutine(danceRoutine);
            }

            danceRoutine = StartCoroutine(DanceRoutine());
        }

        public void PlaySing()
        {
            if (singRoutine != null)
            {
                StopCoroutine(singRoutine);
            }

            singRoutine = StartCoroutine(SingRoutine());
        }

        public void StopAllActions()
        {
            if (danceRoutine != null)
            {
                StopCoroutine(danceRoutine);
                danceRoutine = null;
            }

            if (singRoutine != null)
            {
                StopCoroutine(singRoutine);
                singRoutine = null;
            }
        }

        private IEnumerator DanceRoutine()
        {
            float duration = Random.Range(8f, 12f);
            float elapsed = 0f;
            float energy = blueprint?.BehaviorProfile != null ? blueprint.BehaviorProfile.Energy : 0.4f;
            float spinFactor = Mathf.Lerp(45f, 220f, energy);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (robotInstance != null)
                {
                    float wobble = Mathf.Sin(elapsed * 8f) * 0.04f * Mathf.Lerp(0.6f, 1.4f, energy);
                    robotInstance.transform.Rotate(Vector3.up, spinFactor * Time.deltaTime);
                    robotInstance.transform.position += new Vector3(0f, wobble, 0f);
                }

                yield return null;
            }

            danceRoutine = null;
        }

        private IEnumerator SingRoutine()
        {
            float duration = Random.Range(6f, 10f);
            float energy = blueprint?.BehaviorProfile != null ? blueprint.BehaviorProfile.Energy : 0.4f;
            string singStyle = blueprint?.BehaviorProfile?.SingStyle ?? "simple_beeps";

            if (voiceSynthesizer != null && voiceSynthesizer.CanPlay)
            {
                voiceSynthesizer.PlayPattern(singStyle, duration, energy);
            }
            else if (visualPulse != null)
            {
                // Audio fallback: visible pulse keeps interaction feedback intact.
                visualPulse.Pulse(0.8f);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (robotInstance != null)
                {
                    float bob = Mathf.Sin(elapsed * 10f) * 0.003f;
                    robotInstance.transform.position += new Vector3(0f, bob, 0f);
                }

                yield return null;
            }

            singRoutine = null;
        }

        private void SampleFrameRate(float deltaTime)
        {
            fpsAccumulator += deltaTime;
            fpsFrames++;
            fpsCheckTimer += deltaTime;

            if (fpsCheckTimer < 1f || fpsFrames == 0)
            {
                return;
            }

            float avgFps = fpsFrames / Mathf.Max(0.001f, fpsAccumulator);
            if (avgFps < 20f && robotAssembler != null)
            {
                // Low-end fallback: cap accessories before sacrificing movement responsiveness.
                robotAssembler.SetAccessoryBudget(1);
            }
            else if (avgFps >= 24f && robotAssembler != null)
            {
                robotAssembler.SetAccessoryBudget(3);
            }

            fpsAccumulator = 0f;
            fpsFrames = 0;
            fpsCheckTimer = 0f;
        }
    }
}

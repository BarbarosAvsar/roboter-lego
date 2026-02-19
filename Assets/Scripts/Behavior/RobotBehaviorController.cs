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
        [SerializeField] private float maxMoveRadius = 5f;
        [SerializeField] private RobotVoiceSynthesizer voiceSynthesizer;
        [SerializeField] private VisualPulseController visualPulse;
        [SerializeField] private RobotAssembler robotAssembler;
        [SerializeField] private MonoBehaviour audioCueServiceBehaviour;

        private GameObject robotInstance;
        private RobotBlueprint blueprint;
        private Coroutine danceRoutine;
        private Coroutine singRoutine;
        private IAudioCueService audioCueService;
        private float fpsAccumulator;
        private int fpsFrames;
        private float fpsCheckTimer;

        private void Awake()
        {
            audioCueService = audioCueServiceBehaviour as IAudioCueService;
            if (audioCueService == null)
            {
                var services = UnityObjectLookup.FindAll<MonoBehaviour>();
                for (int i = 0; i < services.Length; i++)
                {
                    if (services[i] is IAudioCueService service)
                    {
                        audioCueService = service;
                        if (audioCueServiceBehaviour == null)
                        {
                            audioCueServiceBehaviour = services[i];
                        }

                        break;
                    }
                }
            }
        }

        public void BindRobot(GameObject robotInstance, RobotBlueprint blueprint)
        {
            this.robotInstance = robotInstance;
            this.blueprint = blueprint;
            StopAllActions();
            audioCueService?.SetMovementLoop(false, 0f);
        }

        public void TickMovement(Vector2 directionalInput, float deltaTime)
        {
            if (robotInstance == null)
            {
                return;
            }

            if (directionalInput.magnitude < tiltDeadZone)
            {
                audioCueService?.SetMovementLoop(false, 0f);
                SampleFrameRate(deltaTime);
                return;
            }

            Vector2 clamped = Vector2.ClampMagnitude(directionalInput, 1f);
            Vector3 move = new Vector3(clamped.x, 0f, clamped.y) * (moveSpeed * deltaTime);
            robotInstance.transform.position += move;
            ClampRobotPosition();

            if (move.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(move.normalized, Vector3.up);
                robotInstance.transform.rotation = Quaternion.RotateTowards(
                    robotInstance.transform.rotation,
                    target,
                    turnSpeed * deltaTime);
            }

            audioCueService?.SetMovementLoop(true, clamped.magnitude);
            SampleFrameRate(deltaTime);
        }

        public void PlayDance()
        {
            if (danceRoutine != null)
            {
                StopCoroutine(danceRoutine);
            }

            float energy = blueprint?.BehaviorProfile != null ? blueprint.BehaviorProfile.Energy : 0.4f;
            string danceStyle = blueprint?.BehaviorProfile?.DanceStyle ?? "spin_bounce";
            audioCueService?.PlayDanceMusic(danceStyle, energy);
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

            audioCueService?.SetMovementLoop(false, 0f);
            audioCueService?.StopDanceMusic();
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
                    ClampRobotPosition();
                }

                yield return null;
            }

            danceRoutine = null;
            audioCueService?.StopDanceMusic();
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
                    ClampRobotPosition();
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

        private void ClampRobotPosition()
        {
            if (robotInstance == null || maxMoveRadius <= 0f)
            {
                return;
            }

            Vector3 position = robotInstance.transform.position;
            Vector2 xz = new Vector2(position.x, position.z);
            if (xz.sqrMagnitude > maxMoveRadius * maxMoveRadius)
            {
                xz = xz.normalized * maxMoveRadius;
                robotInstance.transform.position = new Vector3(xz.x, position.y, xz.y);
            }
        }
    }
}

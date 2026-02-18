using System.Collections.Generic;
using RoboterLego.Assembly;
using RoboterLego.Behavior;
using RoboterLego.Domain;
using RoboterLego.Generation;
using RoboterLego.Input;
using RoboterLego.UI;
using UnityEngine;

namespace RoboterLego.Session
{
    public sealed class RobotSessionController : MonoBehaviour, ISessionController
    {
        [Header("Dependencies")]
        [SerializeField] private TouchInputCollector touchInputCollector;
        [SerializeField] private MotionSampler motionSampler;
        [SerializeField] private SwipeFallbackInput swipeFallbackInput;
        [SerializeField] private RobotGenerator robotGenerator;
        [SerializeField] private RobotAssembler robotAssembler;
        [SerializeField] private RobotBehaviorController behaviorController;
        [SerializeField] private ToddlerHudController hudController;
        [SerializeField] private MonoBehaviour audioCueServiceBehaviour;

        [Header("Session Settings")]
        [SerializeField] private float createWindowSeconds = 6f;

        private IAudioCueService audioCueService;
        private InputFeatureExtractor featureExtractor;
        private SessionState currentState = SessionState.Idle;
        private float createTimer;

        private readonly List<TouchEvent> createTouchEvents = new List<TouchEvent>();
        private readonly List<MotionFrame> createMotionFrames = new List<MotionFrame>();
        private readonly List<Vector2> createStrokePoints = new List<Vector2>();

        public SessionState CurrentState => currentState;

        private void Awake()
        {
            audioCueService = audioCueServiceBehaviour as IAudioCueService;
            featureExtractor = new InputFeatureExtractor(new ShapeRecognizer());

            if (hudController != null)
            {
                hudController.DancePressed += HandleDancePressed;
                hudController.SingPressed += HandleSingPressed;
                hudController.NewRobotPressed += HandleNewRobotPressed;
            }

            if (!ValidateDependencies())
            {
                enabled = false;
            }
        }

        private void Start()
        {
            StartSession();
        }

        private void OnDestroy()
        {
            if (hudController != null)
            {
                hudController.DancePressed -= HandleDancePressed;
                hudController.SingPressed -= HandleSingPressed;
                hudController.NewRobotPressed -= HandleNewRobotPressed;
            }
        }

        private void Update()
        {
            switch (currentState)
            {
                case SessionState.Create:
                    TickCreate();
                    break;
                case SessionState.Play:
                    TickPlay();
                    break;
            }
        }

        public void StartSession()
        {
            ClearCreateBuffers();
            createTimer = 0f;
            currentState = SessionState.Create;

            if (touchInputCollector != null)
            {
                touchInputCollector.ClearAll();
            }

            if (hudController != null)
            {
                hudController.SetPlayControlsVisible(false);
            }

            audioCueService?.PlayCreateCue();
        }

        public void ResetSession()
        {
            behaviorController?.StopAllActions();
            robotAssembler?.Clear();
            StartSession();
        }

        private void TickCreate()
        {
            createTimer += Time.deltaTime;

            var touchEvents = touchInputCollector != null ? touchInputCollector.DrainTouchEvents() : new List<TouchEvent>();
            var motionFrames = motionSampler != null ? motionSampler.DrainFrames() : new List<MotionFrame>();
            var strokePoints = touchInputCollector != null ? touchInputCollector.DrainStrokePoints() : new List<Vector2>();

            if (touchEvents.Count > 0)
            {
                createTouchEvents.AddRange(touchEvents);
            }

            if (motionFrames.Count > 0)
            {
                createMotionFrames.AddRange(motionFrames);
            }

            if (strokePoints.Count > 0)
            {
                createStrokePoints.Clear();
                createStrokePoints.AddRange(strokePoints);
            }

            if (createTimer >= createWindowSeconds)
            {
                GenerateAndEnterPlay();
            }
        }

        private void TickPlay()
        {
            var touchEvents = touchInputCollector != null ? touchInputCollector.DrainTouchEvents() : new List<TouchEvent>();
            swipeFallbackInput?.Feed(touchEvents);

            Vector2 directionalInput = Vector2.zero;
            bool hasGyro = motionSampler != null && motionSampler.TryGetDirectionalTilt(out directionalInput);
            if (!hasGyro && swipeFallbackInput != null)
            {
                directionalInput = swipeFallbackInput.GetDirection();
            }

            behaviorController?.TickMovement(directionalInput, Time.deltaTime);
        }

        private void GenerateAndEnterPlay()
        {
            currentState = SessionState.Generate;
            audioCueService?.PlayGenerateCue();

            InputFeatures features = featureExtractor.Extract(createTouchEvents, createStrokePoints, createMotionFrames);
            GenerationSeed seed = robotGenerator.CreateSeed(features);
            // Semi-random magic mode: preserve gesture influence but vary nonce per session.
            seed = new GenerationSeed(seed.FeatureHash, seed.VariationNonce ^ Random.Range(1, int.MaxValue));
            RobotBlueprint blueprint = robotGenerator.Generate(features, seed);
            GameObject robot = robotAssembler.Assemble(blueprint);

            behaviorController.BindRobot(robot, blueprint);

            ClearCreateBuffers();
            currentState = SessionState.Play;
            hudController?.SetPlayControlsVisible(true);
            audioCueService?.PlayPlayCue();
        }

        private bool ValidateDependencies()
        {
            bool ok = true;

            if (touchInputCollector == null)
            {
                Debug.LogError("RobotSessionController: TouchInputCollector is required.");
                ok = false;
            }

            if (robotGenerator == null)
            {
                Debug.LogError("RobotSessionController: RobotGenerator is required.");
                ok = false;
            }

            if (robotAssembler == null)
            {
                Debug.LogError("RobotSessionController: RobotAssembler is required.");
                ok = false;
            }

            if (behaviorController == null)
            {
                Debug.LogError("RobotSessionController: RobotBehaviorController is required.");
                ok = false;
            }

            return ok;
        }

        private void ClearCreateBuffers()
        {
            createTouchEvents.Clear();
            createMotionFrames.Clear();
            createStrokePoints.Clear();
        }

        private void HandleDancePressed()
        {
            if (currentState == SessionState.Play)
            {
                behaviorController?.PlayDance();
            }
        }

        private void HandleSingPressed()
        {
            if (currentState == SessionState.Play)
            {
                behaviorController?.PlaySing();
            }
        }

        private void HandleNewRobotPressed()
        {
            ResetSession();
        }
    }
}

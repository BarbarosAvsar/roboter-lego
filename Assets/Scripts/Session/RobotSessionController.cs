using System.Collections.Generic;
using RoboterLego.Assembly;
using RoboterLego.Behavior;
using RoboterLego.Domain;
using RoboterLego.Generation;
using RoboterLego.Input;
using RoboterLego.UI;
using UnityEngine;
using UnityInput = UnityEngine.Input;

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
        [SerializeField] private RobotCustomizationController customizationController;
        [SerializeField] private RobotBehaviorController behaviorController;
        [SerializeField] private CustomizationInputRouter customizationInputRouter;
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
        private bool hudEventsBound;
        private RobotBlueprint previewBlueprint;

        public SessionState CurrentState => currentState;

        private void Awake()
        {
            ResolveDependencies();
            audioCueService = audioCueServiceBehaviour as IAudioCueService;
            if (audioCueService == null)
            {
                audioCueService = FindAudioCueServiceOnObject() ?? FindAudioCueServiceInScene();
            }

            featureExtractor = new InputFeatureExtractor(new ShapeRecognizer());
            BindHudEvents();

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
            UnbindHudEvents();
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
            behaviorController?.StopAllActions();
            audioCueService?.SetMovementLoop(false, 0f);
            audioCueService?.StopDanceMusic();
            robotAssembler?.Clear();

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
                hudController.SetCreateControlsVisible(true);
            }

            audioCueService?.PlayCreateCue();
            BuildCreatePreviewRobot();
            customizationInputRouter?.SetCreateModeActive(true);
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

            Vector2 keyboardInput = ReadKeyboardDirectionalInput();
            if (keyboardInput.sqrMagnitude > 0.0001f)
            {
                directionalInput += keyboardInput;
                directionalInput = Vector2.ClampMagnitude(directionalInput, 1f);
            }

            behaviorController?.TickMovement(directionalInput, Time.deltaTime);
        }

        private void GenerateAndEnterPlay()
        {
            currentState = SessionState.Generate;
            customizationInputRouter?.SetCreateModeActive(false);
            customizationController?.EndCreate();
            audioCueService?.PlayGenerateCue();

            InputFeatures features = featureExtractor.Extract(createTouchEvents, createStrokePoints, createMotionFrames);
            GenerationSeed seed = robotGenerator.CreateSeed(features);
            // Semi-random magic mode: preserve gesture influence but vary nonce per session.
            seed = new GenerationSeed(seed.FeatureHash, seed.VariationNonce ^ Random.Range(1, int.MaxValue));
            RobotBlueprint blueprint = robotGenerator.Generate(features, seed);
            audioCueService?.PlayBuildSequence(Mathf.Clamp01(features.ShakeEnergy + 0.2f));

            RobotBlueprint customizedBlueprint = customizationController != null
                ? customizationController.ApplyToBlueprint(blueprint)
                : blueprint;
            GameObject robot = robotAssembler.Assemble(customizedBlueprint, customizationController != null ? customizationController.State : null);
            customizationController?.ApplyPresentation(robot);

            behaviorController.BindRobot(robot, customizedBlueprint);

            ClearCreateBuffers();
            currentState = SessionState.Play;
            hudController?.SetPlayControlsVisible(true);
            hudController?.SetCreateControlsVisible(false);
            audioCueService?.PlayPlayCue();
        }

        private void ResolveDependencies()
        {
            if (touchInputCollector == null)
            {
                touchInputCollector = GetComponent<TouchInputCollector>() ?? UnityObjectLookup.FindFirst<TouchInputCollector>();
            }

            if (motionSampler == null)
            {
                motionSampler = GetComponent<MotionSampler>() ?? UnityObjectLookup.FindFirst<MotionSampler>();
            }

            if (swipeFallbackInput == null)
            {
                swipeFallbackInput = GetComponent<SwipeFallbackInput>() ?? UnityObjectLookup.FindFirst<SwipeFallbackInput>();
            }

            if (robotGenerator == null)
            {
                robotGenerator = GetComponent<RobotGenerator>() ?? UnityObjectLookup.FindFirst<RobotGenerator>();
            }

            if (robotAssembler == null)
            {
                robotAssembler = GetComponent<RobotAssembler>() ?? UnityObjectLookup.FindFirst<RobotAssembler>();
            }

            if (behaviorController == null)
            {
                behaviorController = GetComponent<RobotBehaviorController>() ?? UnityObjectLookup.FindFirst<RobotBehaviorController>();
            }

            if (customizationController == null)
            {
                customizationController = GetComponent<RobotCustomizationController>() ?? UnityObjectLookup.FindFirst<RobotCustomizationController>();
            }

            if (customizationInputRouter == null)
            {
                customizationInputRouter = GetComponent<CustomizationInputRouter>() ?? UnityObjectLookup.FindFirst<CustomizationInputRouter>();
            }

            if (hudController == null)
            {
                hudController = UnityObjectLookup.FindFirst<ToddlerHudController>();
            }

            if (audioCueServiceBehaviour == null)
            {
                audioCueServiceBehaviour = FindAudioCueBehaviourInScene();
            }
        }

        private IAudioCueService FindAudioCueServiceOnObject()
        {
            var components = GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IAudioCueService service)
                {
                    return service;
                }
            }

            return null;
        }

        private MonoBehaviour FindAudioCueBehaviourInScene()
        {
            var localComponents = GetComponents<MonoBehaviour>();
            for (int i = 0; i < localComponents.Length; i++)
            {
                if (localComponents[i] is IAudioCueService)
                {
                    return localComponents[i];
                }
            }

            var sceneComponents = UnityObjectLookup.FindAll<MonoBehaviour>();
            for (int i = 0; i < sceneComponents.Length; i++)
            {
                if (sceneComponents[i] is IAudioCueService)
                {
                    return sceneComponents[i];
                }
            }

            return null;
        }

        private IAudioCueService FindAudioCueServiceInScene()
        {
            var sceneComponents = UnityObjectLookup.FindAll<MonoBehaviour>();
            for (int i = 0; i < sceneComponents.Length; i++)
            {
                if (sceneComponents[i] is IAudioCueService service)
                {
                    return service;
                }
            }

            return null;
        }

        private void BindHudEvents()
        {
            if (hudEventsBound || hudController == null)
            {
                return;
            }

            hudController.DancePressed += HandleDancePressed;
            hudController.SingPressed += HandleSingPressed;
            hudController.NewRobotPressed += HandleNewRobotPressed;
            hudController.PartPrevPressed += HandlePartPrevPressed;
            hudController.PartNextPressed += HandlePartNextPressed;
            hudController.ColorPressed += HandleColorPressed;
            hudController.EnvironmentPressed += HandleEnvironmentPressed;
            hudEventsBound = true;
        }

        private void UnbindHudEvents()
        {
            if (!hudEventsBound || hudController == null)
            {
                return;
            }

            hudController.DancePressed -= HandleDancePressed;
            hudController.SingPressed -= HandleSingPressed;
            hudController.NewRobotPressed -= HandleNewRobotPressed;
            hudController.PartPrevPressed -= HandlePartPrevPressed;
            hudController.PartNextPressed -= HandlePartNextPressed;
            hudController.ColorPressed -= HandleColorPressed;
            hudController.EnvironmentPressed -= HandleEnvironmentPressed;
            hudEventsBound = false;
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

            if (motionSampler == null && swipeFallbackInput == null)
            {
                Debug.LogError("RobotSessionController: MotionSampler or SwipeFallbackInput is required.");
                ok = false;
            }

            if (customizationController == null)
            {
                Debug.LogWarning("RobotSessionController: RobotCustomizationController not found; create-phase customization will be unavailable.");
            }

            if (customizationInputRouter == null)
            {
                Debug.LogWarning("RobotSessionController: CustomizationInputRouter not found; non-HUD customization inputs will be unavailable.");
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

        private void HandlePartPrevPressed()
        {
            if (currentState == SessionState.Create)
            {
                customizationController?.CycleSelectedPart(-1);
            }
        }

        private void HandlePartNextPressed()
        {
            if (currentState == SessionState.Create)
            {
                customizationController?.CycleSelectedPart(1);
            }
        }

        private void HandleColorPressed()
        {
            if (currentState == SessionState.Create)
            {
                customizationController?.CycleColor(1);
            }
        }

        private void HandleEnvironmentPressed()
        {
            if (currentState == SessionState.Create)
            {
                customizationController?.CycleEnvironment(1);
            }
        }

        private void BuildCreatePreviewRobot()
        {
            if (robotGenerator == null || robotAssembler == null)
            {
                return;
            }

            var features = BuildFriendlyDefaultFeatures();
            var seed = robotGenerator.CreateSeed(features);
            previewBlueprint = robotGenerator.Generate(features, seed);

            if (customizationController != null)
            {
                customizationController.BeginCreate(previewBlueprint, true);
                audioCueService?.PlayBuildSequence(0.25f);
                return;
            }

            var previewRobot = robotAssembler.Assemble(previewBlueprint);
            behaviorController?.BindRobot(previewRobot, previewBlueprint);
        }

        private static InputFeatures BuildFriendlyDefaultFeatures()
        {
            return new InputFeatures
            {
                TapCount = 2,
                SwipeDirection = SwipeDirection.Right,
                ShapeType = ShapeType.Circle,
                ShakeEnergy = 0.25f,
                HasMeaningfulInput = true
            };
        }

        private static Vector2 ReadKeyboardDirectionalInput()
        {
            float x = 0f;
            float y = 0f;

            if (UnityInput.GetKey(KeyCode.A) || UnityInput.GetKey(KeyCode.LeftArrow))
            {
                x -= 1f;
            }

            if (UnityInput.GetKey(KeyCode.D) || UnityInput.GetKey(KeyCode.RightArrow))
            {
                x += 1f;
            }

            if (UnityInput.GetKey(KeyCode.W) || UnityInput.GetKey(KeyCode.UpArrow))
            {
                y += 1f;
            }

            if (UnityInput.GetKey(KeyCode.S) || UnityInput.GetKey(KeyCode.DownArrow))
            {
                y -= 1f;
            }

            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }
    }
}

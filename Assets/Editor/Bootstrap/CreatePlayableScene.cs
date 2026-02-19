using RoboterLego.Assembly;
using RoboterLego.Assets;
using RoboterLego.Behavior;
using RoboterLego.Domain;
using RoboterLego.Environments;
using RoboterLego.Generation;
using RoboterLego.Input;
using RoboterLego.Session;
using RoboterLego.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoboterLego.Editor.Bootstrap
{
    public static class CreatePlayableScene
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("RoboterLego/Bootstrap/Create Playable Scene")]
        public static void ExecuteMenu()
        {
            Execute();
            Debug.Log($"Playable scene created at {ScenePath}");
        }

        public static void Execute()
        {
            EnsureSceneFolder();
            ContentValidation.ValidateModuleCatalogStyleTags("Assets/Resources/RobotContent/module_catalog.json");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildWorld(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            ConfigureProjectSettings.ApplyForAndroid();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildWorld(Scene scene)
        {
            var appRoot = new GameObject("AppRoot");

            var contentLoader = appRoot.AddComponent<RobotContentLoader>();
            var generator = appRoot.AddComponent<RobotGenerator>();
            var resourceProvider = appRoot.AddComponent<ResourceLegoAssetProvider>();
            var addressablesProvider = appRoot.AddComponent<AddressablesLegoAssetProvider>();
            var proceduralProvider = appRoot.AddComponent<ProceduralLegoAssetProvider>();
            var compositeProvider = appRoot.AddComponent<CompositeLegoAssetProvider>();
            var assembler = appRoot.AddComponent<RobotAssembler>();
            var faceBuilder = appRoot.AddComponent<RobotFaceBuilder>();
            var voice = appRoot.AddComponent<RobotVoiceSynthesizer>();
            var pulse = appRoot.AddComponent<VisualPulseController>();
            var appearance = appRoot.AddComponent<RobotAppearanceController>();
            var environmentController = appRoot.AddComponent<RobotEnvironmentController>();
            var customizationController = appRoot.AddComponent<RobotCustomizationController>();
            var customizationInput = appRoot.AddComponent<CustomizationInputRouter>();
            var behavior = appRoot.AddComponent<RobotBehaviorController>();
            var touch = appRoot.AddComponent<TouchInputCollector>();
            var motion = appRoot.AddComponent<MotionSampler>();
            var swipeFallback = appRoot.AddComponent<SwipeFallbackInput>();
            var audioCue = appRoot.AddComponent<SimpleAudioCueService>();
            var sessionController = appRoot.AddComponent<RobotSessionController>();

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.93f, 0.96f, 1f);
            camera.transform.position = new Vector3(0f, 3.2f, -5.6f);
            camera.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            cameraGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            if (UnityObjectLookup.FindFirst<EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<EventSystem>();
                eventSystemGo.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1200);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var hud = canvasGo.AddComponent<ToddlerHudController>();
            var danceButton = CreateButton(canvasGo.transform, "Dance", new Vector2(0.07f, 0.10f), new Vector2(0.25f, 0.22f), new Color(0.98f, 0.72f, 0.22f));
            var singButton = CreateButton(canvasGo.transform, "Sing", new Vector2(0.28f, 0.10f), new Vector2(0.46f, 0.22f), new Color(0.22f, 0.78f, 0.95f));
            var newRobotButton = CreateButton(canvasGo.transform, "New Robot", new Vector2(0.49f, 0.10f), new Vector2(0.74f, 0.22f), new Color(0.35f, 0.84f, 0.45f));
            var environmentButton = CreateButton(canvasGo.transform, "Environment", new Vector2(0.77f, 0.10f), new Vector2(0.95f, 0.22f), new Color(0.70f, 0.74f, 0.98f));

            var partPrevButton = CreateButton(canvasGo.transform, "Part -", new Vector2(0.05f, 0.80f), new Vector2(0.22f, 0.92f), new Color(0.97f, 0.81f, 0.44f));
            var partNextButton = CreateButton(canvasGo.transform, "Part +", new Vector2(0.24f, 0.80f), new Vector2(0.41f, 0.92f), new Color(0.97f, 0.63f, 0.35f));
            var colorButton = CreateButton(canvasGo.transform, "Color", new Vector2(0.43f, 0.80f), new Vector2(0.60f, 0.92f), new Color(0.40f, 0.88f, 0.58f));

            SetObjectReference(generator, "contentLoader", contentLoader);

            SetObjectReference(assembler, "contentLoader", contentLoader);
            SetObjectReference(assembler, "assetProviderBehaviour", compositeProvider);
            SetObjectReference(assembler, "faceBuilder", faceBuilder);

            SetObjectReference(behavior, "voiceSynthesizer", voice);
            SetObjectReference(behavior, "visualPulse", pulse);
            SetObjectReference(behavior, "robotAssembler", assembler);
            SetObjectReference(behavior, "audioCueServiceBehaviour", audioCue);

            SetObjectReference(hud, "danceButton", danceButton);
            SetObjectReference(hud, "singButton", singButton);
            SetObjectReference(hud, "newRobotButton", newRobotButton);
            SetObjectReference(hud, "partPrevButton", partPrevButton);
            SetObjectReference(hud, "partNextButton", partNextButton);
            SetObjectReference(hud, "colorButton", colorButton);
            SetObjectReference(hud, "environmentButton", environmentButton);

            SetObjectReference(compositeProvider, "resourceProvider", resourceProvider);
            SetObjectReference(compositeProvider, "addressablesProvider", addressablesProvider);
            SetObjectReference(compositeProvider, "proceduralProvider", proceduralProvider);

            SetObjectReference(environmentController, "targetCamera", camera);
            SetObjectReference(environmentController, "directionalLight", light);

            SetObjectReference(customizationController, "robotAssembler", assembler);
            SetObjectReference(customizationController, "appearanceController", appearance);
            SetObjectReference(customizationController, "environmentController", environmentController);

            SetObjectReference(customizationInput, "customizationController", customizationController);
            SetObjectReference(customizationInput, "motionSampler", motion);
            SetObjectReference(customizationInput, "targetCamera", camera);

            SetObjectReference(sessionController, "touchInputCollector", touch);
            SetObjectReference(sessionController, "motionSampler", motion);
            SetObjectReference(sessionController, "swipeFallbackInput", swipeFallback);
            SetObjectReference(sessionController, "robotGenerator", generator);
            SetObjectReference(sessionController, "robotAssembler", assembler);
            SetObjectReference(sessionController, "customizationController", customizationController);
            SetObjectReference(sessionController, "behaviorController", behavior);
            SetObjectReference(sessionController, "customizationInputRouter", customizationInput);
            SetObjectReference(sessionController, "hudController", hud);
            SetObjectReference(sessionController, "audioCueServiceBehaviour", audioCue);
            SetFloat(sessionController, "createWindowSeconds", 6f);

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static Button CreateButton(Transform parent, string title, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var buttonGo = new GameObject($"{title} Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            var image = buttonGo.GetComponent<Image>();
            image.color = color;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelGo.GetComponent<Text>();
            text.text = title;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 28;
            text.resizeTextMaxSize = 72;
            text.fontStyle = FontStyle.Bold;
            text.font = ResolveUiFont();

            return buttonGo.GetComponent<Button>();
        }

        private static Font ResolveUiFont()
        {
            var candidates = new[] { "LegacyRuntime.ttf", "Arial.ttf" };
            foreach (var candidate in candidates)
            {
                try
                {
                    var font = Resources.GetBuiltinResource<Font>(candidate);
                    if (font != null)
                    {
                        return font;
                    }
                }
                catch (System.ArgumentException)
                {
                    // Continue to next fallback font candidate.
                }
            }

            return Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        private static void EnsureSceneFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}

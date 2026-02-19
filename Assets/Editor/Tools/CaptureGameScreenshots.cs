using System;
using System.IO;
using RoboterLego.Assembly;
using RoboterLego.Domain;
using RoboterLego.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoboterLego.Editor.Tools
{
    public static class CaptureGameScreenshots
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const int Width = 1920;
        private const int Height = 1080;

        [MenuItem("RoboterLego/Tools/Capture Screenshots")]
        public static void ExecuteMenu()
        {
            Execute();
        }

        public static void Execute()
        {
            if (!File.Exists(ScenePath))
            {
                throw new FileNotFoundException($"Scene not found: {ScenePath}");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            BuildRobotForCapture();

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = UnityObjectLookup.FindFirst<Camera>();
            }

            if (camera == null)
            {
                throw new InvalidOperationException("No camera found in the scene.");
            }

            string outputDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Screenshots"));
            Directory.CreateDirectory(outputDir);

            SavePoseCapture(
                camera,
                new Vector3(0f, 3.2f, -5.6f),
                Quaternion.Euler(20f, 0f, 0f),
                Path.Combine(outputDir, "game-front.png"));

            SavePoseCapture(
                camera,
                new Vector3(3.6f, 2.9f, -4.8f),
                Quaternion.Euler(20f, -35f, 0f),
                Path.Combine(outputDir, "game-angle.png"));

            SavePoseCapture(
                camera,
                new Vector3(0.6f, 1.9f, -2.7f),
                Quaternion.Euler(12f, -8f, 0f),
                Path.Combine(outputDir, "game-closeup.png"));

            Debug.Log($"Screenshots saved to: {outputDir}");
            AssetDatabase.Refresh();
        }

        private static void BuildRobotForCapture()
        {
            RobotGenerator generator = UnityObjectLookup.FindFirst<RobotGenerator>();
            RobotAssembler assembler = UnityObjectLookup.FindFirst<RobotAssembler>();

            if (generator == null || assembler == null)
            {
                throw new InvalidOperationException("RobotGenerator and RobotAssembler are required in the scene.");
            }

            var features = new InputFeatures
            {
                TapCount = 2,
                SwipeDirection = SwipeDirection.Right,
                ShapeType = ShapeType.Circle,
                ShakeEnergy = 0.35f,
                HasMeaningfulInput = true
            };

            GenerationSeed seed = generator.CreateSeed(features);
            RobotBlueprint blueprint = generator.Generate(features, seed);
            GameObject robot = assembler.Assemble(blueprint);
            if (robot == null)
            {
                throw new InvalidOperationException("Failed to assemble robot for screenshot capture.");
            }
        }

        private static void SavePoseCapture(Camera camera, Vector3 position, Quaternion rotation, string outputPath)
        {
            Vector3 originalPosition = camera.transform.position;
            Quaternion originalRotation = camera.transform.rotation;

            try
            {
                camera.transform.position = position;
                camera.transform.rotation = rotation;
                SaveCameraRender(camera, outputPath);
            }
            finally
            {
                camera.transform.position = originalPosition;
                camera.transform.rotation = originalRotation;
            }
        }

        private static void SaveCameraRender(Camera camera, string outputPath)
        {
            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                try
                {
                    texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                    texture.Apply();

                    byte[] pngData = texture.EncodeToPNG();
                    File.WriteAllBytes(outputPath, pngData);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }
}

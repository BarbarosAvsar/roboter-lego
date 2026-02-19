using System;
using System.IO;
using RoboterLego.Capture;
using RoboterLego.Editor.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoboterLego.Editor.Capture
{
    public static class RobotScreenshotBatch
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

        private static string outputDir;
        private static string markerPath;
        private static DateTime startedAtUtc;
        private static bool captureStarted;
        private static bool previousEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions previousEnterPlayModeOptions;
        private static bool enterPlayModeOptionsPatched;

        [MenuItem("RoboterLego/Tools/Capture 12 Screenshots With UI")]
        public static void CaptureTwelveWithUiMenu()
        {
            CaptureTwelveWithUi();
        }

        public static void CaptureTwelveWithUi()
        {
            outputDir = ResolveOutputDirectory();
            markerPath = Path.Combine(outputDir, "_capture_complete.txt");
            Directory.CreateDirectory(outputDir);
            if (File.Exists(markerPath))
            {
                File.Delete(markerPath);
            }

            if (!File.Exists(ScenePath))
            {
                CreatePlayableScene.Execute();
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            captureStarted = false;
            startedAtUtc = DateTime.UtcNow;
            ConfigureEnterPlayModeOptionsForCapture();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && !captureStarted)
            {
                captureStarted = true;
                RobotScreenshotDriver.RunSequenceAndExit(outputDir);
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode && File.Exists(markerPath))
            {
                FinalizeCaptureFromMarker();
            }
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.isPlaying && !captureStarted)
            {
                captureStarted = true;
                RobotScreenshotDriver.RunSequenceAndExit(outputDir);
                return;
            }

            if (DateTime.UtcNow - startedAtUtc > Timeout)
            {
                CleanupCallbacks();
                RestoreEnterPlayModeOptions();
                Debug.LogError($"Screenshot capture timed out after {Timeout.TotalMinutes} minutes.");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            if (!File.Exists(markerPath))
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            FinalizeCaptureFromMarker();
        }

        private static void FinalizeCaptureFromMarker()
        {
            string markerContent = File.ReadAllText(markerPath);
            CleanupCallbacks();
            RestoreEnterPlayModeOptions();
            AssetDatabase.Refresh();

            bool success = markerContent.StartsWith("success", StringComparison.OrdinalIgnoreCase);
            if (success)
            {
                Debug.Log($"Screenshot capture completed: {outputDir}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }

                return;
            }

            Debug.LogError($"Screenshot capture failed: {markerContent}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void CleanupCallbacks()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private static void ConfigureEnterPlayModeOptionsForCapture()
        {
            if (enterPlayModeOptionsPatched)
            {
                return;
            }

            previousEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            previousEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions | EnterPlayModeOptions.DisableDomainReload;
            enterPlayModeOptionsPatched = true;
        }

        private static void RestoreEnterPlayModeOptions()
        {
            if (!enterPlayModeOptionsPatched)
            {
                return;
            }

            EditorSettings.enterPlayModeOptionsEnabled = previousEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = previousEnterPlayModeOptions;
            enterPlayModeOptionsPatched = false;
        }

        private static string ResolveOutputDirectory()
        {
            string configured = ReadCommandLineArgument("-screenshotOutput");
            if (string.IsNullOrWhiteSpace(configured))
            {
                configured = Path.Combine("Builds", "Screenshots");
            }

            if (Path.IsPathRooted(configured))
            {
                return configured;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, configured));
        }

        private static string ReadCommandLineArgument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}

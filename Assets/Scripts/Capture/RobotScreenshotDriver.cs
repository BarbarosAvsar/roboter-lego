using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using RoboterLego.Assembly;
using RoboterLego.Behavior;
using RoboterLego.Domain;
using RoboterLego.Session;
using UnityEngine;
using UnityEngine.UI;

namespace RoboterLego.Capture
{
    public sealed class RobotScreenshotDriver : MonoBehaviour
    {
        private struct CanvasCaptureState
        {
            public Canvas Canvas;
            public RenderMode RenderMode;
            public Camera WorldCamera;
            public float PlaneDistance;
        }

        private static readonly string[] ShotNames =
        {
            "01_create_default_factory.png",
            "02_create_core_swap_moon.png",
            "03_create_leftarm_swap_neon.png",
            "04_create_rightarm_swap_desert.png",
            "05_create_accessory_swap_arctic.png",
            "06_create_face_ears_swap_factory.png",
            "07_create_color_cycle_1.png",
            "08_create_color_cycle_2.png",
            "09_play_default_ui.png",
            "10_play_dance_ui.png",
            "11_play_move_ui.png",
            "12_play_new_robot_regenerated_ui.png"
        };

        private string outputDirectory;
        private bool running;

        public static void RunSequenceAndExit(string outputDir)
        {
            var existing = UnityObjectLookup.FindFirst<RobotScreenshotDriver>();
            if (existing == null)
            {
                var go = new GameObject("RobotScreenshotDriver");
                existing = go.AddComponent<RobotScreenshotDriver>();
                DontDestroyOnLoad(go);
            }

            existing.Begin(outputDir);
        }

        private void Begin(string outputDir)
        {
            if (running)
            {
                return;
            }

            running = true;
            outputDirectory = ResolveOutputDirectory(outputDir);
            StartCoroutine(CaptureRoutine());
        }

        private IEnumerator CaptureRoutine()
        {
            IEnumerator innerRoutine = CaptureRoutineInternal();
            while (true)
            {
                object yielded;
                try
                {
                    if (!innerRoutine.MoveNext())
                    {
                        WriteCompletionMarker(outputDirectory, true, null);
                        yield break;
                    }

                    yielded = innerRoutine.Current;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    WriteCompletionMarker(outputDirectory, false, ex.ToString());
                    yield break;
                }

                yield return yielded;
            }
        }

        private IEnumerator CaptureRoutineInternal()
        {
            Directory.CreateDirectory(outputDirectory);
            DeleteStaleShots(outputDirectory);
            Screen.SetResolution(1365, 768, false);

            var session = UnityObjectLookup.FindFirst<RobotSessionController>();
            var customization = UnityObjectLookup.FindFirst<RobotCustomizationController>();
            var behavior = UnityObjectLookup.FindFirst<RobotBehaviorController>();
            if (session == null || customization == null || behavior == null)
            {
                throw new InvalidOperationException("Screenshot capture requires RobotSessionController, RobotCustomizationController, and RobotBehaviorController.");
            }

            SetPrivateFloat(session, "createWindowSeconds", 30f);
            session.ResetSession();
            yield return WaitForState(session, SessionState.Create, 5f);
            yield return new WaitForSeconds(0.2f);

            yield return Capture("01_create_default_factory.png");

            customization.SelectSlot(RobotPartSlot.Core);
            customization.CycleSelectedPart(1);
            SetEnvironmentIndex(customization, 1);
            yield return new WaitForSeconds(0.15f);
            yield return Capture("02_create_core_swap_moon.png");

            customization.SelectSlot(RobotPartSlot.LeftArm);
            customization.CycleSelectedPart(1);
            SetEnvironmentIndex(customization, 2);
            yield return new WaitForSeconds(0.15f);
            yield return Capture("03_create_leftarm_swap_neon.png");

            customization.SelectSlot(RobotPartSlot.RightArm);
            customization.CycleSelectedPart(1);
            SetEnvironmentIndex(customization, 3);
            yield return new WaitForSeconds(0.15f);
            yield return Capture("04_create_rightarm_swap_desert.png");

            customization.SelectSlot(RobotPartSlot.TopAccessory);
            customization.CycleSelectedPart(1);
            SetEnvironmentIndex(customization, 4);
            yield return new WaitForSeconds(0.15f);
            yield return Capture("05_create_accessory_swap_arctic.png");

            customization.SelectSlot(RobotPartSlot.Face);
            customization.CycleSelectedPart(1);
            customization.SelectSlot(RobotPartSlot.Ears);
            customization.CycleSelectedPart(1);
            SetEnvironmentIndex(customization, 0);
            yield return new WaitForSeconds(0.15f);
            yield return Capture("06_create_face_ears_swap_factory.png");

            customization.CycleColor(1);
            yield return new WaitForSeconds(0.15f);
            yield return Capture("07_create_color_cycle_1.png");

            customization.CycleColor(1);
            yield return new WaitForSeconds(0.15f);
            yield return Capture("08_create_color_cycle_2.png");

            SetPrivateFloat(session, "createWindowSeconds", 0.2f);
            yield return WaitForState(session, SessionState.Play, 5f);
            yield return new WaitForSeconds(0.2f);
            yield return Capture("09_play_default_ui.png");

            InvokeButton("Dance Button");
            yield return new WaitForSeconds(0.25f);
            yield return Capture("10_play_dance_ui.png");

            for (int i = 0; i < 20; i++)
            {
                behavior.TickMovement(Vector2.right, Time.deltaTime > 0f ? Time.deltaTime : 0.016f);
                yield return null;
            }

            yield return Capture("11_play_move_ui.png");

            InvokeButton("New Robot Button");
            yield return WaitForState(session, SessionState.Play, 8f);
            yield return new WaitForSeconds(0.2f);
            yield return Capture("12_play_new_robot_regenerated_ui.png");
        }

        private IEnumerator Capture(string fileName)
        {
            string path = Path.Combine(outputDirectory, fileName);
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = UnityObjectLookup.FindFirst<Camera>();
            }

            if (camera == null)
            {
                throw new InvalidOperationException("No camera available for screenshot capture.");
            }

            int width = Mathf.Max(Screen.width, 640);
            int height = Mathf.Max(Screen.height, 360);
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            var canvasStates = new List<CanvasCaptureState>();

            try
            {
                PrepareCanvasesForCameraCapture(camera, canvasStates);
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;

                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                byte[] pngData;
                try
                {
                    texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    texture.Apply(false, false);
                    pngData = texture.EncodeToPNG();
                }
                finally
                {
                    UnityEngine.Object.Destroy(texture);
                }

                if (pngData == null || pngData.Length == 0)
                {
                    throw new IOException($"Screenshot data was empty: {path}");
                }

                File.WriteAllBytes(path, pngData);
            }
            finally
            {
                RestoreCanvasesAfterCapture(canvasStates);
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.Destroy(renderTexture);
            }

            if (!File.Exists(path))
            {
                throw new IOException($"Screenshot was not written: {path}");
            }

            var fileInfo = new FileInfo(path);
            if (fileInfo.Length <= 0)
            {
                throw new IOException($"Screenshot file was empty: {path}");
            }

            yield return null;
        }

        private static void PrepareCanvasesForCameraCapture(Camera camera, List<CanvasCaptureState> states)
        {
            var canvases = UnityObjectLookup.FindAll<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }

                states.Add(new CanvasCaptureState
                {
                    Canvas = canvas,
                    RenderMode = canvas.renderMode,
                    WorldCamera = canvas.worldCamera,
                    PlaneDistance = canvas.planeDistance
                });

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
        }

        private static void RestoreCanvasesAfterCapture(List<CanvasCaptureState> states)
        {
            for (int i = 0; i < states.Count; i++)
            {
                CanvasCaptureState state = states[i];
                if (state.Canvas == null)
                {
                    continue;
                }

                state.Canvas.renderMode = state.RenderMode;
                state.Canvas.worldCamera = state.WorldCamera;
                state.Canvas.planeDistance = state.PlaneDistance;
            }
        }

        private static IEnumerator WaitForState(RobotSessionController session, SessionState targetState, float timeoutSeconds)
        {
            float remaining = timeoutSeconds;
            while (session != null && session.CurrentState != targetState && remaining > 0f)
            {
                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (session == null)
            {
                throw new InvalidOperationException("RobotSessionController became unavailable during screenshot capture.");
            }

            if (session.CurrentState != targetState)
            {
                throw new TimeoutException($"Timed out waiting for session state '{targetState}'.");
            }
        }

        private static void InvokeButton(string buttonName)
        {
            var buttons = UnityObjectLookup.FindAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == buttonName)
                {
                    buttons[i].onClick.Invoke();
                    return;
                }
            }

            throw new InvalidOperationException($"Button '{buttonName}' was not found.");
        }

        private static void SetEnvironmentIndex(RobotCustomizationController customizationController, int targetIndex)
        {
            if (customizationController == null)
            {
                return;
            }

            int current = customizationController.State.EnvironmentThemeIndex;
            int delta = targetIndex - current;
            customizationController.CycleEnvironment(delta);
        }

        private static void SetPrivateFloat(object target, string fieldName, float value)
        {
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var field = target.GetType().GetField(fieldName, flags);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().Name, fieldName);
            }

            field.SetValue(target, value);
        }

        private static string ResolveOutputDirectory(string configuredPath)
        {
            string path = string.IsNullOrWhiteSpace(configuredPath) ? Path.Combine("Builds", "Screenshots") : configuredPath;
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, path));
        }

        private static void DeleteStaleShots(string outputDir)
        {
            for (int i = 0; i < ShotNames.Length; i++)
            {
                string path = Path.Combine(outputDir, ShotNames[i]);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            string marker = Path.Combine(outputDir, "_capture_complete.txt");
            if (File.Exists(marker))
            {
                File.Delete(marker);
            }
        }

        private static void WriteCompletionMarker(string outputDir, bool success, string details)
        {
            Directory.CreateDirectory(outputDir);
            string markerPath = Path.Combine(outputDir, "_capture_complete.txt");
            string content = success ? "success" : $"failure:{Environment.NewLine}{details}";
            File.WriteAllText(markerPath, content);
        }
    }
}

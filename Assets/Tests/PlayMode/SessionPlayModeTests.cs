using System.Collections;
using System.Reflection;
using NUnit.Framework;
using RoboterLego.Assembly;
using RoboterLego.Domain;
using RoboterLego.Environments;
using RoboterLego.Input;
using RoboterLego.Session;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RoboterLego.Tests.PlayMode
{
    public sealed class SessionPlayModeTests
    {
        private const string MainSceneName = "Main";

        [UnityTest]
        public IEnumerator BootstrappedScene_ContainsCoreObjects()
        {
            yield return LoadMainScene();

            Assert.NotNull(UnityObjectLookup.FindFirst<RobotSessionController>());
            Assert.NotNull(UnityObjectLookup.FindFirst<RobotAssembler>());
            Assert.NotNull(UnityObjectLookup.FindFirst<MotionSampler>());
            Assert.NotNull(UnityObjectLookup.FindFirst<TouchInputCollector>());
            Assert.NotNull(UnityObjectLookup.FindFirst<Canvas>());
            Assert.NotNull(UnityObjectLookup.FindFirst<RobotCustomizationController>());
            Assert.NotNull(UnityObjectLookup.FindFirst<RobotEnvironmentController>());
            Assert.NotNull(UnityObjectLookup.FindFirst<CustomizationInputRouter>());
        }

        [UnityTest]
        public IEnumerator Session_TransitionsToPlay_AndBuildsRobot()
        {
            yield return LoadMainScene();

            var session = UnityObjectLookup.FindFirst<RobotSessionController>();
            var assembler = UnityObjectLookup.FindFirst<RobotAssembler>();
            Assert.NotNull(session);
            Assert.NotNull(assembler);

            SetPrivateFloat(session, "createWindowSeconds", 0.2f);
            session.ResetSession();
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(SessionState.Play, session.CurrentState);
            Assert.NotNull(assembler.ActiveRobot);
            var colorButton = FindButton("Color Button");
            Assert.IsNull(colorButton);
        }

        [UnityTest]
        public IEnumerator HudButtons_TriggerActions_AndReset()
        {
            yield return LoadMainScene();

            var session = UnityObjectLookup.FindFirst<RobotSessionController>();
            Assert.NotNull(session);

            SetPrivateFloat(session, "createWindowSeconds", 0.2f);
            session.ResetSession();
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(SessionState.Play, session.CurrentState);

            Button dance = FindButton("Dance Button");
            Button sing = FindButton("Sing Button");
            Button reset = FindButton("New Robot Button");
            Assert.NotNull(dance);
            Assert.NotNull(sing);
            Assert.NotNull(reset);

            dance.onClick.Invoke();
            sing.onClick.Invoke();
            yield return new WaitForSeconds(0.2f);

            reset.onClick.Invoke();
            yield return null;
            Assert.AreEqual(SessionState.Create, session.CurrentState);
        }

        [UnityTest]
        public IEnumerator CreatePhase_ContainsPreviewRobot_AndCustomizationButtons()
        {
            yield return LoadMainScene();

            var session = UnityObjectLookup.FindFirst<RobotSessionController>();
            var assembler = UnityObjectLookup.FindFirst<RobotAssembler>();
            Assert.NotNull(session);
            Assert.NotNull(assembler);

            Assert.AreEqual(SessionState.Create, session.CurrentState);
            Assert.NotNull(assembler.ActiveRobot);
            Assert.NotNull(FindButton("Part - Button"));
            Assert.NotNull(FindButton("Part + Button"));
            Assert.NotNull(FindButton("Color Button"));
            Assert.NotNull(FindButton("Environment Button"));
        }

        [UnityTest]
        public IEnumerator CreateControls_CycleCustomizationState()
        {
            yield return LoadMainScene();

            var customization = UnityObjectLookup.FindFirst<RobotCustomizationController>();
            Assert.NotNull(customization);
            Assert.AreEqual(SessionState.Create, UnityObjectLookup.FindFirst<RobotSessionController>().CurrentState);

            int armBefore = customization.State.LeftArmVariantIndex;
            int colorBefore = customization.State.ColorPaletteIndex;
            int environmentBefore = customization.State.EnvironmentThemeIndex;

            FindButton("Part + Button").onClick.Invoke();
            FindButton("Color Button").onClick.Invoke();
            FindButton("Environment Button").onClick.Invoke();
            yield return null;

            Assert.AreNotEqual(armBefore, customization.State.LeftArmVariantIndex);
            Assert.AreNotEqual(colorBefore, customization.State.ColorPaletteIndex);
            Assert.AreNotEqual(environmentBefore, customization.State.EnvironmentThemeIndex);
        }

        [UnityTest]
        public IEnumerator SwipeFallback_EmitsDirection()
        {
            var go = new GameObject("SwipeFallbackTest");
            var fallback = go.AddComponent<SwipeFallbackInput>();

            var events = new[]
            {
                new TouchEvent(TouchEventType.Down, new Vector2(0f, 0f), 0f),
                new TouchEvent(TouchEventType.Up, new Vector2(200f, 0f), 0.1f)
            };

            fallback.Feed(events);
            yield return null;

            Vector2 direction = fallback.GetDirection();
            Assert.AreEqual(Vector2.right, direction);

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Session_UsesSwipeFallback_WhenGyroUnavailable()
        {
            yield return LoadMainScene();

            var session = UnityObjectLookup.FindFirst<RobotSessionController>();
            var motionSampler = UnityObjectLookup.FindFirst<MotionSampler>();
            var swipeFallback = UnityObjectLookup.FindFirst<SwipeFallbackInput>();
            var assembler = UnityObjectLookup.FindFirst<RobotAssembler>();
            Assert.NotNull(session);
            Assert.NotNull(motionSampler);
            Assert.NotNull(swipeFallback);
            Assert.NotNull(assembler);

            SetPrivateFloat(session, "createWindowSeconds", 0.2f);
            session.ResetSession();
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(SessionState.Play, session.CurrentState);
            Assert.NotNull(assembler.ActiveRobot);

            SetPrivateProperty(motionSampler, "HasGyroscope", false);

            Vector3 start = assembler.ActiveRobot.transform.position;
            var events = new[]
            {
                new TouchEvent(TouchEventType.Down, new Vector2(0f, 0f), Time.time),
                new TouchEvent(TouchEventType.Up, new Vector2(180f, 0f), Time.time + 0.1f)
            };

            swipeFallback.Feed(events);
            yield return new WaitForSeconds(0.2f);

            Vector3 end = assembler.ActiveRobot.transform.position;
            Assert.Greater((end - start).sqrMagnitude, 0.0001f);
        }

        [UnityTest]
        public IEnumerator EndToEnd_CreateGeneratePlayResetLoop()
        {
            yield return LoadMainScene();

            var session = UnityObjectLookup.FindFirst<RobotSessionController>();
            Assert.NotNull(session);

            SetPrivateFloat(session, "createWindowSeconds", 0.2f);
            session.ResetSession();
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(SessionState.Play, session.CurrentState);

            FindButton("New Robot Button").onClick.Invoke();
            yield return null;
            Assert.AreEqual(SessionState.Create, session.CurrentState);

            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(SessionState.Play, session.CurrentState);
        }

        private static IEnumerator LoadMainScene()
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
            Assert.NotNull(op, "Scene 'Main' could not be loaded. Run scripts/bootstrap-project.ps1 first.");
            while (!op.isDone)
            {
                yield return null;
            }
        }

        private static Button FindButton(string name)
        {
            var buttons = UnityObjectLookup.FindAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == name)
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private static void SetPrivateFloat(object target, string fieldName, float value)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = target.GetType().GetField(fieldName, flags);
            Assert.NotNull(field, $"Missing private field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static void SetPrivateProperty(object target, string propertyName, object value)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var property = target.GetType().GetProperty(propertyName, flags);
            Assert.NotNull(property, $"Missing property '{propertyName}'.");
            try
            {
                property.SetValue(target, value);
            }
            catch
            {
                // Fallback for private auto-property setter access differences in some Unity runtimes.
                var backingField = target.GetType().GetField($"<{propertyName}>k__BackingField", flags);
                Assert.NotNull(backingField, $"Missing backing field for property '{propertyName}'.");
                backingField.SetValue(target, value);
            }
        }
    }
}

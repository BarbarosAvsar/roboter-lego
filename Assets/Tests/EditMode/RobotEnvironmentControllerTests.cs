using System.Reflection;
using NUnit.Framework;
using RoboterLego.Domain;
using RoboterLego.Environments;
using UnityEngine;

namespace RoboterLego.Tests.EditMode
{
    public sealed class RobotEnvironmentControllerTests
    {
        private GameObject host;
        private RobotEnvironmentController environmentController;
        private Camera cameraComponent;
        private Light lightComponent;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("EnvironmentControllerTests");
            cameraComponent = new GameObject("TestCamera").AddComponent<Camera>();
            lightComponent = new GameObject("TestLight").AddComponent<Light>();
            environmentController = host.AddComponent<RobotEnvironmentController>();
            SetPrivateField(environmentController, "targetCamera", cameraComponent);
            SetPrivateField(environmentController, "directionalLight", lightComponent);
        }

        [TearDown]
        public void TearDown()
        {
            if (cameraComponent != null)
            {
                Object.DestroyImmediate(cameraComponent.gameObject);
            }

            if (lightComponent != null)
            {
                Object.DestroyImmediate(lightComponent.gameObject);
            }

            Object.DestroyImmediate(host);
        }

        [Test]
        public void ApplyThemeIndex_BuildsAllFiveThemes()
        {
            for (int i = 0; i < 5; i++)
            {
                environmentController.ApplyThemeIndex(i);
                Assert.AreEqual((EnvironmentTheme)i, environmentController.CurrentTheme);
                Transform root = host.transform.Find("EnvironmentRoot");
                Assert.NotNull(root, $"Environment root missing for theme {i}");
                Assert.Greater(root.childCount, 0, $"Environment has no geometry for theme {i}");
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = target.GetType().GetField(fieldName, flags);
            Assert.NotNull(field, $"Field '{fieldName}' not found.");
            field.SetValue(target, value);
        }
    }
}

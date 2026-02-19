using NUnit.Framework;
using RoboterLego.Assembly;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Tests.EditMode
{
    public sealed class RobotCustomizationControllerTests
    {
        private GameObject host;
        private RobotCustomizationController controller;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("CustomizationControllerTests");
            controller = host.AddComponent<RobotCustomizationController>();
            controller.ResetState();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void CycleSlot_WrapsVariantIndices()
        {
            controller.CycleSlot(RobotPartSlot.Core, -1);
            Assert.AreEqual(2, controller.State.CoreVariantIndex);

            controller.CycleSlot(RobotPartSlot.Core, 1);
            Assert.AreEqual(0, controller.State.CoreVariantIndex);
        }

        [Test]
        public void CycleColor_AndEnvironment_WrapDeterministically()
        {
            controller.CycleColor(-1);
            Assert.AreEqual(5, controller.State.ColorPaletteIndex);

            controller.CycleEnvironment(-1);
            Assert.AreEqual(4, controller.State.EnvironmentThemeIndex);
        }
    }
}

using NUnit.Framework;
using RoboterLego.Assembly;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Tests.EditMode
{
    public sealed class RobotFaceBuilderTests
    {
        private GameObject host;
        private RobotFaceBuilder faceBuilder;
        private GameObject core;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("FaceBuilderTests");
            faceBuilder = host.AddComponent<RobotFaceBuilder>();
            core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "Core";
            core.transform.SetParent(host.transform, false);
            core.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void BuildFace_CreatesEyesNoseMouthAndEarsMarkers()
        {
            var customization = new RobotCustomizationState
            {
                FaceVariantIndex = 2,
                EarsVariantIndex = 1
            };

            faceBuilder.BuildFace(core.transform, customization);

            Transform faceRoot = core.transform.Find("FaceRoot");
            Transform earsRoot = core.transform.Find("EarsRoot");
            Assert.NotNull(faceRoot);
            Assert.NotNull(earsRoot);
            Assert.NotNull(faceRoot.GetComponent<RobotPartMarker>());
            Assert.NotNull(earsRoot.GetComponent<RobotPartMarker>());
            Assert.NotNull(faceRoot.Find("EyeLeft"));
            Assert.NotNull(faceRoot.Find("EyeRight"));
            Assert.NotNull(faceRoot.Find("Nose"));
            Assert.NotNull(faceRoot.Find("Mouth"));
            Assert.GreaterOrEqual(earsRoot.childCount, 2);
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using RoboterLego.Domain;
using RoboterLego.Input;
using UnityEngine;

namespace RoboterLego.Tests.EditMode
{
    public sealed class InputFeatureExtractorTests
    {
        [Test]
        public void Extract_ComputesExpectedFeatures()
        {
            var recognizer = new ShapeRecognizer();
            var extractor = new InputFeatureExtractor(recognizer);

            var touchEvents = new List<TouchEvent>
            {
                new TouchEvent(TouchEventType.Down, new Vector2(0f, 0f), 0f),
                new TouchEvent(TouchEventType.Move, new Vector2(200f, 0f), 0.1f),
                new TouchEvent(TouchEventType.Up, new Vector2(200f, 0f), 0.2f),
                new TouchEvent(TouchEventType.Tap, new Vector2(10f, 10f), 0.4f),
                new TouchEvent(TouchEventType.Tap, new Vector2(12f, 12f), 0.9f)
            };

            var circlePoints = new List<Vector2>();
            for (int i = 0; i <= 24; i++)
            {
                float angle = i / 24f * Mathf.PI * 2f;
                circlePoints.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 80f);
            }

            var motionFrames = new List<MotionFrame>
            {
                new MotionFrame(Vector3.zero, new Vector3(0f, 0.1f, 0f), 0f),
                new MotionFrame(Vector3.zero, new Vector3(0.9f, 1.2f, 0.2f), 0.1f),
                new MotionFrame(Vector3.zero, new Vector3(-1.1f, -0.7f, 0.3f), 0.2f),
                new MotionFrame(Vector3.zero, new Vector3(1.3f, -0.9f, -0.2f), 0.3f)
            };

            InputFeatures features = extractor.Extract(touchEvents, circlePoints, motionFrames);

            Assert.AreEqual(2, features.TapCount);
            Assert.AreEqual(SwipeDirection.Right, features.SwipeDirection);
            Assert.AreEqual(ShapeType.Circle, features.ShapeType);
            Assert.Greater(features.ShakeEnergy, 0.1f);
            Assert.IsTrue(features.HasMeaningfulInput);
        }
    }
}

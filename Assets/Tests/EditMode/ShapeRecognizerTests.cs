using System.Collections.Generic;
using NUnit.Framework;
using RoboterLego.Domain;
using RoboterLego.Input;
using UnityEngine;

namespace RoboterLego.Tests.EditMode
{
    public sealed class ShapeRecognizerTests
    {
        private ShapeRecognizer recognizer;

        [SetUp]
        public void Setup()
        {
            recognizer = new ShapeRecognizer();
        }

        [Test]
        public void Recognize_Line_ReturnsLine()
        {
            var points = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(30f, 2f),
                new Vector2(60f, 4f),
                new Vector2(90f, 5f),
                new Vector2(120f, 6f)
            };

            ShapeType shape = recognizer.Recognize(points);
            Assert.AreEqual(ShapeType.Line, shape);
        }

        [Test]
        public void Recognize_Triangle_ReturnsTriangle()
        {
            var points = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(50f, 90f),
                new Vector2(100f, 0f),
                new Vector2(0f, 0f)
            };

            ShapeType shape = recognizer.Recognize(points);
            Assert.AreEqual(ShapeType.Triangle, shape);
        }

        [Test]
        public void Recognize_Circle_ReturnsCircle()
        {
            var points = new List<Vector2>();
            for (int i = 0; i <= 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                points.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 120f);
            }

            ShapeType shape = recognizer.Recognize(points);
            Assert.AreEqual(ShapeType.Circle, shape);
        }

        [Test]
        public void Recognize_Swirl_ReturnsSwirl()
        {
            var points = new List<Vector2>();
            for (int i = 0; i < 64; i++)
            {
                float t = i / 63f;
                float angle = t * Mathf.PI * 4.2f;
                float radius = Mathf.Lerp(20f, 120f, t);
                points.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            ShapeType shape = recognizer.Recognize(points);
            Assert.AreEqual(ShapeType.Swirl, shape);
        }
    }
}

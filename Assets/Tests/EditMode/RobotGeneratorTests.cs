using System.Collections.Generic;
using NUnit.Framework;
using RoboterLego.Domain;
using RoboterLego.Generation;
using UnityEngine;

namespace RoboterLego.Tests.EditMode
{
    public sealed class RobotGeneratorTests
    {
        private GameObject testObject;
        private RobotContentLoader loader;
        private RobotGenerator generator;
        private ModuleCatalogData catalog;

        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("GeneratorTestObject");
            loader = testObject.AddComponent<RobotContentLoader>();
            generator = testObject.AddComponent<RobotGenerator>();
            catalog = loader.LoadModuleCatalog();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(testObject);
        }

        [Test]
        public void Generate_SameSeed_ProducesDeterministicBlueprint()
        {
            var features = new InputFeatures
            {
                TapCount = 4,
                SwipeDirection = SwipeDirection.Left,
                ShapeType = ShapeType.Swirl,
                ShakeEnergy = 0.75f,
                HasMeaningfulInput = true
            };

            GenerationSeed seed = generator.CreateSeed(features);
            RobotBlueprint a = generator.Generate(features, seed);
            RobotBlueprint b = generator.Generate(features, seed);

            Assert.AreEqual(a.CoreModuleId, b.CoreModuleId);
            CollectionAssert.AreEqual(a.LimbModuleIds, b.LimbModuleIds);
            CollectionAssert.AreEqual(a.AccessoryModuleIds, b.AccessoryModuleIds);
            Assert.AreEqual(a.BehaviorProfile.MoveStyle, b.BehaviorProfile.MoveStyle);
        }

        [Test]
        public void Generate_OutputsKnownModulesAndValidCounts()
        {
            var features = new InputFeatures
            {
                TapCount = 1,
                SwipeDirection = SwipeDirection.Up,
                ShapeType = ShapeType.Circle,
                ShakeEnergy = 0.2f,
                HasMeaningfulInput = true
            };

            GenerationSeed seed = generator.CreateSeed(features);
            RobotBlueprint blueprint = generator.Generate(features, seed);

            var knownIds = new HashSet<string>();
            for (int i = 0; i < catalog.Modules.Length; i++)
            {
                knownIds.Add(catalog.Modules[i].Id);
            }

            Assert.IsTrue(knownIds.Contains(blueprint.CoreModuleId));
            Assert.LessOrEqual(blueprint.LimbModuleIds.Count, 2);
            Assert.LessOrEqual(blueprint.AccessoryModuleIds.Count, 3);

            for (int i = 0; i < blueprint.LimbModuleIds.Count; i++)
            {
                Assert.IsTrue(knownIds.Contains(blueprint.LimbModuleIds[i]));
            }

            for (int i = 0; i < blueprint.AccessoryModuleIds.Count; i++)
            {
                Assert.IsTrue(knownIds.Contains(blueprint.AccessoryModuleIds[i]));
            }
        }

        [Test]
        public void Generate_TenThousandSeedsAlwaysProduceValidBlueprints()
        {
            var rules = loader.LoadCompatibilityRules();
            var validator = new RobotBlueprintValidator(
                new ModuleCatalogIndex(catalog.Modules),
                new CompatibilityGraph(rules.Rules));
            var random = new System.Random(1337);

            for (int i = 0; i < 10000; i++)
            {
                var features = new InputFeatures
                {
                    TapCount = random.Next(0, 10),
                    SwipeDirection = (SwipeDirection)random.Next(0, 5),
                    ShapeType = (ShapeType)random.Next(0, 6),
                    ShakeEnergy = (float)random.NextDouble(),
                    HasMeaningfulInput = random.NextDouble() > 0.15
                };

                var seed = generator.CreateSeed(features);
                var blueprint = generator.Generate(features, seed);
                bool valid = validator.IsValid(blueprint, out string reason);
                Assert.IsTrue(valid, $"Invalid blueprint at iteration {i}: {reason}");
            }
        }
    }
}

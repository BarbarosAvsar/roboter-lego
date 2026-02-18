using System;
using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Generation
{
    public sealed class RobotGenerator : MonoBehaviour, IGenerator
    {
        [SerializeField] private RobotContentLoader contentLoader;

        private ModuleCatalogIndex moduleCatalog;
        private CompatibilityGraph compatibilityGraph;
        private RobotBlueprintValidator blueprintValidator;

        private void Awake()
        {
            if (contentLoader == null)
            {
                contentLoader = GetComponent<RobotContentLoader>();
            }

            if (contentLoader == null)
            {
                Debug.LogError("RobotContentLoader is required by RobotGenerator.");
                return;
            }

            var modules = contentLoader.LoadModuleCatalog();
            var rules = contentLoader.LoadCompatibilityRules();
            moduleCatalog = new ModuleCatalogIndex(modules.Modules);
            compatibilityGraph = new CompatibilityGraph(rules.Rules);
            blueprintValidator = new RobotBlueprintValidator(moduleCatalog, compatibilityGraph);
        }

        public GenerationSeed CreateSeed(InputFeatures features)
        {
            int shape = (int)features.ShapeType;
            int swipe = (int)features.SwipeDirection;
            int taps = Mathf.Clamp(features.TapCount, 0, 20);
            int energy = Mathf.RoundToInt(Mathf.Clamp01(features.ShakeEnergy) * 1000f);

            int hash = 17;
            hash = hash * 31 + shape;
            hash = hash * 31 + swipe;
            hash = hash * 31 + taps;
            hash = hash * 31 + energy;
            hash = hash * 31 + (features.HasMeaningfulInput ? 1 : 0);

            int nonce = Mathf.Abs(hash * 7919 + 104729);
            return new GenerationSeed(hash, nonce);
        }

        public RobotBlueprint Generate(InputFeatures features, GenerationSeed seed)
        {
            EnsureInitialized();

            if (!features.HasMeaningfulInput)
            {
                features = BuildFriendlyDefaultFeatures();
            }

            var blueprint = GenerateUnvalidated(features, seed);
            if (blueprintValidator.IsValid(blueprint, out _))
            {
                return blueprint;
            }

            // Safety fallback: regenerate from a known toddler-friendly default profile.
            var fallbackFeatures = BuildFriendlyDefaultFeatures();
            var fallbackSeed = CreateSeed(fallbackFeatures);
            var fallbackBlueprint = GenerateUnvalidated(fallbackFeatures, fallbackSeed);
            if (blueprintValidator.IsValid(fallbackBlueprint, out string fallbackReason))
            {
                return fallbackBlueprint;
            }

            throw new InvalidOperationException($"Generator produced invalid blueprint and fallback failed: {fallbackReason}");
        }

        private RobotBlueprint GenerateUnvalidated(InputFeatures features, GenerationSeed seed)
        {
            var random = new System.Random(seed.FeatureHash ^ seed.VariationNonce);
            string styleTag = MapShapeToStyle(features.ShapeType);

            ModuleSpec core = PickModule(ModuleCategory.Core, styleTag, random);
            if (core == null)
            {
                throw new InvalidOperationException("No core modules available.");
            }

            var blueprint = new RobotBlueprint
            {
                CoreModuleId = core.Id,
                BehaviorProfile = BuildBehaviorProfile(features)
            };

            int desiredLimbCount = DesiredLimbCount(features.SwipeDirection);
            int desiredAccessoryCount = DesiredAccessoryCount(features.TapCount, features.ShapeType);

            AttachModules(
                core,
                ModuleCategory.Limb,
                "core_limb_port",
                desiredLimbCount,
                styleTag,
                random,
                blueprint.LimbModuleIds);

            AttachModules(
                core,
                ModuleCategory.Accessory,
                "core_accessory_port",
                desiredAccessoryCount,
                styleTag,
                random,
                blueprint.AccessoryModuleIds);

            return blueprint;
        }

        private void AttachModules(
            ModuleSpec core,
            ModuleCategory category,
            string requiredCoreSocketType,
            int desiredCount,
            string styleTag,
            System.Random random,
            ICollection<string> output)
        {
            if (desiredCount <= 0)
            {
                return;
            }

            int availablePorts = CountSocketsOfType(core, requiredCoreSocketType);
            int maxCount = Mathf.Min(desiredCount, availablePorts);
            if (maxCount == 0)
            {
                return;
            }

            var styleCandidates = moduleCatalog.GetByCategoryAndStyle(category, styleTag);
            var fallbackCandidates = moduleCatalog.GetByCategory(category);

            for (int i = 0; i < maxCount; i++)
            {
                var selected = PickCompatibleModule(styleCandidates, requiredCoreSocketType, random)
                    ?? PickCompatibleModule(fallbackCandidates, requiredCoreSocketType, random);

                if (selected != null)
                {
                    output.Add(selected.Id);
                }
            }
        }

        private ModuleSpec PickModule(ModuleCategory category, string styleTag, System.Random random)
        {
            var styleCandidates = moduleCatalog.GetByCategoryAndStyle(category, styleTag);
            if (styleCandidates.Count > 0)
            {
                return styleCandidates[random.Next(styleCandidates.Count)];
            }

            var fallback = moduleCatalog.GetByCategory(category);
            if (fallback.Count > 0)
            {
                return fallback[random.Next(fallback.Count)];
            }

            return null;
        }

        private ModuleSpec PickCompatibleModule(
            IReadOnlyList<ModuleSpec> candidates,
            string parentSocketType,
            System.Random random)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var compatible = new List<ModuleSpec>();
            for (int i = 0; i < candidates.Count; i++)
            {
                var module = candidates[i];
                if (module?.Sockets == null)
                {
                    continue;
                }

                for (int s = 0; s < module.Sockets.Length; s++)
                {
                    if (compatibilityGraph.IsAllowed(parentSocketType, module.Sockets[s].SocketType))
                    {
                        compatible.Add(module);
                        break;
                    }
                }
            }

            return compatible.Count == 0 ? null : compatible[random.Next(compatible.Count)];
        }

        private static int CountSocketsOfType(ModuleSpec module, string socketType)
        {
            if (module?.Sockets == null || string.IsNullOrEmpty(socketType))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < module.Sockets.Length; i++)
            {
                if (module.Sockets[i].SocketType == socketType)
                {
                    count++;
                }
            }

            return count;
        }

        private static string MapShapeToStyle(ShapeType shapeType)
        {
            switch (shapeType)
            {
                case ShapeType.Circle:
                    return "rounded";
                case ShapeType.Square:
                    return "bulky";
                case ShapeType.Triangle:
                    return "pointed";
                case ShapeType.Line:
                    return "tall";
                case ShapeType.Swirl:
                    return "swirl";
                default:
                    return "rounded";
            }
        }

        private static int DesiredLimbCount(SwipeDirection direction)
        {
            switch (direction)
            {
                case SwipeDirection.Left:
                case SwipeDirection.Right:
                    return 2;
                case SwipeDirection.Up:
                case SwipeDirection.Down:
                    return 2;
                default:
                    return 2;
            }
        }

        private static int DesiredAccessoryCount(int tapCount, ShapeType shapeType)
        {
            int baseCount = 1 + Mathf.Clamp(tapCount / 2, 0, 2);
            if (shapeType == ShapeType.Swirl)
            {
                baseCount = Mathf.Min(baseCount + 1, 3);
            }

            return baseCount;
        }

        private static InputFeatures BuildFriendlyDefaultFeatures()
        {
            return new InputFeatures
            {
                TapCount = 2,
                SwipeDirection = SwipeDirection.Right,
                ShapeType = ShapeType.Circle,
                ShakeEnergy = 0.25f,
                HasMeaningfulInput = true
            };
        }

        private static BehaviorProfile BuildBehaviorProfile(InputFeatures features)
        {
            string moveStyle = features.SwipeDirection switch
            {
                SwipeDirection.Left => "slide_left",
                SwipeDirection.Right => "slide_right",
                SwipeDirection.Up => "march",
                SwipeDirection.Down => "backstep",
                _ => "wobble"
            };

            string danceStyle = features.ShapeType switch
            {
                ShapeType.Circle => "spin_bounce",
                ShapeType.Square => "stomp",
                ShapeType.Triangle => "jab_step",
                ShapeType.Line => "lean_shimmy",
                ShapeType.Swirl => "twirl",
                _ => "gentle_bounce"
            };

            string singStyle = features.ShapeType switch
            {
                ShapeType.Circle => "warm_beeps",
                ShapeType.Square => "low_chords",
                ShapeType.Triangle => "chirp_arps",
                ShapeType.Line => "long_tones",
                ShapeType.Swirl => "warble_riff",
                _ => "simple_beeps"
            };

            return new BehaviorProfile
            {
                MoveStyle = moveStyle,
                DanceStyle = danceStyle,
                SingStyle = singStyle,
                Energy = Mathf.Clamp01(Mathf.Max(0.2f, features.ShakeEnergy))
            };
        }

        private void EnsureInitialized()
        {
            if (moduleCatalog != null && compatibilityGraph != null)
            {
                return;
            }

            Awake();
        }
    }
}

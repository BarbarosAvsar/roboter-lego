using System.Collections.Generic;
using RoboterLego.Assets;
using RoboterLego.Domain;
using RoboterLego.Generation;
using UnityEngine;

namespace RoboterLego.Assembly
{
    public sealed class RobotAssembler : MonoBehaviour
    {
        [SerializeField] private RobotContentLoader contentLoader;
        [SerializeField] private MonoBehaviour assetProviderBehaviour;
        [SerializeField] private RobotFaceBuilder faceBuilder;

        private ILegoAssetProvider assetProvider;
        private ModuleCatalogIndex moduleCatalog;
        private GameObject activeRobot;
        private int accessoryBudget = 3;
        private bool initialized;

        public GameObject ActiveRobot => activeRobot;
        public int FaceVariantCount => faceBuilder != null ? faceBuilder.FaceVariantCount : RobotFaceBuilder.DefaultFaceVariantCount;
        public int EarVariantCount => faceBuilder != null ? faceBuilder.EarVariantCount : RobotFaceBuilder.DefaultEarVariantCount;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        public int GetModuleCount(ModuleCategory category)
        {
            EnsureInitialized();
            return moduleCatalog != null ? moduleCatalog.GetByCategory(category).Count : 0;
        }

        public void SetAccessoryBudget(int count)
        {
            accessoryBudget = Mathf.Clamp(count, 0, 3);
        }

        public GameObject Assemble(RobotBlueprint blueprint)
        {
            return Assemble(blueprint, null);
        }

        public GameObject Assemble(RobotBlueprint blueprint, RobotCustomizationState customizationState)
        {
            EnsureInitialized();
            Clear();

            RobotBlueprint effectiveBlueprint = BuildCustomizedBlueprint(blueprint, customizationState);
            if (effectiveBlueprint == null)
            {
                return null;
            }

            var coreSpec = moduleCatalog.GetById(effectiveBlueprint.CoreModuleId);
            if (coreSpec == null)
            {
                Debug.LogError($"Core module not found: {effectiveBlueprint.CoreModuleId}");
                return null;
            }

            activeRobot = new GameObject("RobotInstance");
            activeRobot.transform.SetParent(transform, false);

            int coreVariant = customizationState != null ? customizationState.CoreVariantIndex : 0;
            var coreGo = InstantiatePrefab(coreSpec, activeRobot.transform, RobotPartSlot.Core, coreVariant, true);
            if (coreGo == null)
            {
                return null;
            }

            AttachLimbModules(coreGo.transform, coreSpec, effectiveBlueprint.LimbModuleIds, customizationState);
            AttachAccessoryModules(coreGo.transform, coreSpec, effectiveBlueprint.AccessoryModuleIds, customizationState);
            faceBuilder?.BuildFace(coreGo.transform, customizationState);

            return activeRobot;
        }

        public RobotBlueprint BuildCustomizedBlueprint(RobotBlueprint blueprint, RobotCustomizationState customizationState)
        {
            EnsureInitialized();
            var customized = RobotBlueprintClone.Clone(blueprint);
            if (customized == null || customizationState == null || moduleCatalog == null)
            {
                return customized;
            }

            var coreModule = GetModuleByVariant(ModuleCategory.Core, customizationState.CoreVariantIndex);
            if (coreModule != null)
            {
                customized.CoreModuleId = coreModule.Id;
            }

            var leftArmModule = GetModuleByVariant(ModuleCategory.Limb, customizationState.LeftArmVariantIndex);
            var rightArmModule = GetModuleByVariant(ModuleCategory.Limb, customizationState.RightArmVariantIndex);
            customized.LimbModuleIds.Clear();
            if (leftArmModule != null)
            {
                customized.LimbModuleIds.Add(leftArmModule.Id);
            }

            if (rightArmModule != null)
            {
                customized.LimbModuleIds.Add(rightArmModule.Id);
            }

            int targetAccessoryCount = Mathf.Max(1, blueprint != null && blueprint.AccessoryModuleIds != null ? blueprint.AccessoryModuleIds.Count : 1);
            var topAccessory = GetModuleByVariant(ModuleCategory.Accessory, customizationState.TopAccessoryVariantIndex);
            customized.AccessoryModuleIds.Clear();
            if (topAccessory != null)
            {
                customized.AccessoryModuleIds.Add(topAccessory.Id);
            }

            var accessories = moduleCatalog.GetByCategory(ModuleCategory.Accessory);
            for (int i = 1; i < targetAccessoryCount; i++)
            {
                if (blueprint != null && blueprint.AccessoryModuleIds != null && i < blueprint.AccessoryModuleIds.Count)
                {
                    customized.AccessoryModuleIds.Add(blueprint.AccessoryModuleIds[i]);
                }
                else if (accessories.Count > 0)
                {
                    customized.AccessoryModuleIds.Add(accessories[i % accessories.Count].Id);
                }
            }

            return customized;
        }

        public void Clear()
        {
            if (activeRobot != null)
            {
                Destroy(activeRobot);
                activeRobot = null;
            }
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            assetProvider = assetProviderBehaviour as ILegoAssetProvider;
            if (assetProvider == null)
            {
                assetProvider = FindFirstProviderOnObject();
                if (assetProvider is MonoBehaviour behaviour)
                {
                    assetProviderBehaviour = behaviour;
                }
            }

            if (assetProvider == null)
            {
                var composite = GetComponent<CompositeLegoAssetProvider>();
                if (composite == null)
                {
                    composite = gameObject.AddComponent<CompositeLegoAssetProvider>();
                }

                assetProvider = composite;
                assetProviderBehaviour = composite;
            }

            if (contentLoader == null)
            {
                contentLoader = GetComponent<RobotContentLoader>();
            }

            if (contentLoader == null)
            {
                Debug.LogError("RobotContentLoader is required by RobotAssembler.");
                return;
            }

            if (faceBuilder == null)
            {
                faceBuilder = GetComponent<RobotFaceBuilder>();
                if (faceBuilder == null)
                {
                    faceBuilder = gameObject.AddComponent<RobotFaceBuilder>();
                }
            }

            var catalog = contentLoader.LoadModuleCatalog();
            moduleCatalog = new ModuleCatalogIndex(catalog.Modules);
            initialized = true;
        }

        private void AttachLimbModules(
            Transform coreTransform,
            ModuleSpec coreSpec,
            IList<string> moduleIds,
            RobotCustomizationState customizationState)
        {
            if (moduleIds == null)
            {
                return;
            }

            var sockets = GetSockets(coreSpec, "core_limb_port");
            int attachCount = Mathf.Min(moduleIds.Count, sockets.Count);
            for (int i = 0; i < attachCount; i++)
            {
                var moduleSpec = moduleCatalog.GetById(moduleIds[i]);
                if (moduleSpec == null)
                {
                    continue;
                }

                RobotPartSlot slot = i == 0 ? RobotPartSlot.LeftArm : RobotPartSlot.RightArm;
                int variant = 0;
                if (customizationState != null)
                {
                    variant = slot == RobotPartSlot.LeftArm
                        ? customizationState.LeftArmVariantIndex
                        : customizationState.RightArmVariantIndex;
                }

                var moduleGo = InstantiatePrefab(moduleSpec, coreTransform, slot, variant, true);
                if (moduleGo == null)
                {
                    continue;
                }

                var socket = sockets[i];
                moduleGo.transform.localPosition = socket.LocalPosition;
                moduleGo.transform.localEulerAngles = socket.LocalEulerAngles;
            }
        }

        private void AttachAccessoryModules(
            Transform coreTransform,
            ModuleSpec coreSpec,
            IList<string> moduleIds,
            RobotCustomizationState customizationState)
        {
            if (moduleIds == null || accessoryBudget <= 0)
            {
                return;
            }

            var sockets = GetSockets(coreSpec, "core_accessory_port");
            int attachCount = Mathf.Min(Mathf.Min(moduleIds.Count, sockets.Count), accessoryBudget);
            for (int i = 0; i < attachCount; i++)
            {
                var moduleSpec = moduleCatalog.GetById(moduleIds[i]);
                if (moduleSpec == null)
                {
                    continue;
                }

                RobotPartSlot? slot = i == 0 ? RobotPartSlot.TopAccessory : (RobotPartSlot?)null;
                int variant = customizationState != null ? customizationState.TopAccessoryVariantIndex : 0;
                var moduleGo = InstantiatePrefab(moduleSpec, coreTransform, slot, variant, i == 0);
                if (moduleGo == null)
                {
                    continue;
                }

                var socket = sockets[i];
                moduleGo.transform.localPosition = socket.LocalPosition;
                moduleGo.transform.localEulerAngles = socket.LocalEulerAngles;
            }
        }

        private static List<SocketSpec> GetSockets(ModuleSpec spec, string socketType)
        {
            var list = new List<SocketSpec>();
            if (spec?.Sockets == null)
            {
                return list;
            }

            for (int i = 0; i < spec.Sockets.Length; i++)
            {
                if (spec.Sockets[i].SocketType == socketType)
                {
                    list.Add(spec.Sockets[i]);
                }
            }

            return list;
        }

        private ModuleSpec GetModuleByVariant(ModuleCategory category, int variantIndex)
        {
            var modules = moduleCatalog.GetByCategory(category);
            if (modules == null || modules.Count == 0)
            {
                return null;
            }

            return modules[PositiveMod(variantIndex, modules.Count)];
        }

        private GameObject InstantiatePrefab(
            ModuleSpec moduleSpec,
            Transform parent,
            RobotPartSlot? markerSlot,
            int markerVariantIndex,
            bool ensureClickableCollider)
        {
            var prefab = assetProvider?.LoadPrefab(moduleSpec.PrefabRef, moduleSpec);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab for module: {moduleSpec.Id}");
                return null;
            }

            var instance = Instantiate(prefab, parent);
            instance.name = moduleSpec.Id;
            instance.SetActive(true);

            if (markerSlot.HasValue)
            {
                var marker = instance.GetComponent<RobotPartMarker>();
                if (marker == null)
                {
                    marker = instance.AddComponent<RobotPartMarker>();
                }

                marker.Configure(markerSlot.Value, markerVariantIndex);
            }

            if (ensureClickableCollider)
            {
                EnsureClickableCollider(instance);
            }

            return instance;
        }

        private static void EnsureClickableCollider(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instance.GetComponentInChildren<Collider>(true) != null)
            {
                return;
            }

            var renderer = instance.GetComponentInChildren<Renderer>(true);
            var collider = instance.AddComponent<BoxCollider>();
            if (renderer != null)
            {
                Vector3 center = instance.transform.InverseTransformPoint(renderer.bounds.center);
                Vector3 size = instance.transform.InverseTransformVector(renderer.bounds.size);
                size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                collider.center = center;
                collider.size = size;
            }
            else
            {
                collider.center = Vector3.zero;
                collider.size = Vector3.one * 0.5f;
            }
        }

        private void EnsureInitialized()
        {
            if (moduleCatalog != null && assetProvider != null)
            {
                return;
            }

            InitializeIfNeeded();
        }

        private ILegoAssetProvider FindFirstProviderOnObject()
        {
            var components = GetComponents<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is ILegoAssetProvider provider)
                {
                    return provider;
                }
            }

            return null;
        }

        private static int PositiveMod(int value, int modulus)
        {
            if (modulus <= 0)
            {
                return 0;
            }

            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}

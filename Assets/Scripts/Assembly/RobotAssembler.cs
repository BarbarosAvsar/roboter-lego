using System.Collections.Generic;
using RoboterLego.Domain;
using RoboterLego.Generation;
using UnityEngine;

namespace RoboterLego.Assembly
{
    public sealed class RobotAssembler : MonoBehaviour
    {
        [SerializeField] private RobotContentLoader contentLoader;
        [SerializeField] private MonoBehaviour assetProviderBehaviour;

        private ILegoAssetProvider assetProvider;
        private ModuleCatalogIndex moduleCatalog;
        private GameObject activeRobot;
        private int accessoryBudget = 3;

        public GameObject ActiveRobot => activeRobot;

        private void Awake()
        {
            assetProvider = assetProviderBehaviour as ILegoAssetProvider;
            if (assetProvider == null)
            {
                Debug.LogError("assetProviderBehaviour must implement ILegoAssetProvider.");
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

            var catalog = contentLoader.LoadModuleCatalog();
            moduleCatalog = new ModuleCatalogIndex(catalog.Modules);
        }

        public void SetAccessoryBudget(int count)
        {
            accessoryBudget = Mathf.Clamp(count, 0, 3);
        }

        public GameObject Assemble(RobotBlueprint blueprint)
        {
            EnsureInitialized();
            Clear();

            if (blueprint == null)
            {
                return null;
            }

            var coreSpec = moduleCatalog.GetById(blueprint.CoreModuleId);
            if (coreSpec == null)
            {
                Debug.LogError($"Core module not found: {blueprint.CoreModuleId}");
                return null;
            }

            activeRobot = new GameObject("RobotInstance");
            activeRobot.transform.SetParent(transform, false);

            var coreGo = InstantiatePrefab(coreSpec, activeRobot.transform);
            if (coreGo == null)
            {
                return null;
            }

            AttachModules(coreGo.transform, coreSpec, blueprint.LimbModuleIds, "core_limb_port", int.MaxValue);
            AttachModules(coreGo.transform, coreSpec, blueprint.AccessoryModuleIds, "core_accessory_port", accessoryBudget);

            return activeRobot;
        }

        public void Clear()
        {
            if (activeRobot != null)
            {
                Destroy(activeRobot);
                activeRobot = null;
            }
        }

        private void AttachModules(
            Transform coreTransform,
            ModuleSpec coreSpec,
            IList<string> moduleIds,
            string requiredSocketType,
            int maxCount)
        {
            if (moduleIds == null || maxCount <= 0)
            {
                return;
            }

            var sockets = GetSockets(coreSpec, requiredSocketType);
            int attachCount = Mathf.Min(Mathf.Min(moduleIds.Count, sockets.Count), maxCount);
            for (int i = 0; i < attachCount; i++)
            {
                var moduleSpec = moduleCatalog.GetById(moduleIds[i]);
                if (moduleSpec == null)
                {
                    continue;
                }

                var moduleGo = InstantiatePrefab(moduleSpec, coreTransform);
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

        private GameObject InstantiatePrefab(ModuleSpec moduleSpec, Transform parent)
        {
            var prefab = assetProvider?.LoadPrefab(moduleSpec.PrefabRef);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab for module: {moduleSpec.Id}");
                return null;
            }

            var instance = Instantiate(prefab, parent);
            instance.name = moduleSpec.Id;
            instance.SetActive(true);
            return instance;
        }

        private void EnsureInitialized()
        {
            if (moduleCatalog != null && assetProvider != null)
            {
                return;
            }

            Awake();
        }
    }
}

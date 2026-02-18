using RoboterLego.Domain;

namespace RoboterLego.Generation
{
    public sealed class RobotBlueprintValidator
    {
        private readonly ModuleCatalogIndex moduleCatalog;
        private readonly CompatibilityGraph compatibilityGraph;

        public RobotBlueprintValidator(ModuleCatalogIndex moduleCatalog, CompatibilityGraph compatibilityGraph)
        {
            this.moduleCatalog = moduleCatalog;
            this.compatibilityGraph = compatibilityGraph;
        }

        public bool IsValid(RobotBlueprint blueprint, out string reason)
        {
            reason = string.Empty;

            if (blueprint == null || string.IsNullOrWhiteSpace(blueprint.CoreModuleId))
            {
                reason = "Blueprint missing core module.";
                return false;
            }

            var core = moduleCatalog.GetById(blueprint.CoreModuleId);
            if (core == null || core.Category != ModuleCategory.Core)
            {
                reason = "Core module invalid or unknown.";
                return false;
            }

            int maxLimb = CountSocketType(core, "core_limb_port");
            int maxAccessory = CountSocketType(core, "core_accessory_port");

            if (blueprint.LimbModuleIds.Count > maxLimb)
            {
                reason = "Too many limb modules for selected core.";
                return false;
            }

            if (blueprint.AccessoryModuleIds.Count > maxAccessory)
            {
                reason = "Too many accessory modules for selected core.";
                return false;
            }

            for (int i = 0; i < blueprint.LimbModuleIds.Count; i++)
            {
                if (!IsValidChild(blueprint.LimbModuleIds[i], ModuleCategory.Limb, "core_limb_port"))
                {
                    reason = $"Invalid limb module at index {i}.";
                    return false;
                }
            }

            for (int i = 0; i < blueprint.AccessoryModuleIds.Count; i++)
            {
                if (!IsValidChild(blueprint.AccessoryModuleIds[i], ModuleCategory.Accessory, "core_accessory_port"))
                {
                    reason = $"Invalid accessory module at index {i}.";
                    return false;
                }
            }

            return true;
        }

        private bool IsValidChild(string id, ModuleCategory category, string coreSocketType)
        {
            var module = moduleCatalog.GetById(id);
            if (module == null || module.Category != category || module.Sockets == null)
            {
                return false;
            }

            for (int i = 0; i < module.Sockets.Length; i++)
            {
                if (compatibilityGraph.IsAllowed(coreSocketType, module.Sockets[i].SocketType))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountSocketType(ModuleSpec module, string socketType)
        {
            if (module?.Sockets == null)
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
    }
}

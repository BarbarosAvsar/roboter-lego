using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Generation
{
    public sealed class RobotContentLoader : MonoBehaviour
    {
        [SerializeField] private string moduleCatalogResourcePath = "RobotContent/module_catalog";
        [SerializeField] private string compatibilityResourcePath = "RobotContent/compatibility_rules";

        public ModuleCatalogData LoadModuleCatalog()
        {
            var textAsset = Resources.Load<TextAsset>(moduleCatalogResourcePath);
            if (textAsset != null)
            {
                var parsed = JsonUtility.FromJson<ModuleCatalogData>(textAsset.text);
                if (parsed != null && parsed.Modules != null && parsed.Modules.Length > 0)
                {
                    return parsed;
                }
            }

            Debug.LogWarning("Module catalog not found or empty. Using fallback in-code catalog.");
            return BuildFallbackCatalog();
        }

        public CompatibilityRulesData LoadCompatibilityRules()
        {
            var textAsset = Resources.Load<TextAsset>(compatibilityResourcePath);
            if (textAsset != null)
            {
                var parsed = JsonUtility.FromJson<CompatibilityRulesData>(textAsset.text);
                if (parsed != null && parsed.Rules != null && parsed.Rules.Length > 0)
                {
                    return parsed;
                }
            }

            Debug.LogWarning("Compatibility rules not found or empty. Using fallback in-code rules.");
            return BuildFallbackRules();
        }

        private static ModuleCatalogData BuildFallbackCatalog()
        {
            return new ModuleCatalogData
            {
                Modules = new[]
                {
                    Core("core_round_01", "rounded", "Assets/Lego/CoreRound01"),
                    Core("core_box_01", "bulky", "Assets/Lego/CoreBox01"),
                    Core("core_tall_01", "tall", "Assets/Lego/CoreTall01"),
                    Limb("limb_point_01", "pointed", "Assets/Lego/LimbPoint01"),
                    Limb("limb_round_01", "rounded", "Assets/Lego/LimbRound01"),
                    Limb("limb_block_01", "bulky", "Assets/Lego/LimbBlock01"),
                    Limb("limb_tall_01", "tall", "Assets/Lego/LimbTall01"),
                    Accessory("acc_antenna_01", "swirl", "Assets/Lego/AccAntenna01"),
                    Accessory("acc_antenna_02", "swirl", "Assets/Lego/AccAntenna02"),
                    Accessory("acc_topper_01", "rounded", "Assets/Lego/AccTopper01"),
                    Accessory("acc_fin_01", "pointed", "Assets/Lego/AccFin01"),
                    Accessory("acc_light_01", "bulky", "Assets/Lego/AccLight01")
                }
            };
        }

        private static CompatibilityRulesData BuildFallbackRules()
        {
            return new CompatibilityRulesData
            {
                Rules = new[]
                {
                    Rule("core_limb_port", "limb_core_socket", true),
                    Rule("core_accessory_port", "accessory_core_socket", true)
                }
            };
        }

        private static ModuleSpec Core(string id, string style, string prefabRef)
        {
            return new ModuleSpec
            {
                Id = id,
                Category = ModuleCategory.Core,
                PrefabRef = prefabRef,
                StyleTags = new[] { style },
                Sockets = new[]
                {
                    Socket("core_limb_1", "core_limb_port", new Vector3(-0.35f, 0f, 0f)),
                    Socket("core_limb_2", "core_limb_port", new Vector3(0.35f, 0f, 0f)),
                    Socket("core_accessory_1", "core_accessory_port", new Vector3(0f, 0.3f, 0f)),
                    Socket("core_accessory_2", "core_accessory_port", new Vector3(0f, 0.55f, 0f))
                }
            };
        }

        private static ModuleSpec Limb(string id, string style, string prefabRef)
        {
            return new ModuleSpec
            {
                Id = id,
                Category = ModuleCategory.Limb,
                PrefabRef = prefabRef,
                StyleTags = new[] { style },
                Sockets = new[]
                {
                    Socket("limb_core", "limb_core_socket", Vector3.zero)
                }
            };
        }

        private static ModuleSpec Accessory(string id, string style, string prefabRef)
        {
            return new ModuleSpec
            {
                Id = id,
                Category = ModuleCategory.Accessory,
                PrefabRef = prefabRef,
                StyleTags = new[] { style },
                Sockets = new[]
                {
                    Socket("acc_core", "accessory_core_socket", Vector3.zero)
                }
            };
        }

        private static SocketSpec Socket(string id, string type, Vector3 localPosition)
        {
            return new SocketSpec
            {
                SocketId = id,
                SocketType = type,
                LocalPosition = localPosition,
                LocalEulerAngles = Vector3.zero
            };
        }

        private static CompatibilityRule Rule(string a, string b, bool allowed)
        {
            return new CompatibilityRule
            {
                SocketTypeA = a,
                SocketTypeB = b,
                Allowed = allowed
            };
        }
    }
}

using System;
using System.IO;
using RoboterLego.Generation;
using UnityEditor;
using UnityEngine;

namespace RoboterLego.Editor.Bootstrap
{
    public static class ContentValidation
    {
        [MenuItem("RoboterLego/Validate/Module Catalog Style Tags")]
        public static void ValidateModuleCatalogStyleTagsMenu()
        {
            ValidateModuleCatalogStyleTags("Assets/Resources/RobotContent/module_catalog.json");
            Debug.Log("Module catalog validation succeeded.");
        }

        public static void ValidateModuleCatalogStyleTags(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new InvalidOperationException($"Missing module catalog at '{jsonPath}'.");
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<ModuleCatalogData>(json);
            if (data?.Modules == null || data.Modules.Length == 0)
            {
                throw new InvalidOperationException("Module catalog is empty.");
            }

            for (int i = 0; i < data.Modules.Length; i++)
            {
                var module = data.Modules[i];
                if (module == null)
                {
                    throw new InvalidOperationException($"Module at index {i} is null.");
                }

                if (string.IsNullOrWhiteSpace(module.Id))
                {
                    throw new InvalidOperationException($"Module at index {i} has no id.");
                }

                if (module.StyleTags == null || module.StyleTags.Length == 0)
                {
                    throw new InvalidOperationException($"Module '{module.Id}' has no style tags.");
                }

                bool hasValidStyle = false;
                for (int s = 0; s < module.StyleTags.Length; s++)
                {
                    if (!string.IsNullOrWhiteSpace(module.StyleTags[s]))
                    {
                        hasValidStyle = true;
                        break;
                    }
                }

                if (!hasValidStyle)
                {
                    throw new InvalidOperationException($"Module '{module.Id}' only contains empty style tags.");
                }
            }
        }
    }
}

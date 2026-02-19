using System;
using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Assets
{
    public sealed class ProceduralLegoAssetProvider : MonoBehaviour, ILegoAssetProvider
    {
        [SerializeField] private Material overrideMaterial;

        private readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        public GameObject LoadPrefab(string prefabRef, ModuleSpec moduleSpec = null)
        {
            string cacheKey = string.IsNullOrWhiteSpace(prefabRef) ? $"proc:{moduleSpec?.Id ?? "unknown"}" : prefabRef;
            if (cache.TryGetValue(cacheKey, out var prefab))
            {
                return prefab;
            }

            var generated = BuildPrefab(moduleSpec);
            generated.name = $"Proc_{moduleSpec?.Id ?? cacheKey}";
            generated.SetActive(false);
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(generated);
            }
            cache[cacheKey] = generated;
            return generated;
        }

        private GameObject BuildPrefab(ModuleSpec moduleSpec)
        {
            string style = ResolveStyle(moduleSpec);
            var root = new GameObject("ProceduralLegoModule");

            Color color = ResolveColor(moduleSpec?.Category ?? ModuleCategory.Accessory, style);
            Material material = overrideMaterial != null ? overrideMaterial : BuildMaterial(color);

            switch (moduleSpec?.Category ?? ModuleCategory.Accessory)
            {
                case ModuleCategory.Core:
                    BuildCore(root.transform, style, material);
                    break;
                case ModuleCategory.Limb:
                    BuildLimb(root.transform, style, material);
                    break;
                case ModuleCategory.Accessory:
                default:
                    BuildAccessory(root.transform, style, material);
                    break;
            }

            return root;
        }

        private static string ResolveStyle(ModuleSpec moduleSpec)
        {
            if (moduleSpec?.StyleTags != null && moduleSpec.StyleTags.Length > 0 && !string.IsNullOrWhiteSpace(moduleSpec.StyleTags[0]))
            {
                return moduleSpec.StyleTags[0].ToLowerInvariant();
            }

            return "rounded";
        }

        private static void BuildCore(Transform parent, string style, Material material)
        {
            Vector3 bodyScale = style switch
            {
                "bulky" => new Vector3(0.8f, 0.5f, 0.7f),
                "tall" => new Vector3(0.5f, 0.85f, 0.5f),
                _ => new Vector3(0.62f, 0.55f, 0.62f)
            };

            CreateBrick(parent, new Vector3(0f, 0f, 0f), bodyScale, material);
            CreateStudStrip(parent, 2, style == "tall" ? 3 : 2, new Vector3(0f, bodyScale.y * 0.5f + 0.06f, 0f), material);
        }

        private static void BuildLimb(Transform parent, string style, Material material)
        {
            Vector3 mainScale = style switch
            {
                "pointed" => new Vector3(0.2f, 0.45f, 0.2f),
                "tall" => new Vector3(0.18f, 0.55f, 0.18f),
                "bulky" => new Vector3(0.28f, 0.4f, 0.28f),
                _ => new Vector3(0.22f, 0.36f, 0.22f)
            };

            CreateBrick(parent, new Vector3(0f, -0.02f, 0f), mainScale, material);
            if (style == "pointed")
            {
                CreateCone(parent, new Vector3(0f, mainScale.y * 0.5f + 0.02f, 0f), new Vector3(0.12f, 0.22f, 0.12f), material);
            }
            else
            {
                CreateStudStrip(parent, 1, 1, new Vector3(0f, mainScale.y * 0.5f + 0.06f, 0f), material);
            }
        }

        private static void BuildAccessory(Transform parent, string style, Material material)
        {
            switch (style)
            {
                case "swirl":
                    CreateStudStrip(parent, 1, 1, new Vector3(0f, 0.08f, 0f), material);
                    CreateBrick(parent, new Vector3(0f, 0.2f, 0f), new Vector3(0.07f, 0.36f, 0.07f), material);
                    CreateBrick(parent, new Vector3(0.09f, 0.38f, 0f), new Vector3(0.07f, 0.07f, 0.07f), material);
                    break;
                case "pointed":
                    CreateCone(parent, new Vector3(0f, 0.16f, 0f), new Vector3(0.18f, 0.28f, 0.18f), material);
                    break;
                case "bulky":
                    CreateBrick(parent, new Vector3(0f, 0.1f, 0f), new Vector3(0.28f, 0.2f, 0.28f), material);
                    CreateStudStrip(parent, 2, 1, new Vector3(0f, 0.24f, 0f), material);
                    break;
                default:
                    CreateBrick(parent, new Vector3(0f, 0.1f, 0f), new Vector3(0.2f, 0.2f, 0.2f), material);
                    CreateStudStrip(parent, 1, 1, new Vector3(0f, 0.24f, 0f), material);
                    break;
            }
        }

        private static Color ResolveColor(ModuleCategory category, string style)
        {
            return category switch
            {
                ModuleCategory.Core => style switch
                {
                    "bulky" => new Color(0.11f, 0.53f, 0.93f),
                    "tall" => new Color(0.12f, 0.72f, 0.54f),
                    _ => new Color(0.95f, 0.42f, 0.25f)
                },
                ModuleCategory.Limb => style switch
                {
                    "pointed" => new Color(0.91f, 0.65f, 0.17f),
                    "tall" => new Color(0.32f, 0.36f, 0.89f),
                    _ => new Color(0.8f, 0.25f, 0.63f)
                },
                _ => style switch
                {
                    "swirl" => new Color(0.95f, 0.85f, 0.2f),
                    "pointed" => new Color(0.86f, 0.3f, 0.28f),
                    _ => new Color(0.65f, 0.76f, 0.83f)
                }
            };
        }

        private static Material BuildMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private static void CreateBrick(Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            ApplyMaterial(go, material);
        }

        private static void CreateStudStrip(Transform parent, int xCount, int zCount, Vector3 offset, Material material)
        {
            float spacing = 0.15f;
            float startX = -(xCount - 1) * spacing * 0.5f;
            float startZ = -(zCount - 1) * spacing * 0.5f;
            for (int x = 0; x < xCount; x++)
            {
                for (int z = 0; z < zCount; z++)
                {
                    var stud = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    stud.transform.SetParent(parent, false);
                    stud.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);
                    stud.transform.localPosition = new Vector3(startX + x * spacing, offset.y, startZ + z * spacing) + new Vector3(offset.x, 0f, offset.z);
                    ApplyMaterial(stud, material);
                }
            }
        }

        private static void CreateCone(Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            // Primitive cone substitute using a scaled cylinder for broad compatibility.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            ApplyMaterial(go, material);
        }

        private static void ApplyMaterial(GameObject go, Material material)
        {
            if (material == null)
            {
                return;
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}

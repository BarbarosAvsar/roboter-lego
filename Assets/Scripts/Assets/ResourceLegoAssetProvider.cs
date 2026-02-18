using System;
using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Assets
{
    [Serializable]
    public sealed class LegoPrefabEntry
    {
        public string PrefabRef;
        public GameObject Prefab;
    }

    public sealed class ResourceLegoAssetProvider : MonoBehaviour, ILegoAssetProvider
    {
        [SerializeField] private List<LegoPrefabEntry> explicitMappings = new List<LegoPrefabEntry>();
        [SerializeField] private Material placeholderMaterial;

        private readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private GameObject placeholderPrefab;

        private void Awake()
        {
            for (int i = 0; i < explicitMappings.Count; i++)
            {
                var item = explicitMappings[i];
                if (item == null || string.IsNullOrWhiteSpace(item.PrefabRef) || item.Prefab == null)
                {
                    continue;
                }

                cache[item.PrefabRef] = item.Prefab;
            }
        }

        public GameObject LoadPrefab(string prefabRef)
        {
            if (string.IsNullOrWhiteSpace(prefabRef))
            {
                return GetPlaceholder();
            }

            if (cache.TryGetValue(prefabRef, out var prefab))
            {
                return prefab;
            }

            var resourcePrefab = Resources.Load<GameObject>(prefabRef);
            if (resourcePrefab != null)
            {
                cache[prefabRef] = resourcePrefab;
                return resourcePrefab;
            }

            Debug.LogWarning($"Prefab '{prefabRef}' not found in mappings/resources. Using placeholder.");
            return GetPlaceholder();
        }

        private GameObject GetPlaceholder()
        {
            if (placeholderPrefab != null)
            {
                return placeholderPrefab;
            }

            placeholderPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholderPrefab.name = "LegoPlaceholderPrefab";
            placeholderPrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            var collider = placeholderPrefab.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            if (placeholderMaterial != null)
            {
                var renderer = placeholderPrefab.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = placeholderMaterial;
                }
            }

            placeholderPrefab.SetActive(false);
            DontDestroyOnLoad(placeholderPrefab);
            return placeholderPrefab;
        }
    }
}

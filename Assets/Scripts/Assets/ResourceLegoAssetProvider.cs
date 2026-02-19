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

        private readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

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

        public GameObject LoadPrefab(string prefabRef, ModuleSpec moduleSpec = null)
        {
            if (string.IsNullOrWhiteSpace(prefabRef))
            {
                return null;
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
            return null;
        }
    }
}

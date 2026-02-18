using System;
using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoboterLego.Assets
{
    public sealed class AddressablesLegoAssetProvider : MonoBehaviour, ILegoAssetProvider
    {
        private readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        public GameObject LoadPrefab(string prefabRef)
        {
            if (string.IsNullOrWhiteSpace(prefabRef))
            {
                return null;
            }

            if (cache.TryGetValue(prefabRef, out var prefab))
            {
                return prefab;
            }

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(prefabRef);
            GameObject loaded = handle.WaitForCompletion();
            if (loaded != null)
            {
                cache[prefabRef] = loaded;
                return loaded;
            }

            Debug.LogWarning($"Addressable prefab not found: {prefabRef}");
            return null;
        }

        private void OnDestroy()
        {
            cache.Clear();
        }
    }
}

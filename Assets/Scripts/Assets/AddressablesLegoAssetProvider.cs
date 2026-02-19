using System;
using System.Collections.Generic;
using System.IO;
using RoboterLego.Domain;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoboterLego.Assets
{
    public sealed class AddressablesLegoAssetProvider : MonoBehaviour, ILegoAssetProvider
    {
        private readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private bool availabilityChecked;
        private bool addressablesAvailable = true;

        public GameObject LoadPrefab(string prefabRef, ModuleSpec moduleSpec = null)
        {
            if (string.IsNullOrWhiteSpace(prefabRef))
            {
                return null;
            }

            if (!IsAddressablesAvailable())
            {
                return null;
            }

            if (cache.TryGetValue(prefabRef, out var prefab))
            {
                return prefab;
            }

            try
            {
                var locationsHandle = Addressables.LoadResourceLocationsAsync(prefabRef, typeof(GameObject));
                var locations = locationsHandle.WaitForCompletion();
                bool hasLocations = locations != null && locations.Count > 0;
                Addressables.Release(locationsHandle);
                if (!hasLocations)
                {
                    return null;
                }

                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(prefabRef);
                GameObject loaded = handle.WaitForCompletion();
                if (loaded != null)
                {
                    cache[prefabRef] = loaded;
                    return loaded;
                }
            }
            catch (Exception)
            {
                // Addressables not configured or key missing; chain provider handles fallback.
            }
            return null;
        }

        private bool IsAddressablesAvailable()
        {
#if UNITY_EDITOR
            if (!availabilityChecked)
            {
                availabilityChecked = true;
                addressablesAvailable = HasEditorRuntimeData();
            }
#endif
            return addressablesAvailable;
        }

#if UNITY_EDITOR
        private static bool HasEditorRuntimeData()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string addressablesRoot = Path.Combine(projectRoot, "Library", "com.unity.addressables", "aa");
            if (!Directory.Exists(addressablesRoot))
            {
                return false;
            }

            return Directory.GetFiles(addressablesRoot, "settings.json", SearchOption.AllDirectories).Length > 0;
        }
#endif

        private void OnDestroy()
        {
            cache.Clear();
        }
    }
}

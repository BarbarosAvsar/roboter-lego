using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Assets
{
    public sealed class CompositeLegoAssetProvider : MonoBehaviour, ILegoAssetProviderChain
    {
        [SerializeField] private ResourceLegoAssetProvider resourceProvider;
        [SerializeField] private AddressablesLegoAssetProvider addressablesProvider;
        [SerializeField] private ProceduralLegoAssetProvider proceduralProvider;

        private readonly List<ILegoAssetProvider> providers = new List<ILegoAssetProvider>();

        public IReadOnlyList<ILegoAssetProvider> Providers => providers;

        private void Awake()
        {
            EnsureProviders();
        }

        public GameObject LoadPrefab(string prefabRef, ModuleSpec moduleSpec = null)
        {
            EnsureProviders();
            for (int i = 0; i < providers.Count; i++)
            {
                var provider = providers[i];
                if (provider == null)
                {
                    continue;
                }

                var prefab = provider.LoadPrefab(prefabRef, moduleSpec);
                if (prefab != null)
                {
                    return prefab;
                }
            }

            return null;
        }

        private void EnsureProviders()
        {
            if (providers.Count > 0)
            {
                return;
            }

            if (resourceProvider == null)
            {
                resourceProvider = GetComponent<ResourceLegoAssetProvider>();
                if (resourceProvider == null)
                {
                    resourceProvider = gameObject.AddComponent<ResourceLegoAssetProvider>();
                }
            }

            if (addressablesProvider == null)
            {
                addressablesProvider = GetComponent<AddressablesLegoAssetProvider>();
                if (addressablesProvider == null)
                {
                    addressablesProvider = gameObject.AddComponent<AddressablesLegoAssetProvider>();
                }
            }

            if (proceduralProvider == null)
            {
                proceduralProvider = GetComponent<ProceduralLegoAssetProvider>();
                if (proceduralProvider == null)
                {
                    proceduralProvider = gameObject.AddComponent<ProceduralLegoAssetProvider>();
                }
            }

            providers.Add(resourceProvider);
            providers.Add(addressablesProvider);
            providers.Add(proceduralProvider);
        }
    }
}

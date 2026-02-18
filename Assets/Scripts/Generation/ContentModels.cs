using System;
using RoboterLego.Domain;

namespace RoboterLego.Generation
{
    [Serializable]
    public sealed class ModuleCatalogData
    {
        public ModuleSpec[] Modules;
    }

    [Serializable]
    public sealed class CompatibilityRulesData
    {
        public CompatibilityRule[] Rules;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using RoboterLego.Domain;

namespace RoboterLego.Generation
{
    public sealed class ModuleCatalogIndex
    {
        private readonly Dictionary<string, ModuleSpec> byId;
        private readonly Dictionary<ModuleCategory, List<ModuleSpec>> byCategory;

        public ModuleCatalogIndex(IEnumerable<ModuleSpec> modules)
        {
            byId = new Dictionary<string, ModuleSpec>(StringComparer.OrdinalIgnoreCase);
            byCategory = new Dictionary<ModuleCategory, List<ModuleSpec>>();

            foreach (ModuleCategory category in Enum.GetValues(typeof(ModuleCategory)))
            {
                byCategory[category] = new List<ModuleSpec>();
            }

            if (modules == null)
            {
                return;
            }

            foreach (var module in modules)
            {
                if (module == null || string.IsNullOrWhiteSpace(module.Id))
                {
                    continue;
                }

                byId[module.Id] = module;
                byCategory[module.Category].Add(module);
            }
        }

        public ModuleSpec GetById(string id)
        {
            return id != null && byId.TryGetValue(id, out var module) ? module : null;
        }

        public IReadOnlyList<ModuleSpec> GetByCategory(ModuleCategory category)
        {
            return byCategory[category];
        }

        public IReadOnlyList<ModuleSpec> GetByCategoryAndStyle(ModuleCategory category, string styleTag)
        {
            if (string.IsNullOrEmpty(styleTag))
            {
                return GetByCategory(category);
            }

            return byCategory[category]
                .Where(m => m.StyleTags != null && m.StyleTags.Any(s => string.Equals(s, styleTag, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }
}

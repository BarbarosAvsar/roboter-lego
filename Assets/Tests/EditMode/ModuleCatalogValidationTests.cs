using NUnit.Framework;
using RoboterLego.Generation;
using UnityEngine;

namespace RoboterLego.Tests.EditMode
{
    public sealed class ModuleCatalogValidationTests
    {
        [Test]
        public void ModuleCatalog_AllModulesHaveAtLeastOneStyleTag()
        {
            var host = new GameObject("ModuleCatalogValidationTests");
            var loader = host.AddComponent<RobotContentLoader>();
            var catalog = loader.LoadModuleCatalog();
            Assert.NotNull(catalog);
            Assert.NotNull(catalog.Modules);
            Assert.Greater(catalog.Modules.Length, 0);

            for (int i = 0; i < catalog.Modules.Length; i++)
            {
                var module = catalog.Modules[i];
                Assert.NotNull(module, $"Module at index {i} is null.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(module.Id), $"Module at index {i} has no id.");
                Assert.NotNull(module.StyleTags, $"Module '{module.Id}' has null style tags.");
                Assert.Greater(module.StyleTags.Length, 0, $"Module '{module.Id}' has no style tags.");

                bool hasNonEmpty = false;
                for (int s = 0; s < module.StyleTags.Length; s++)
                {
                    if (!string.IsNullOrWhiteSpace(module.StyleTags[s]))
                    {
                        hasNonEmpty = true;
                        break;
                    }
                }

                Assert.IsTrue(hasNonEmpty, $"Module '{module.Id}' has only empty style tags.");
            }

            Object.DestroyImmediate(host);
        }
    }
}

using RoboterLego.Assembly;
using UnityEngine;

namespace RoboterLego.Behavior
{
    public sealed class RobotAppearanceController : MonoBehaviour
    {
        [System.Serializable]
        private struct Palette
        {
            public Color body;
            public Color accent;
            public Color detail;
        }

        [SerializeField] private Palette[] palettes;

        private MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        public int PaletteCount => ActivePalettes.Length;

        public int WrapPaletteIndex(int value)
        {
            return PositiveMod(value, PaletteCount);
        }

        public void ApplyPalette(GameObject robot, int paletteIndex)
        {
            if (robot == null)
            {
                return;
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            var selectedPalette = ActivePalettes[WrapPaletteIndex(paletteIndex)];
            var renderers = robot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color color = ResolveColorForRenderer(renderer, selectedPalette);
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_Color", color);
                propertyBlock.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private Palette[] ActivePalettes
        {
            get
            {
                if (palettes != null && palettes.Length > 0)
                {
                    return palettes;
                }

                return DefaultPalettes;
            }
        }

        private static Color ResolveColorForRenderer(Renderer renderer, Palette palette)
        {
            var marker = renderer.GetComponentInParent<RobotPartMarker>();
            if (marker == null)
            {
                return palette.body;
            }

            switch (marker.Slot)
            {
                case Domain.RobotPartSlot.TopAccessory:
                case Domain.RobotPartSlot.Ears:
                    return palette.accent;
                case Domain.RobotPartSlot.Face:
                    return palette.detail;
                default:
                    return palette.body;
            }
        }

        private static int PositiveMod(int value, int modulus)
        {
            if (modulus <= 0)
            {
                return 0;
            }

            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static readonly Palette[] DefaultPalettes =
        {
            new Palette
            {
                body = new Color(0.92f, 0.42f, 0.28f),
                accent = new Color(0.78f, 0.32f, 0.88f),
                detail = new Color(0.70f, 0.86f, 0.95f)
            },
            new Palette
            {
                body = new Color(0.22f, 0.62f, 0.91f),
                accent = new Color(0.95f, 0.72f, 0.19f),
                detail = new Color(0.87f, 0.92f, 0.98f)
            },
            new Palette
            {
                body = new Color(0.18f, 0.77f, 0.55f),
                accent = new Color(0.95f, 0.34f, 0.40f),
                detail = new Color(0.86f, 0.92f, 0.95f)
            },
            new Palette
            {
                body = new Color(0.70f, 0.72f, 0.80f),
                accent = new Color(0.16f, 0.22f, 0.38f),
                detail = new Color(0.94f, 0.94f, 0.96f)
            },
            new Palette
            {
                body = new Color(0.93f, 0.83f, 0.34f),
                accent = new Color(0.22f, 0.56f, 0.32f),
                detail = new Color(0.90f, 0.95f, 0.91f)
            },
            new Palette
            {
                body = new Color(0.38f, 0.28f, 0.85f),
                accent = new Color(0.96f, 0.48f, 0.18f),
                detail = new Color(0.90f, 0.92f, 1.00f)
            }
        };
    }
}

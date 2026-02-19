using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Environments
{
    public sealed class RobotEnvironmentController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Light directionalLight;
        [SerializeField] private float floorSize = 18f;

        private GameObject environmentRoot;

        public EnvironmentTheme CurrentTheme { get; private set; } = EnvironmentTheme.RobotFactory;
        public int ThemeCount => 5;

        private void Awake()
        {
            ResolveDependencies();
            ApplyTheme(CurrentTheme);
        }

        public int WrapThemeIndex(int value)
        {
            return PositiveMod(value, ThemeCount);
        }

        public void ApplyThemeIndex(int index)
        {
            ApplyTheme((EnvironmentTheme)WrapThemeIndex(index));
        }

        public void CycleTheme(int delta)
        {
            ApplyThemeIndex((int)CurrentTheme + delta);
        }

        public void ApplyTheme(EnvironmentTheme theme)
        {
            ResolveDependencies();
            CurrentTheme = theme;

            if (environmentRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(environmentRoot);
                }
                else
                {
                    DestroyImmediate(environmentRoot);
                }
            }

            environmentRoot = new GameObject("EnvironmentRoot");
            environmentRoot.transform.SetParent(transform, false);

            BuildFloor(environmentRoot.transform, ResolveFloorColor(theme));
            BuildProps(environmentRoot.transform, theme);
            ApplyLighting(theme);
        }

        private void BuildProps(Transform parent, EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.MoonStation:
                    BuildMoonStation(parent);
                    break;
                case EnvironmentTheme.NeonLab:
                    BuildNeonLab(parent);
                    break;
                case EnvironmentTheme.DesertScrapyard:
                    BuildDesertScrapyard(parent);
                    break;
                case EnvironmentTheme.ArcticHangar:
                    BuildArcticHangar(parent);
                    break;
                default:
                    BuildRobotFactory(parent);
                    break;
            }
        }

        private void BuildRobotFactory(Transform parent)
        {
            for (int i = -2; i <= 2; i++)
            {
                CreateProp(parent, PrimitiveType.Cube, new Vector3(i * 1.4f, 0.7f, 4.6f), new Vector3(0.8f, 1.4f, 0.8f), new Color(0.42f, 0.45f, 0.48f));
                CreateProp(parent, PrimitiveType.Cylinder, new Vector3(i * 1.4f, 1.55f, 4.6f), new Vector3(0.12f, 0.32f, 0.12f), new Color(0.86f, 0.68f, 0.24f));
            }
        }

        private void BuildMoonStation(Transform parent)
        {
            CreateProp(parent, PrimitiveType.Sphere, new Vector3(-3.8f, 1.2f, 5.2f), new Vector3(1.8f, 1.8f, 1.8f), new Color(0.77f, 0.77f, 0.80f));
            CreateProp(parent, PrimitiveType.Cylinder, new Vector3(3.4f, 1.5f, 4.8f), new Vector3(1.2f, 0.3f, 1.2f), new Color(0.86f, 0.89f, 0.92f));
            for (int i = 0; i < 8; i++)
            {
                float x = -4f + i * 1.1f;
                CreateProp(parent, PrimitiveType.Cylinder, new Vector3(x, 0.15f, 3.8f), new Vector3(0.09f, 0.15f, 0.09f), new Color(0.53f, 0.56f, 0.60f));
            }
        }

        private void BuildNeonLab(Transform parent)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = -2.8f + i * 1.9f;
                CreateProp(parent, PrimitiveType.Cube, new Vector3(x, 1.3f, 4.6f), new Vector3(0.25f, 2.6f, 0.25f), new Color(0.20f, 0.85f, 0.95f));
                CreateProp(parent, PrimitiveType.Cube, new Vector3(x, 0.05f, 2.5f), new Vector3(0.25f, 0.1f, 5.2f), new Color(0.98f, 0.30f, 0.56f));
            }
        }

        private void BuildDesertScrapyard(Transform parent)
        {
            for (int i = 0; i < 6; i++)
            {
                float x = -3.5f + i * 1.4f;
                float y = 0.25f + (i % 3) * 0.2f;
                float z = 3.9f + (i % 2) * 1.1f;
                CreateProp(parent, PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(0.7f, 0.45f, 0.7f), new Color(0.56f, 0.43f, 0.31f));
                CreateProp(parent, PrimitiveType.Cylinder, new Vector3(x, y + 0.42f, z), new Vector3(0.14f, 0.08f, 0.14f), new Color(0.75f, 0.53f, 0.21f));
            }
        }

        private void BuildArcticHangar(Transform parent)
        {
            for (int i = -2; i <= 2; i++)
            {
                CreateProp(parent, PrimitiveType.Cylinder, new Vector3(i * 1.5f, 1.2f, 4.9f), new Vector3(0.18f, 1.2f, 0.18f), new Color(0.71f, 0.81f, 0.90f));
                CreateProp(parent, PrimitiveType.Cube, new Vector3(i * 1.5f, 2.45f, 4.9f), new Vector3(0.7f, 0.12f, 0.7f), new Color(0.90f, 0.97f, 1.00f));
            }
        }

        private void ApplyLighting(EnvironmentTheme theme)
        {
            if (targetCamera != null)
            {
                targetCamera.backgroundColor = ResolveSkyColor(theme);
            }

            if (directionalLight != null)
            {
                directionalLight.color = ResolveLightColor(theme);
                directionalLight.intensity = ResolveLightIntensity(theme);
            }
        }

        private void BuildFloor(Transform parent, Color color)
        {
            CreateProp(parent, PrimitiveType.Cube, new Vector3(0f, -0.05f, 0f), new Vector3(floorSize, 0.1f, floorSize), color);
        }

        private static void CreateProp(Transform parent, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = BuildMaterial(color);
            }
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

            var material = shader != null ? new Material(shader) : null;
            if (material != null)
            {
                material.color = color;
            }

            return material;
        }

        private void ResolveDependencies()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (directionalLight == null)
            {
                directionalLight = UnityObjectLookup.FindFirst<Light>();
            }
        }

        private static Color ResolveSkyColor(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.MoonStation:
                    return new Color(0.08f, 0.10f, 0.15f);
                case EnvironmentTheme.NeonLab:
                    return new Color(0.06f, 0.07f, 0.13f);
                case EnvironmentTheme.DesertScrapyard:
                    return new Color(0.85f, 0.73f, 0.52f);
                case EnvironmentTheme.ArcticHangar:
                    return new Color(0.78f, 0.90f, 0.98f);
                default:
                    return new Color(0.86f, 0.90f, 0.96f);
            }
        }

        private static Color ResolveFloorColor(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.MoonStation:
                    return new Color(0.34f, 0.36f, 0.41f);
                case EnvironmentTheme.NeonLab:
                    return new Color(0.13f, 0.15f, 0.20f);
                case EnvironmentTheme.DesertScrapyard:
                    return new Color(0.60f, 0.46f, 0.31f);
                case EnvironmentTheme.ArcticHangar:
                    return new Color(0.80f, 0.89f, 0.95f);
                default:
                    return new Color(0.48f, 0.50f, 0.53f);
            }
        }

        private static Color ResolveLightColor(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.MoonStation:
                    return new Color(0.78f, 0.86f, 1.00f);
                case EnvironmentTheme.NeonLab:
                    return new Color(0.75f, 0.82f, 0.98f);
                case EnvironmentTheme.DesertScrapyard:
                    return new Color(1.00f, 0.88f, 0.68f);
                case EnvironmentTheme.ArcticHangar:
                    return new Color(0.90f, 0.97f, 1.00f);
                default:
                    return Color.white;
            }
        }

        private static float ResolveLightIntensity(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.MoonStation:
                    return 0.85f;
                case EnvironmentTheme.NeonLab:
                    return 0.95f;
                case EnvironmentTheme.DesertScrapyard:
                    return 1.05f;
                case EnvironmentTheme.ArcticHangar:
                    return 1.2f;
                default:
                    return 1.15f;
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
    }
}

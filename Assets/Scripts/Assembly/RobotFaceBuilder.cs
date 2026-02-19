using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Assembly
{
    public sealed class RobotFaceBuilder : MonoBehaviour
    {
        public const int DefaultFaceVariantCount = 4;
        public const int DefaultEarVariantCount = 4;

        public int FaceVariantCount => DefaultFaceVariantCount;
        public int EarVariantCount => DefaultEarVariantCount;

        public void BuildFace(Transform coreTransform, RobotCustomizationState customizationState)
        {
            if (coreTransform == null)
            {
                return;
            }

            var bounds = CalculateLocalBounds(coreTransform);
            if (bounds.extents.sqrMagnitude < 0.0001f)
            {
                bounds = new Bounds(Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f));
            }

            int faceVariant = PositiveMod(customizationState != null ? customizationState.FaceVariantIndex : 0, FaceVariantCount);
            int earsVariant = PositiveMod(customizationState != null ? customizationState.EarsVariantIndex : 0, EarVariantCount);
            Material sharedMaterial = ResolveSharedMaterial(coreTransform);

            var faceRoot = new GameObject("FaceRoot");
            faceRoot.transform.SetParent(coreTransform, false);
            faceRoot.transform.localPosition = new Vector3(0f, bounds.center.y + bounds.extents.y * 0.05f, bounds.extents.z + 0.06f);
            var faceMarker = faceRoot.AddComponent<RobotPartMarker>();
            faceMarker.Configure(RobotPartSlot.Face, faceVariant);
            var faceCollider = faceRoot.AddComponent<BoxCollider>();
            faceCollider.center = Vector3.zero;
            faceCollider.size = new Vector3(bounds.extents.x * 1.15f, bounds.extents.y * 0.95f, 0.2f);

            BuildEyesNoseMouth(faceRoot.transform, faceVariant, bounds, sharedMaterial);

            var earsRoot = new GameObject("EarsRoot");
            earsRoot.transform.SetParent(coreTransform, false);
            earsRoot.transform.localPosition = Vector3.zero;
            var earsMarker = earsRoot.AddComponent<RobotPartMarker>();
            earsMarker.Configure(RobotPartSlot.Ears, earsVariant);
            var earCollider = earsRoot.AddComponent<BoxCollider>();
            earCollider.center = new Vector3(0f, bounds.center.y + bounds.extents.y * 0.35f, 0f);
            earCollider.size = new Vector3(bounds.extents.x * 2.1f, bounds.extents.y * 0.9f, bounds.extents.z * 1.2f);

            BuildEars(earsRoot.transform, earsVariant, bounds, sharedMaterial);
        }

        private static void BuildEyesNoseMouth(Transform parent, int variant, Bounds bounds, Material material)
        {
            float width = Mathf.Max(0.12f, bounds.extents.x * 0.7f);
            float eyeY = bounds.extents.y * 0.2f;
            float eyeSpacing = width * 0.6f;
            float eyeRadius = 0.08f;
            float mouthY = -bounds.extents.y * 0.2f;

            switch (variant)
            {
                case 1:
                    eyeY = bounds.extents.y * 0.3f;
                    eyeSpacing = width * 0.7f;
                    eyeRadius = 0.07f;
                    mouthY = -bounds.extents.y * 0.3f;
                    break;
                case 2:
                    eyeY = bounds.extents.y * 0.1f;
                    eyeSpacing = width * 0.5f;
                    eyeRadius = 0.09f;
                    mouthY = -bounds.extents.y * 0.12f;
                    break;
                case 3:
                    eyeY = bounds.extents.y * 0.27f;
                    eyeSpacing = width * 0.55f;
                    eyeRadius = 0.06f;
                    mouthY = -bounds.extents.y * 0.36f;
                    break;
            }

            CreatePart(parent, "EyeLeft", PrimitiveType.Sphere, new Vector3(-eyeSpacing * 0.5f, eyeY, 0f), Vector3.one * eyeRadius, material);
            CreatePart(parent, "EyeRight", PrimitiveType.Sphere, new Vector3(eyeSpacing * 0.5f, eyeY, 0f), Vector3.one * eyeRadius, material);
            CreatePart(parent, "Nose", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.01f), new Vector3(0.07f, 0.11f, 0.06f), material);

            if (variant == 2)
            {
                CreatePart(parent, "Mouth", PrimitiveType.Cylinder, new Vector3(0f, mouthY, 0.01f), new Vector3(0.11f, 0.03f, 0.11f), material);
            }
            else
            {
                CreatePart(parent, "Mouth", PrimitiveType.Cube, new Vector3(0f, mouthY, 0.01f), new Vector3(0.26f, 0.04f, 0.05f), material);
            }
        }

        private static void BuildEars(Transform parent, int variant, Bounds bounds, Material material)
        {
            float earX = bounds.extents.x + 0.08f;
            float earY = bounds.center.y + bounds.extents.y * 0.28f;

            switch (variant)
            {
                case 1:
                    CreatePart(parent, "EarLeft", PrimitiveType.Cylinder, new Vector3(-earX, earY, 0f), new Vector3(0.08f, 0.13f, 0.08f), material);
                    CreatePart(parent, "EarRight", PrimitiveType.Cylinder, new Vector3(earX, earY, 0f), new Vector3(0.08f, 0.13f, 0.08f), material);
                    break;
                case 2:
                    CreatePart(parent, "EarLeft", PrimitiveType.Cube, new Vector3(-earX, earY, 0f), new Vector3(0.11f, 0.23f, 0.08f), material);
                    CreatePart(parent, "EarRight", PrimitiveType.Cube, new Vector3(earX, earY, 0f), new Vector3(0.11f, 0.23f, 0.08f), material);
                    CreatePart(parent, "EarLeftTip", PrimitiveType.Cylinder, new Vector3(-earX, earY + 0.15f, 0f), new Vector3(0.04f, 0.09f, 0.04f), material);
                    CreatePart(parent, "EarRightTip", PrimitiveType.Cylinder, new Vector3(earX, earY + 0.15f, 0f), new Vector3(0.04f, 0.09f, 0.04f), material);
                    break;
                case 3:
                    CreatePart(parent, "EarLeft", PrimitiveType.Cylinder, new Vector3(-earX, earY + 0.02f, 0f), new Vector3(0.05f, 0.2f, 0.05f), material);
                    CreatePart(parent, "EarRight", PrimitiveType.Cylinder, new Vector3(earX, earY + 0.02f, 0f), new Vector3(0.05f, 0.2f, 0.05f), material);
                    CreatePart(parent, "EarLeftCap", PrimitiveType.Sphere, new Vector3(-earX, earY + 0.24f, 0f), Vector3.one * 0.08f, material);
                    CreatePart(parent, "EarRightCap", PrimitiveType.Sphere, new Vector3(earX, earY + 0.24f, 0f), Vector3.one * 0.08f, material);
                    break;
                default:
                    CreatePart(parent, "EarLeft", PrimitiveType.Cube, new Vector3(-earX, earY, 0f), new Vector3(0.12f, 0.14f, 0.08f), material);
                    CreatePart(parent, "EarRight", PrimitiveType.Cube, new Vector3(earX, earY, 0f), new Vector3(0.12f, 0.14f, 0.08f), material);
                    break;
            }
        }

        private static GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            if (material != null)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                }
            }

            return go;
        }

        private static Bounds CalculateLocalBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            bool initialized = false;
            Bounds worldBounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!initialized)
                {
                    worldBounds = renderers[i].bounds;
                    initialized = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderers[i].bounds);
                }
            }

            Vector3 localCenter = root.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = root.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            return new Bounds(localCenter, localSize);
        }

        private static Material ResolveSharedMaterial(Transform root)
        {
            var renderer = root.GetComponentInChildren<Renderer>(true);
            return renderer != null ? renderer.sharedMaterial : null;
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

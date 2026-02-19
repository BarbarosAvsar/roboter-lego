using RoboterLego.Behavior;
using RoboterLego.Domain;
using RoboterLego.Environments;
using RoboterLego.Generation;
using UnityEngine;

namespace RoboterLego.Assembly
{
    public sealed class RobotCustomizationController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private RobotAssembler robotAssembler;
        [SerializeField] private RobotAppearanceController appearanceController;
        [SerializeField] private RobotEnvironmentController environmentController;

        [Header("Fallback Variant Counts")]
        [SerializeField] private int fallbackCoreVariants = 3;
        [SerializeField] private int fallbackLimbVariants = 4;
        [SerializeField] private int fallbackAccessoryVariants = 4;

        private readonly RobotCustomizationState state = new RobotCustomizationState();
        private RobotBlueprint createBaseBlueprint;

        public RobotCustomizationState State => state;
        public bool IsCreateMode { get; private set; }

        private void Awake()
        {
            ResolveDependencies();
            state.Reset();
            ApplyEnvironmentSelection();
        }

        public void BeginCreate(RobotBlueprint baseBlueprint, bool resetState)
        {
            ResolveDependencies();
            IsCreateMode = true;
            createBaseBlueprint = RobotBlueprintClone.Clone(baseBlueprint);
            if (resetState)
            {
                state.Reset();
            }

            ApplyEnvironmentSelection();
            RebuildPreview();
        }

        public void EndCreate()
        {
            IsCreateMode = false;
        }

        public void ResetState()
        {
            state.Reset();
        }

        public RobotBlueprint ApplyToBlueprint(RobotBlueprint source)
        {
            if (source == null)
            {
                return null;
            }

            ResolveDependencies();
            if (robotAssembler != null)
            {
                return robotAssembler.BuildCustomizedBlueprint(source, state);
            }

            return RobotBlueprintClone.Clone(source);
        }

        public GameObject BuildAndApply(RobotBlueprint sourceBlueprint)
        {
            ResolveDependencies();
            if (robotAssembler == null)
            {
                return null;
            }

            RobotBlueprint customized = ApplyToBlueprint(sourceBlueprint);
            GameObject robot = robotAssembler.Assemble(customized, state);
            ApplyPresentation(robot);
            return robot;
        }

        public void ApplyPresentation(GameObject robot)
        {
            ResolveDependencies();
            appearanceController?.ApplyPalette(robot, state.ColorPaletteIndex);
            ApplyEnvironmentSelection();
        }

        public void SelectSlot(RobotPartSlot slot)
        {
            state.SelectedSlot = slot;
        }

        public void CycleSelectedPart(int delta)
        {
            CycleSlot(state.SelectedSlot, delta);
        }

        public void CycleSlot(RobotPartSlot slot, int delta)
        {
            int variantCount = GetVariantCountForSlot(slot);
            if (variantCount <= 0)
            {
                return;
            }

            int next = PositiveMod(state.GetSlotVariantIndex(slot) + delta, variantCount);
            state.SetSlotVariantIndex(slot, next);
            state.SelectedSlot = slot;
            RebuildPreview();
        }

        public void CycleColor(int delta)
        {
            ResolveDependencies();
            int count = appearanceController != null ? appearanceController.PaletteCount : 6;
            state.ColorPaletteIndex = PositiveMod(state.ColorPaletteIndex + delta, Mathf.Max(1, count));
            RebuildPreview();
        }

        public void CycleEnvironment(int delta)
        {
            ResolveDependencies();
            int count = environmentController != null ? environmentController.ThemeCount : 5;
            state.EnvironmentThemeIndex = PositiveMod(state.EnvironmentThemeIndex + delta, Mathf.Max(1, count));
            ApplyEnvironmentSelection();
        }

        public void CyclePartFromMarker(RobotPartMarker marker, int delta)
        {
            if (marker == null)
            {
                return;
            }

            state.SelectedSlot = marker.Slot;
            CycleSlot(marker.Slot, delta);
        }

        private void RebuildPreview()
        {
            if (!IsCreateMode || createBaseBlueprint == null || robotAssembler == null)
            {
                return;
            }

            BuildAndApply(createBaseBlueprint);
        }

        private void ApplyEnvironmentSelection()
        {
            environmentController?.ApplyThemeIndex(state.EnvironmentThemeIndex);
        }

        private int GetVariantCountForSlot(RobotPartSlot slot)
        {
            ResolveDependencies();
            switch (slot)
            {
                case RobotPartSlot.Core:
                    return Mathf.Max(1, robotAssembler != null ? robotAssembler.GetModuleCount(ModuleCategory.Core) : fallbackCoreVariants);
                case RobotPartSlot.LeftArm:
                case RobotPartSlot.RightArm:
                    return Mathf.Max(1, robotAssembler != null ? robotAssembler.GetModuleCount(ModuleCategory.Limb) : fallbackLimbVariants);
                case RobotPartSlot.TopAccessory:
                    return Mathf.Max(1, robotAssembler != null ? robotAssembler.GetModuleCount(ModuleCategory.Accessory) : fallbackAccessoryVariants);
                case RobotPartSlot.Face:
                    return Mathf.Max(1, robotAssembler != null ? robotAssembler.FaceVariantCount : RobotFaceBuilder.DefaultFaceVariantCount);
                case RobotPartSlot.Ears:
                    return Mathf.Max(1, robotAssembler != null ? robotAssembler.EarVariantCount : RobotFaceBuilder.DefaultEarVariantCount);
                default:
                    return 1;
            }
        }

        private void ResolveDependencies()
        {
            if (robotAssembler == null)
            {
                robotAssembler = GetComponent<RobotAssembler>() ?? UnityObjectLookup.FindFirst<RobotAssembler>();
            }

            if (appearanceController == null)
            {
                appearanceController = GetComponent<RobotAppearanceController>() ?? UnityObjectLookup.FindFirst<RobotAppearanceController>();
            }

            if (environmentController == null)
            {
                environmentController = GetComponent<RobotEnvironmentController>() ?? UnityObjectLookup.FindFirst<RobotEnvironmentController>();
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

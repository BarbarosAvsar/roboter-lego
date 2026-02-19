using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboterLego.Domain
{
    public enum TouchEventType
    {
        Down,
        Move,
        Up,
        Tap
    }

    public enum ShapeType
    {
        Unknown,
        Circle,
        Square,
        Triangle,
        Line,
        Swirl
    }

    public enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public enum SessionState
    {
        Idle,
        Create,
        Generate,
        Play
    }

    public enum ModuleCategory
    {
        Core,
        Limb,
        Accessory
    }

    public enum RobotPartSlot
    {
        Core,
        LeftArm,
        RightArm,
        TopAccessory,
        Face,
        Ears
    }

    public enum EnvironmentTheme
    {
        RobotFactory,
        MoonStation,
        NeonLab,
        DesertScrapyard,
        ArcticHangar
    }

    [Serializable]
    public struct TouchEvent
    {
        public TouchEventType Type;
        public Vector2 Position;
        public float Timestamp;

        public TouchEvent(TouchEventType type, Vector2 position, float timestamp)
        {
            Type = type;
            Position = position;
            Timestamp = timestamp;
        }
    }

    [Serializable]
    public struct MotionFrame
    {
        public Vector3 Gyro;
        public Vector3 Accel;
        public float Timestamp;

        public MotionFrame(Vector3 gyro, Vector3 accel, float timestamp)
        {
            Gyro = gyro;
            Accel = accel;
            Timestamp = timestamp;
        }
    }

    [Serializable]
    public struct InputFeatures
    {
        public int TapCount;
        public SwipeDirection SwipeDirection;
        public ShapeType ShapeType;
        public float ShakeEnergy;
        public bool HasMeaningfulInput;
    }

    [Serializable]
    public struct GenerationSeed
    {
        public int FeatureHash;
        public int VariationNonce;

        public GenerationSeed(int featureHash, int variationNonce)
        {
            FeatureHash = featureHash;
            VariationNonce = variationNonce;
        }
    }

    [Serializable]
    public sealed class ModuleSpec
    {
        public string Id;
        public ModuleCategory Category;
        public string[] StyleTags;
        public string PrefabRef;
        public SocketSpec[] Sockets;
    }

    [Serializable]
    public sealed class SocketSpec
    {
        public string SocketId;
        public string SocketType;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
    }

    [Serializable]
    public sealed class CompatibilityRule
    {
        public string SocketTypeA;
        public string SocketTypeB;
        public bool Allowed;
    }

    [Serializable]
    public sealed class BehaviorProfile
    {
        public string MoveStyle;
        public string DanceStyle;
        public string SingStyle;
        [Range(0f, 1f)]
        public float Energy;
    }

    [Serializable]
    public sealed class RobotBlueprint
    {
        public string CoreModuleId;
        public List<string> LimbModuleIds = new List<string>();
        public List<string> AccessoryModuleIds = new List<string>();
        public BehaviorProfile BehaviorProfile = new BehaviorProfile();
    }

    [Serializable]
    public sealed class RobotCustomizationState
    {
        public RobotPartSlot SelectedSlot = RobotPartSlot.LeftArm;
        public int CoreVariantIndex;
        public int LeftArmVariantIndex;
        public int RightArmVariantIndex;
        public int TopAccessoryVariantIndex;
        public int FaceVariantIndex;
        public int EarsVariantIndex;
        public int ColorPaletteIndex;
        public int EnvironmentThemeIndex;

        public int GetSlotVariantIndex(RobotPartSlot slot)
        {
            switch (slot)
            {
                case RobotPartSlot.Core:
                    return CoreVariantIndex;
                case RobotPartSlot.LeftArm:
                    return LeftArmVariantIndex;
                case RobotPartSlot.RightArm:
                    return RightArmVariantIndex;
                case RobotPartSlot.TopAccessory:
                    return TopAccessoryVariantIndex;
                case RobotPartSlot.Face:
                    return FaceVariantIndex;
                case RobotPartSlot.Ears:
                    return EarsVariantIndex;
                default:
                    return 0;
            }
        }

        public void SetSlotVariantIndex(RobotPartSlot slot, int value)
        {
            switch (slot)
            {
                case RobotPartSlot.Core:
                    CoreVariantIndex = value;
                    break;
                case RobotPartSlot.LeftArm:
                    LeftArmVariantIndex = value;
                    break;
                case RobotPartSlot.RightArm:
                    RightArmVariantIndex = value;
                    break;
                case RobotPartSlot.TopAccessory:
                    TopAccessoryVariantIndex = value;
                    break;
                case RobotPartSlot.Face:
                    FaceVariantIndex = value;
                    break;
                case RobotPartSlot.Ears:
                    EarsVariantIndex = value;
                    break;
            }
        }

        public void Reset()
        {
            SelectedSlot = RobotPartSlot.LeftArm;
            CoreVariantIndex = 0;
            LeftArmVariantIndex = 0;
            RightArmVariantIndex = 0;
            TopAccessoryVariantIndex = 0;
            FaceVariantIndex = 0;
            EarsVariantIndex = 0;
            ColorPaletteIndex = 0;
            EnvironmentThemeIndex = 0;
        }
    }
}

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
}

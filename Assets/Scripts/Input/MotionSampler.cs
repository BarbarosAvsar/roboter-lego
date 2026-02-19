using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;
using UnityInput = UnityEngine.Input;

namespace RoboterLego.Input
{
    public sealed class MotionSampler : MonoBehaviour
    {
        [SerializeField] private float sampleHz = 60f;
        [SerializeField] private float outputHz = 20f;
        [SerializeField] private float smoothing = 0.25f;

        private readonly List<MotionFrame> frames = new List<MotionFrame>();
        private float sampleTimer;
        private float outputTimer;
        private Vector2 smoothedTilt;
        private Vector2 latestRawTilt;
        private Vector3 lastAcceleration;
        private ScreenOrientation lastOrientation;

        public bool HasGyroscope { get; private set; }
        public float CurrentShakeEnergy { get; private set; }

        private void Awake()
        {
            HasGyroscope = SystemInfo.supportsGyroscope;
            if (HasGyroscope)
            {
                UnityInput.gyro.enabled = true;
            }

            lastAcceleration = UnityInput.acceleration;
            lastOrientation = Screen.orientation;
        }

        private void Update()
        {
            if (Screen.orientation != lastOrientation)
            {
                lastOrientation = Screen.orientation;
                smoothedTilt = Vector2.zero;
            }

            var sampleInterval = 1f / Mathf.Max(1f, sampleHz);
            sampleTimer += Time.unscaledDeltaTime;
            while (sampleTimer >= sampleInterval)
            {
                sampleTimer -= sampleInterval;
                SampleFrame();
            }

            var outputInterval = 1f / Mathf.Max(1f, outputHz);
            outputTimer += Time.unscaledDeltaTime;
            while (outputTimer >= outputInterval)
            {
                outputTimer -= outputInterval;
                smoothedTilt = Vector2.Lerp(smoothedTilt, latestRawTilt, smoothing);
            }
        }

        public List<MotionFrame> DrainFrames()
        {
            var snapshot = new List<MotionFrame>(frames);
            frames.Clear();
            return snapshot;
        }

        public bool TryGetDirectionalTilt(out Vector2 tilt)
        {
            if (!HasGyroscope)
            {
                tilt = Vector2.zero;
                return false;
            }

            tilt = smoothedTilt;
            return true;
        }

        private void SampleFrame()
        {
            Vector3 gyro = HasGyroscope ? UnityInput.gyro.rotationRateUnbiased : Vector3.zero;
            Vector3 accel = UnityInput.acceleration;
            frames.Add(new MotionFrame(gyro, accel, Time.time));
            CurrentShakeEnergy = Mathf.Clamp01((accel - lastAcceleration).magnitude / 2f);
            lastAcceleration = accel;

            var rawTilt = new Vector2(accel.x, accel.y);
            latestRawTilt = NormalizeTiltByOrientation(rawTilt, Screen.orientation);
        }

        private static Vector2 NormalizeTiltByOrientation(Vector2 tilt, ScreenOrientation orientation)
        {
            switch (orientation)
            {
                case ScreenOrientation.Portrait:
                    return tilt;
                case ScreenOrientation.PortraitUpsideDown:
                    return -tilt;
                case ScreenOrientation.LandscapeLeft:
                    return new Vector2(tilt.y, -tilt.x);
                case ScreenOrientation.LandscapeRight:
                    return new Vector2(-tilt.y, tilt.x);
                default:
                    return tilt;
            }
        }
    }
}

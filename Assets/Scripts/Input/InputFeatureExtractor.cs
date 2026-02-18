using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Input
{
    public sealed class InputFeatureExtractor
    {
        private const float MinSwipeDistancePixels = 60f;

        private readonly IShapeRecognizer shapeRecognizer;

        public InputFeatureExtractor(IShapeRecognizer shapeRecognizer)
        {
            this.shapeRecognizer = shapeRecognizer;
        }

        public InputFeatures Extract(
            IReadOnlyList<TouchEvent> touchEvents,
            IReadOnlyList<Vector2> strokePoints,
            IReadOnlyList<MotionFrame> motionFrames)
        {
            int tapCount = 0;
            Vector2? swipeStart = null;
            Vector2 swipeEnd = Vector2.zero;

            for (int i = 0; i < touchEvents.Count; i++)
            {
                var touchEvent = touchEvents[i];
                if (touchEvent.Type == TouchEventType.Tap)
                {
                    tapCount++;
                }
                else if (touchEvent.Type == TouchEventType.Down && !swipeStart.HasValue)
                {
                    swipeStart = touchEvent.Position;
                    swipeEnd = touchEvent.Position;
                }
                else if (touchEvent.Type == TouchEventType.Move || touchEvent.Type == TouchEventType.Up)
                {
                    swipeEnd = touchEvent.Position;
                }
            }

            var swipeDirection = DetectSwipeDirection(swipeStart, swipeEnd);
            var shapeType = shapeRecognizer.Recognize(strokePoints);
            var shakeEnergy = EstimateShakeEnergy(motionFrames);

            bool hasMeaningfulInput = tapCount > 0
                || swipeDirection != SwipeDirection.None
                || shapeType != ShapeType.Unknown
                || shakeEnergy > 0.15f;

            return new InputFeatures
            {
                TapCount = tapCount,
                SwipeDirection = swipeDirection,
                ShapeType = shapeType,
                ShakeEnergy = shakeEnergy,
                HasMeaningfulInput = hasMeaningfulInput
            };
        }

        private static SwipeDirection DetectSwipeDirection(Vector2? start, Vector2 end)
        {
            if (!start.HasValue)
            {
                return SwipeDirection.None;
            }

            Vector2 delta = end - start.Value;
            if (delta.magnitude < MinSwipeDistancePixels)
            {
                return SwipeDirection.None;
            }

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x > 0f ? SwipeDirection.Right : SwipeDirection.Left;
            }

            return delta.y > 0f ? SwipeDirection.Up : SwipeDirection.Down;
        }

        private static float EstimateShakeEnergy(IReadOnlyList<MotionFrame> motionFrames)
        {
            if (motionFrames == null || motionFrames.Count < 2)
            {
                return 0f;
            }

            float sum = 0f;
            int count = 0;
            for (int i = 1; i < motionFrames.Count; i++)
            {
                Vector3 delta = motionFrames[i].Accel - motionFrames[i - 1].Accel;
                sum += delta.sqrMagnitude;
                count++;
            }

            if (count == 0)
            {
                return 0f;
            }

            float rms = Mathf.Sqrt(sum / count);
            return Mathf.Clamp01(rms / 2.2f);
        }
    }
}

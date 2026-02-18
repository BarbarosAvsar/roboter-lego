using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Input
{
    public sealed class TouchInputCollector : MonoBehaviour
    {
        [SerializeField] private float tapDebounceSeconds = 0.30f;
        [SerializeField] private float minStrokeDistancePixels = 24f;

        private readonly List<TouchEvent> touchEvents = new List<TouchEvent>();
        private readonly List<Vector2> activeStrokePoints = new List<Vector2>();
        private readonly List<Vector2> completedStrokePoints = new List<Vector2>();
        private float lastTapTime = -100f;

        public IReadOnlyList<Vector2> CompletedStrokePoints => completedStrokePoints;

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                ProcessTouch(Input.GetTouch(0));
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            else
            {
                ProcessMouseForEditor();
            }
#endif
        }

        public List<TouchEvent> DrainTouchEvents()
        {
            var snapshot = new List<TouchEvent>(touchEvents);
            touchEvents.Clear();
            return snapshot;
        }

        public List<Vector2> DrainStrokePoints()
        {
            var snapshot = new List<Vector2>(completedStrokePoints);
            completedStrokePoints.Clear();
            return snapshot;
        }

        public void ClearAll()
        {
            touchEvents.Clear();
            activeStrokePoints.Clear();
            completedStrokePoints.Clear();
        }

        private void ProcessTouch(Touch touch)
        {
            var now = Time.time;
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    activeStrokePoints.Clear();
                    AddPoint(touch.position);
                    touchEvents.Add(new TouchEvent(TouchEventType.Down, touch.position, now));
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    AddPoint(touch.position);
                    touchEvents.Add(new TouchEvent(TouchEventType.Move, touch.position, now));
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    AddPoint(touch.position);
                    touchEvents.Add(new TouchEvent(TouchEventType.Up, touch.position, now));
                    FinishStroke(now, touch.position);
                    break;
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        private void ProcessMouseForEditor()
        {
            var now = Time.time;
            if (Input.GetMouseButtonDown(0))
            {
                activeStrokePoints.Clear();
                AddPoint(Input.mousePosition);
                touchEvents.Add(new TouchEvent(TouchEventType.Down, Input.mousePosition, now));
            }
            else if (Input.GetMouseButton(0))
            {
                AddPoint(Input.mousePosition);
                touchEvents.Add(new TouchEvent(TouchEventType.Move, Input.mousePosition, now));
            }
            else if (Input.GetMouseButtonUp(0))
            {
                AddPoint(Input.mousePosition);
                touchEvents.Add(new TouchEvent(TouchEventType.Up, Input.mousePosition, now));
                FinishStroke(now, Input.mousePosition);
            }
        }
#endif

        private void FinishStroke(float now, Vector2 endPosition)
        {
            var strokeDistance = EstimateStrokeDistance(activeStrokePoints);
            completedStrokePoints.Clear();
            completedStrokePoints.AddRange(activeStrokePoints);

            if (strokeDistance <= minStrokeDistancePixels && now - lastTapTime >= tapDebounceSeconds)
            {
                touchEvents.Add(new TouchEvent(TouchEventType.Tap, endPosition, now));
                lastTapTime = now;
            }
        }

        private void AddPoint(Vector2 point)
        {
            if (activeStrokePoints.Count == 0)
            {
                activeStrokePoints.Add(point);
                return;
            }

            if (Vector2.Distance(activeStrokePoints[activeStrokePoints.Count - 1], point) >= 2f)
            {
                activeStrokePoints.Add(point);
            }
        }

        private static float EstimateStrokeDistance(IReadOnlyList<Vector2> points)
        {
            if (points.Count < 2)
            {
                return 0f;
            }

            float distance = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                distance += Vector2.Distance(points[i - 1], points[i]);
            }

            return distance;
        }
    }
}

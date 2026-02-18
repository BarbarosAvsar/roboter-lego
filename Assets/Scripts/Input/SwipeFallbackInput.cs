using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Input
{
    public sealed class SwipeFallbackInput : MonoBehaviour
    {
        [SerializeField] private float minSwipeDistance = 90f;
        [SerializeField] private float holdDurationSeconds = 0.45f;

        private Vector2 currentDirection;
        private float expiresAt;
        private Vector2? activeStart;

        public void Feed(IReadOnlyList<TouchEvent> touchEvents)
        {
            for (int i = 0; i < touchEvents.Count; i++)
            {
                var touchEvent = touchEvents[i];
                if (touchEvent.Type == TouchEventType.Down)
                {
                    activeStart = touchEvent.Position;
                }
                else if (touchEvent.Type == TouchEventType.Up && activeStart.HasValue)
                {
                    var delta = touchEvent.Position - activeStart.Value;
                    if (delta.magnitude >= minSwipeDistance)
                    {
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        {
                            currentDirection = delta.x > 0f ? Vector2.right : Vector2.left;
                        }
                        else
                        {
                            currentDirection = delta.y > 0f ? Vector2.up : Vector2.down;
                        }

                        expiresAt = Time.time + holdDurationSeconds;
                    }

                    activeStart = null;
                }
            }
        }

        public Vector2 GetDirection()
        {
            if (Time.time > expiresAt)
            {
                currentDirection = Vector2.zero;
            }

            return currentDirection;
        }
    }
}

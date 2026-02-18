using System.Collections.Generic;
using RoboterLego.Domain;
using UnityEngine;

namespace RoboterLego.Input
{
    public sealed class ShapeRecognizer : IShapeRecognizer
    {
        public ShapeType Recognize(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 3)
            {
                return ShapeType.Unknown;
            }

            var simplified = Simplify(points, 6f);
            if (simplified.Count < 3)
            {
                return ShapeType.Unknown;
            }

            float pathLength = PathLength(simplified);
            float endDistance = Vector2.Distance(simplified[0], simplified[simplified.Count - 1]);
            float straightness = pathLength <= Mathf.Epsilon ? 1f : endDistance / pathLength;
            float totalTurn = Mathf.Abs(TotalSignedTurnRadians(simplified));

            if (straightness > 0.92f)
            {
                return ShapeType.Line;
            }

            if (totalTurn > Mathf.PI * 3.5f)
            {
                return ShapeType.Swirl;
            }

            var bounds = ComputeBounds(simplified);
            float diagonal = bounds.size.magnitude;
            bool isClosed = endDistance <= diagonal * 0.30f;

            if (!isClosed)
            {
                return ShapeType.Unknown;
            }

            int corners = CountCorners(simplified, 0.75f);
            if (corners >= 3 && corners <= 4)
            {
                if (corners == 3)
                {
                    return ShapeType.Triangle;
                }

                var aspect = Mathf.Abs(bounds.size.x - bounds.size.y) / Mathf.Max(1f, Mathf.Max(bounds.size.x, bounds.size.y));
                if (aspect <= 0.35f)
                {
                    return ShapeType.Square;
                }
            }

            if (LooksCircular(simplified))
            {
                return ShapeType.Circle;
            }

            return ShapeType.Unknown;
        }

        private static List<Vector2> Simplify(IReadOnlyList<Vector2> points, float minDistance)
        {
            var result = new List<Vector2>(points.Count);
            Vector2 previous = points[0];
            result.Add(previous);

            for (int i = 1; i < points.Count; i++)
            {
                var p = points[i];
                if (Vector2.Distance(previous, p) >= minDistance)
                {
                    result.Add(p);
                    previous = p;
                }
            }

            if (result.Count < 2 || result[result.Count - 1] != points[points.Count - 1])
            {
                result.Add(points[points.Count - 1]);
            }

            return result;
        }

        private static float PathLength(IReadOnlyList<Vector2> points)
        {
            float length = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                length += Vector2.Distance(points[i - 1], points[i]);
            }

            return length;
        }

        private static float TotalSignedTurnRadians(IReadOnlyList<Vector2> points)
        {
            float total = 0f;
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector2 a = (points[i] - points[i - 1]).normalized;
                Vector2 b = (points[i + 1] - points[i]).normalized;
                if (a.sqrMagnitude < 0.0001f || b.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                float angle = Mathf.Atan2(a.x * b.y - a.y * b.x, Vector2.Dot(a, b));
                total += angle;
            }

            return total;
        }

        private static Rect ComputeBounds(IReadOnlyList<Vector2> points)
        {
            var min = points[0];
            var max = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                min = Vector2.Min(min, points[i]);
                max = Vector2.Max(max, points[i]);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static int CountCorners(IReadOnlyList<Vector2> points, float minAngleRadians)
        {
            int count = 0;
            for (int i = 1; i < points.Count - 1; i++)
            {
                Vector2 a = (points[i - 1] - points[i]).normalized;
                Vector2 b = (points[i + 1] - points[i]).normalized;
                float angle = Mathf.Acos(Mathf.Clamp(Vector2.Dot(a, b), -1f, 1f));
                if (angle >= minAngleRadians)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool LooksCircular(IReadOnlyList<Vector2> points)
        {
            Vector2 center = Vector2.zero;
            for (int i = 0; i < points.Count; i++)
            {
                center += points[i];
            }

            center /= points.Count;

            float meanRadius = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                meanRadius += Vector2.Distance(center, points[i]);
            }

            meanRadius /= points.Count;
            if (meanRadius < 1f)
            {
                return false;
            }

            float variance = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                float radius = Vector2.Distance(center, points[i]);
                float delta = radius - meanRadius;
                variance += delta * delta;
            }

            variance /= points.Count;
            float normalizedStdDev = Mathf.Sqrt(variance) / meanRadius;
            return normalizedStdDev <= 0.33f;
        }
    }
}

using System.Numerics;

namespace Raid.Battle.Collision;

public static class CollisionHelper
{
    private const float Epsilon = 0.0001f;

    public static bool CircleIsInside(Vector2 center, float radius, Aabb bounds)
    {
        return center.X - radius >= bounds.Min.X
            && center.X + radius <= bounds.Max.X
            && center.Y - radius >= bounds.Min.Y
            && center.Y + radius <= bounds.Max.Y;
    }

    public static bool CircleIsInside(Vector2 center, float radius, Circle arena)
    {
        return Vector2.Distance(center, arena.Center) + radius <= arena.Radius + Epsilon;
    }

    public static bool CircleIntersects(Vector2 center, float radius, Aabb box)
    {
        var closestX = Math.Clamp(center.X, box.Min.X, box.Max.X);
        var closestY = Math.Clamp(center.Y, box.Min.Y, box.Max.Y);
        var dx = center.X - closestX;
        var dy = center.Y - closestY;
        return (dx * dx) + (dy * dy) < (radius * radius);
    }

    public static bool AllowsEntityStep(
        Vector2 from,
        Vector2 to,
        float moverRadius,
        Vector2 blockerPosition,
        float blockerRadius)
    {
        var minSeparation = moverRadius + blockerRadius;
        if (minSeparation <= 0f)
        {
            return true;
        }

        var oldDistance = Vector2.Distance(from, blockerPosition);
        var newDistance = Vector2.Distance(to, blockerPosition);

        if (oldDistance >= minSeparation - Epsilon)
        {
            return newDistance >= minSeparation - Epsilon;
        }

        return newDistance + Epsilon >= oldDistance;
    }

    public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var span = end - start;
        var lengthSquared = span.LengthSquared();
        if (lengthSquared <= Epsilon)
        {
            return start;
        }

        var t = Math.Clamp(Vector2.Dot(point - start, span) / lengthSquared, 0f, 1f);
        return start + (span * t);
    }

    public static bool CircleIntersectsCapsule(
        Vector2 center,
        float circleRadius,
        Vector2 start,
        Vector2 end,
        float capsuleRadius)
    {
        var closest = ClosestPointOnSegment(center, start, end);
        var combined = circleRadius + capsuleRadius;
        return Vector2.DistanceSquared(center, closest) <= (combined * combined) + Epsilon;
    }
}

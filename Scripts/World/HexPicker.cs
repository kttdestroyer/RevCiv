using UnityEngine;

public static class HexPicker
{
    // Inverse of AxialToWorld for pointy-top axial coords.
    // Given a world point (x,z) on the ground plane (y=height of tiles),
    // return axial (q,r) BEFORE rounding.
    public static Vector2 WorldToAxialRaw(Vector3 world, float radius)
    {
        // convert world x,z -> axial q,r (pointy-top)
        float q = (Mathf.Sqrt(3f) / 3f * world.x - 1f / 3f * world.z) / radius;
        float r = (2f / 3f * world.z) / radius;
        return new Vector2(q, r);
    }

    // Round fractional axial to nearest hex using cube rounding
    public static Vector2Int AxialRound(Vector2 qr)
    {
        float q = qr.x; float r = qr.y;
        float s = -q - r;

        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float qDiff = Mathf.Abs(rq - q);
        float rDiff = Mathf.Abs(rr - r);
        float sDiff = Mathf.Abs(rs - s);

        if (qDiff > rDiff && qDiff > sDiff) rq = -rr - rs;
        else if (rDiff > sDiff) rr = -rq - rs;
        // else rs = -rq - rr;

        return new Vector2Int(rq, rr);
    }

    // Ray ? plane (y = groundY) ? axial rounded
    public static bool RayToAxial(Camera cam, Ray ray, float groundY, float radius, out Vector2Int axial, out Vector3 worldPoint)
    {
        axial = default;
        worldPoint = default;

        // intersect with plane y = groundY
        if (ray.direction.y == 0f) return false;
        float t = (groundY - ray.origin.y) / ray.direction.y;
        if (t < 0f) return false;

        worldPoint = ray.origin + ray.direction * t;
        var raw = WorldToAxialRaw(worldPoint, radius);
        axial = AxialRound(raw);
        return true;
    }
}

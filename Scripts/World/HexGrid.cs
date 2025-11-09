using UnityEngine;

public static class HexGrid
{
    // Pointy-top axial directions, clockwise from East
    public static readonly Vector2Int[] AxialDirs =
    {
        new Vector2Int(1, 0),   // E
        new Vector2Int(1, -1),  // NE
        new Vector2Int(0, -1),  // NW
        new Vector2Int(-1, 0),  // W
        new Vector2Int(-1, 1),  // SW
        new Vector2Int(0, 1),   // SE
    };

    /// Axial -> world (pointy-top). radius = center->corner distance.
    public static Vector3 AxialToWorld(int q, int r, float radius, float y = 0f)
    {
        float x = radius * Mathf.Sqrt(3f) * (q + r * 0.5f);
        float z = radius * 1.5f * r;
        return new Vector3(x, y, z);
    }

    /// 6 corners (world space) for pointy-top hex: 30°, 90°, 150°, 210°, 270°, 330°
    public static void GetHexCorners(Vector3 center, float radius, Vector3[] out6)
    {
        const float deg2rad = Mathf.PI / 180f;
        float[] angles = { 30f, 90f, 150f, 210f, 270f, 330f };
        for (int i = 0; i < 6; i++)
        {
            float a = angles[i] * deg2rad;
            out6[i] = new Vector3(
                center.x + radius * Mathf.Cos(a),
                center.y,
                center.z + radius * Mathf.Sin(a)
            );
        }
    }

    public static float EstimateRadiusFromBounds(Bounds b)
    {
        return Mathf.Max(b.extents.x, b.extents.z);
    }

    // ---- Hex distance & ranges ----

    /// Cube/axial hex distance between two axial coords.
    public static int HexDistance(int q1, int r1, int q2, int r2)
    {
        int dq = q1 - q2;
        int dr = r1 - r2;
        int ds = -(q1 + r1) - (-(q2 + r2));
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

    /// Iterate axial coords in a hex range (center q,r, radius R) and invoke action(q,r)
    public static void ForEachInRange(int q, int r, int R, System.Action<int, int> action)
    {
        for (int dq = -R; dq <= R; dq++)
        {
            int rMin = Mathf.Max(-R, -dq - R);
            int rMax = Mathf.Min(R, -dq + R);
            for (int dr = rMin; dr <= rMax; dr++)
            {
                action(q + dq, r + dr);
            }
        }
    }
}

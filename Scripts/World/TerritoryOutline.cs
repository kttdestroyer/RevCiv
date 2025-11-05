using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TerritoryOutline : MonoBehaviour
{
    [Header("Render")]
    public float tileSize = 1f;
    [Tooltip("Thickness of the border band (world units).")]
    public float borderWidth = 0.06f;
    [Tooltip("Height above ground to avoid z-fighting.")]
    public float height = 0.07f;
    [Tooltip("Border color (alpha = opacity).")]
    public Color color = new Color(0.1f, 1f, 0.8f, 0.75f);

    [Header("Update")]
    [Tooltip("Rebuild every N seconds in play mode. Set 0 to rebuild every frame.")]
    public float rebuildInterval = 0.5f;

    MeshFilter mf;
    MeshRenderer mr;
    float timer;

    void OnEnable() { Ensure(); Rebuild(); }
    void OnValidate() { Ensure(); Rebuild(); }
    void Update()
    {
        if (!Application.isPlaying)
        {
            // In Edit Mode, rebuild when moved/validated
            return;
        }

        if (rebuildInterval <= 0f) { Rebuild(); }
        else
        {
            timer += Time.deltaTime;
            if (timer >= rebuildInterval)
            {
                timer = 0f;
                Rebuild();
            }
        }
    }

    void Ensure()
    {
        if (!mf) mf = GetComponent<MeshFilter>();
        if (!mr) mr = GetComponent<MeshRenderer>();
        if (!mf) mf = gameObject.AddComponent<MeshFilter>();
        if (!mr) mr = gameObject.AddComponent<MeshRenderer>();

        // Transparent unlit material
        var shader = Shader.Find("Unlit/Color");
        if (!shader) shader = Shader.Find("Sprites/Default");
        if (!mr.sharedMaterial)
        {
            var mat = new Material(shader);
            mat.renderQueue = 3000;
            mr.sharedMaterial = mat;
        }
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    public void Rebuild()
    {
        if (!mf || !mr) Ensure();

        var my = GetComponent<Settlement>();
        if (!my) return;

        // Collect all tiles owned by this settlement
#if UNITY_6000_0_OR_NEWER
        var tiles = FindObjectsByType<TileInfo>(FindObjectsSortMode.None);
#else
        var tiles = FindObjectsOfType<TileInfo>();
#endif
        var owned = new HashSet<(int x, int z)>();
        foreach (var t in tiles)
        {
            if (!t) continue;
            if (t.ownerSettlementId == my.settlementId)
            {
                // derive grid coordinate from world pos and tileSize
                int gx = Mathf.RoundToInt(t.transform.position.x / Mathf.Max(0.0001f, tileSize));
                int gz = Mathf.RoundToInt(t.transform.position.z / Mathf.Max(0.0001f, tileSize));
                owned.Add((gx, gz));
            }
        }

        // Build quads along edges that face non-owned space
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var cols = new List<Color>();

        foreach (var cell in owned)
        {
            int x = cell.x;
            int z = cell.z;

            // neighbors
            bool left = owned.Contains((x - 1, z));
            bool right = owned.Contains((x + 1, z));
            bool down = owned.Contains((x, z - 1));
            bool up = owned.Contains((x, z + 1));

            // world centers
            float wx = x * tileSize;
            float wz = z * tileSize;
            float half = tileSize * 0.5f;
            float h = height;
            float bw = borderWidth;

            // For each missing neighbor, add a centered edge-quad band

            // Left edge (between x-1 and x)
            if (!left)
            {
                float cx = wx - half; // center of the edge
                AddBandQuad(
                    verts, tris, cols,
                    new Vector3(cx - bw * 0.5f, h, wz - half),
                    new Vector3(cx + bw * 0.5f, h, wz - half),
                    new Vector3(cx + bw * 0.5f, h, wz + half),
                    new Vector3(cx - bw * 0.5f, h, wz + half),
                    color
                );
            }

            // Right edge
            if (!right)
            {
                float cx = wx + half;
                AddBandQuad(
                    verts, tris, cols,
                    new Vector3(cx - bw * 0.5f, h, wz - half),
                    new Vector3(cx + bw * 0.5f, h, wz - half),
                    new Vector3(cx + bw * 0.5f, h, wz + half),
                    new Vector3(cx - bw * 0.5f, h, wz + half),
                    color
                );
            }

            // Bottom edge (down)
            if (!down)
            {
                float cz = wz - half;
                AddBandQuad(
                    verts, tris, cols,
                    new Vector3(wx - half, h, cz - bw * 0.5f),
                    new Vector3(wx + half, h, cz - bw * 0.5f),
                    new Vector3(wx + half, h, cz + bw * 0.5f),
                    new Vector3(wx - half, h, cz + bw * 0.5f),
                    color
                );
            }

            // Top edge (up)
            if (!up)
            {
                float cz = wz + half;
                AddBandQuad(
                    verts, tris, cols,
                    new Vector3(wx - half, h, cz - bw * 0.5f),
                    new Vector3(wx + half, h, cz - bw * 0.5f),
                    new Vector3(wx + half, h, cz + bw * 0.5f),
                    new Vector3(wx - half, h, cz + bw * 0.5f),
                    color
                );
            }
        }

        var mesh = mf.sharedMesh;
        if (mesh == null) { mesh = new Mesh(); mesh.name = "TerritoryOutlineMesh"; }
#if UNITY_EDITOR
        if (!Application.isPlaying) mesh.Clear();
        else mesh.Clear();
#else
        mesh.Clear();
#endif
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(cols);
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;

        // Apply color to material too (useful if shader ignores vertex colors)
        if (mr && mr.sharedMaterial)
        {
            if (mr.sharedMaterial.HasProperty("_Color")) mr.sharedMaterial.SetColor("_Color", color);
            if (mr.sharedMaterial.HasProperty("_BaseColor")) mr.sharedMaterial.SetColor("_BaseColor", color);
        }
    }

    static void AddBandQuad(List<Vector3> v, List<int> t, List<Color> c, Vector3 a, Vector3 b, Vector3 d, Vector3 e, Color col)
    {
        // a-b-d-e (two triangles)
        int idx = v.Count;
        v.Add(a); v.Add(b); v.Add(d); v.Add(e);
        c.Add(col); c.Add(col); c.Add(col); c.Add(col);
        t.Add(idx + 0); t.Add(idx + 2); t.Add(idx + 1);
        t.Add(idx + 0); t.Add(idx + 3); t.Add(idx + 2);
    }
}

using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[DisallowMultipleComponent]
public class HexTileMesh : MonoBehaviour
{
    [Header("Shape")]
    [Tooltip("Hex radius (center to corner). If 0, auto-derive from local scale.")]
    public float radius = 0.5f;

    [Tooltip("Vertical offset of the visual (not thickness).")]
    public float yOffset = 0.0f;

    [Header("Visual")]
    public Color color = new Color(0.35f, 0.75f, 0.35f, 1f);

    MeshFilter mf;
    MeshRenderer mr;

    void Awake() { Ensure(); Build(); }
    void OnValidate() { Ensure(); Build(); }

    void Ensure()
    {
        if (!mf) mf = GetComponent<MeshFilter>();
        if (!mr) mr = GetComponent<MeshRenderer>();

        // Sprites/Default is transparent, double-sided, and simple (good for flat colored polys)
        if (mr.sharedMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (!shader) shader = Shader.Find("Unlit/Transparent");
            var mat = new Material(shader) { renderQueue = 3000 }; // regular transparent queue
            mat.color = color;
            // No z-write for clean layering (tiles at same y)
            TrySetInt(mat, "_ZWrite", 0);
            mr.sharedMaterial = mat;
        }

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void Build()
    {
        if (!mf) return;

        float r = radius;
        if (r <= 0f) r = Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.5f;

        Vector3 worldCenter = transform.position + Vector3.up * yOffset;
        Vector3[] wc = new Vector3[6];
        HexGrid.GetHexCorners(worldCenter, r, wc);
        var inv = transform.worldToLocalMatrix;

        // Vertex fan (center + corners)
        Vector3[] v = new Vector3[7];
        Color[] c = new Color[7];
        int[] t = new int[6 * 3];

        v[0] = inv.MultiplyPoint3x4(worldCenter);
        c[0] = color;

        // Ensure CORRECT winding seen from above (counter-clockwise for Unity default front faces)
        for (int i = 0; i < 6; i++)
        {
            v[i + 1] = inv.MultiplyPoint3x4(wc[i]);
            c[i + 1] = color;

            int iNext = (i + 1) % 6;
            // Front-facing CCW
            t[i * 3 + 0] = 0;
            t[i * 3 + 1] = iNext + 1;
            t[i * 3 + 2] = i + 1;
        }

        var mesh = mf.sharedMesh;
        if (!mesh) { mesh = new Mesh(); mesh.name = "HexTile_Flat"; }
        mesh.Clear();
        mesh.vertices = v;
        mesh.colors = c;
        mesh.triangles = t;
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;

        // Exact picking collider
        var mc = GetComponent<MeshCollider>();
        if (!mc) mc = gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = null;
        mc.sharedMesh = mesh;
        mc.convex = false;

        // sync material color
        if (mr.sharedMaterial != null)
        {
            if (mr.sharedMaterial.HasProperty("_BaseColor")) mr.sharedMaterial.SetColor("_BaseColor", color);
            if (mr.sharedMaterial.HasProperty("_Color")) mr.sharedMaterial.SetColor("_Color", color);
        }
    }

    static void TrySetInt(Material m, string prop, int val)
    {
        if (m.HasProperty(prop)) m.SetInt(prop, val);
    }
}

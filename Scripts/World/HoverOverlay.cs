using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class HoverOverlay : MonoBehaviour
{
    [Header("Visual")]
    public float height = 0.12f;
    public float alpha = 0.65f;
    public Color idleColor = new Color(1f, 1f, 1f, 0.65f);
    public Color okColor = new Color(0.2f, 1f, 0.2f, 0.65f);
    public Color badColor = new Color(1f, 0.2f, 0.2f, 0.65f);

    [Header("Debug")]
    public bool keepVisibleForDebug = false; // shows last hex even when not hovering

    const string ChildName = "HoverOverlay_Hex";

    GameObject child;
    MeshFilter mf;
    MeshRenderer mr;
    Material mat;

    void OnEnable()
    {
        EnsureChild();
        if (!keepVisibleForDebug) Hide();
    }

    void OnDisable()
    {
        if (child) child.SetActive(false);
    }

    void OnDestroy()
    {
        if (child)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(child);
            else Destroy(child);
#else
            Destroy(child);
#endif
            child = null; mf = null; mr = null;
        }
        if (mat)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(mat);
            else Destroy(mat);
#else
            Destroy(mat);
#endif
            mat = null;
        }
    }

    public void ShowAt(TileInfo tile, Color c)
    {
        if (!tile) { if (!keepVisibleForDebug) Hide(); return; }
        EnsureChild();

        // bounds ? center + radius
        Bounds b;
        var bc = tile.GetComponent<BoxCollider>();
        if (bc) b = bc.bounds;
        else
        {
            var r = tile.GetComponent<MeshRenderer>();
            if (!r) { if (!keepVisibleForDebug) Hide(); return; }
            b = r.bounds;
        }

        float radius = HexGrid.EstimateRadiusFromBounds(b);
        Vector3 center = b.center;
        center.y = b.max.y + height;

        if (mf.sharedMesh == null) mf.sharedMesh = new Mesh() { name = "HoverOverlayHex" };
        BuildHexLocal(mf.sharedMesh, child.transform, center, radius, c);

        if (mat != null)
        {
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        }

        if (!child.activeSelf) child.SetActive(true);
    }

    public void Hide()
    {
        EnsureChild();
        if (keepVisibleForDebug) return;
        if (child && child.activeSelf) child.SetActive(false);
    }

    // ---- internal ----
    void EnsureChild()
    {
        // find existing by exact name
        if (!child)
        {
            var t = transform.Find(ChildName);
            if (t) child = t.gameObject;
        }

        // create if missing
        if (!child)
        {
            child = new GameObject(ChildName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
        }

        // ensure required components
        mf = child.GetComponent<MeshFilter>();
        if (!mf) mf = child.AddComponent<MeshFilter>();

        mr = child.GetComponent<MeshRenderer>();
        if (!mr) mr = child.AddComponent<MeshRenderer>();

        // remove any collider that might have come from a primitive
        var col = child.GetComponent<Collider>();
        if (col)
        {
#if UNITY_EDITOR
            DestroyImmediate(col);
#else
            Destroy(col);
#endif
        }

        // ensure a transparent material
        if (!mat)
        {
            var shader = Shader.Find("Sprites/Default");
            if (!shader) shader = Shader.Find("Unlit/Transparent");
            mat = new Material(shader) { renderQueue = 3500 };
            TrySetInt(mat, "_ZWrite", 0);
            TrySetInt(mat, "_Surface", 1);
            TrySetInt(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            TrySetInt(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        if (mr.sharedMaterial != mat) mr.sharedMaterial = mat;

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    static void BuildHexLocal(Mesh mesh, Transform target, Vector3 centerWorld, float radius, Color c)
    {
        Vector3[] corners = new Vector3[6];
        HexGrid.GetHexCorners(centerWorld, radius, corners);

        var inv = target.worldToLocalMatrix;

        Vector3[] v = new Vector3[7]; // center + 6 corners
        Color[] col = new Color[7];
        int[] tri = new int[6 * 3];

        v[0] = inv.MultiplyPoint3x4(centerWorld);
        col[0] = c;

        for (int i = 0; i < 6; i++)
        {
            v[i + 1] = inv.MultiplyPoint3x4(corners[i]);
            col[i + 1] = c;

            int iNext = (i + 1) % 6;
            tri[i * 3 + 0] = 0;
            tri[i * 3 + 1] = i + 1;
            tri[i * 3 + 2] = iNext + 1;
        }

        mesh.Clear();
        mesh.vertices = v;
        mesh.colors = col;
        mesh.triangles = tri;
        mesh.RecalculateBounds();
    }

    static void TrySetInt(Material m, string prop, int val)
    {
        if (m.HasProperty(prop)) m.SetInt(prop, val);
    }
}

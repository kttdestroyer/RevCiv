using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class TerritoryOutline : MonoBehaviour
{
    [Header("Render")]
    public float borderWidth = 0.06f;
    public float height = 0.10f;
    public Color color = new Color(0.1f, 1f, 0.8f, 0.65f);
    public float rebuildInterval = 0.5f;

    const string ChildRender = "TerritoryOutline_Render";

    GameObject renderChild;
    MeshFilter mf;
    MeshRenderer mr;
    Material mat;

    float timer;
    bool needsRebuild = true;
    bool ensuredOnce;

    void OnEnable() { ensuredOnce = false; needsRebuild = true; }
    void OnValidate() { needsRebuild = true; }
    void OnDisable() { if (renderChild) renderChild.SetActive(false); }

    void OnDestroy()
    {
        if (renderChild)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(renderChild);
            else Destroy(renderChild);
#else
            Destroy(renderChild);
#endif
            renderChild = null; mf = null; mr = null;
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

    void Update()
    {
        if (!ensuredOnce)
        {
            EnsureRenderObjects();
            ensuredOnce = true;
        }

        if (needsRebuild)
        {
            EnsureRenderObjects();
            Rebuild();
            needsRebuild = false;
            timer = 0f;
            return;
        }

        if (Application.isPlaying)
        {
            if (rebuildInterval <= 0f) Rebuild();
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
    }

    // --- ALWAYS ensure child + MeshFilter + MeshRenderer BEFORE any material access ---
    void EnsureRenderObjects()
    {
        // Find/create child
        if (!renderChild)
        {
            var t = transform.Find(ChildRender);
            if (t) renderChild = t.gameObject;
        }
        if (!renderChild)
        {
            renderChild = new GameObject(ChildRender);
            renderChild.transform.SetParent(transform, false);
            renderChild.transform.localPosition = Vector3.zero;
            renderChild.transform.localRotation = Quaternion.identity;
            renderChild.transform.localScale = Vector3.one;
        }

        // Ensure required components (repair stale children that lack them)
        mf = renderChild.GetComponent<MeshFilter>();
        if (!mf) mf = renderChild.AddComponent<MeshFilter>();

        mr = renderChild.GetComponent<MeshRenderer>();
        if (!mr) mr = renderChild.AddComponent<MeshRenderer>();

        // Remove any stray Collider that could have been added by primitives
        var col = renderChild.GetComponent<Collider>();
        if (col)
        {
#if UNITY_EDITOR
            DestroyImmediate(col);
#else
            Destroy(col);
#endif
        }

        // Ensure a transparent, non-occluding material
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

        // Safe assignment (mr is guaranteed to exist now)
        if (mr.sharedMaterial != mat) mr.sharedMaterial = mat;

        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        if (!renderChild.activeSelf) renderChild.SetActive(true);
    }

    public void Rebuild()
    {
        if (!mf || !renderChild || !mr) { EnsureRenderObjects(); if (!mf || !mr) return; }

        var my = GetComponent<Settlement>();
        if (!my) { ClearMesh(); Hide(); return; }

#if UNITY_6000_0_OR_NEWER
        var allTiles = FindObjectsByType<TileInfo>(FindObjectsSortMode.None);
#else
        var allTiles = Object.FindObjectsOfType<TileInfo>();
#endif
        if (allTiles == null || allTiles.Length == 0) { ClearMesh(); Hide(); return; }

        var owned = new List<TileInfo>(32);
        var ownedSet = new HashSet<Vector2Int>();
        foreach (var t in allTiles)
        {
            if (!t) continue;
            if (t.ownerSettlementId != my.settlementId) continue;
            owned.Add(t);
            ownedSet.Add(new Vector2Int(t.q, t.r));
        }
        if (owned.Count == 0) { ClearMesh(); Hide(); return; }

        var verts = new List<Vector3>(owned.Count * 24);
        var tris = new List<int>(owned.Count * 36);
        var cols = new List<Color>(owned.Count * 24);

        var inv = renderChild.transform.worldToLocalMatrix;

        foreach (var tile in owned)
        {
            // 6 neighbors (axial pointy-top)
            bool[] hasN = new bool[6];
            for (int d = 0; d < 6; d++)
            {
                var dir = HexGrid.AxialDirs[d];
                hasN[d] = ownedSet.Contains(new Vector2Int(tile.q + dir.x, tile.r + dir.y));
            }

            // Bounds -> radius & center (world)
            Bounds b;
            var mc = tile.GetComponent<MeshCollider>();
            if (mc) b = mc.bounds;
            else
            {
                var bc = tile.GetComponent<BoxCollider>();
                if (bc) b = bc.bounds;
                else
                {
                    var r = tile.GetComponent<MeshRenderer>();
                    if (!r) continue;
                    b = r.bounds;
                }
            }
            float radius = HexGrid.EstimateRadiusFromBounds(b);
            Vector3 center = b.center; center.y = b.max.y + height;

            // Hex corners
            Vector3[] corners = new Vector3[6];
            HexGrid.GetHexCorners(center, radius, corners);

            // Draw along edges that face non-owned neighbors; offset outward from center
            for (int e = 0; e < 6; e++)
            {
                if (hasN[e]) continue;

                int eNext = (e + 1) % 6;
                Vector3 a = corners[e];
                Vector3 b0 = corners[eNext];

                // outward from center towards edge midpoint
                Vector3 mid = (a + b0) * 0.5f;
                Vector3 outward = (mid - center).normalized;

                float bw = borderWidth;
                Vector3 aIn = a - outward * (bw * 0.5f);
                Vector3 bIn = b0 - outward * (bw * 0.5f);
                Vector3 aOut = a + outward * (bw * 0.5f);
                Vector3 bOut = b0 + outward * (bw * 0.5f);

                // world -> local
                aIn = inv.MultiplyPoint3x4(aIn);
                bIn = inv.MultiplyPoint3x4(bIn);
                aOut = inv.MultiplyPoint3x4(aOut);
                bOut = inv.MultiplyPoint3x4(bOut);

                AddQuad(verts, tris, cols, aIn, bIn, bOut, aOut, color);
            }
        }

        var mesh = mf.sharedMesh;
        if (!mesh) { mesh = new Mesh(); mesh.name = "TerritoryOutlineHexMesh"; }
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetColors(cols);
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;

        if (mat != null)
        {
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        }

        if (!renderChild.activeSelf) renderChild.SetActive(true);
    }

    void Hide() { if (renderChild && renderChild.activeSelf) renderChild.SetActive(false); }
    void ClearMesh() { if (mf && mf.sharedMesh) mf.sharedMesh.Clear(); }

    static void AddQuad(List<Vector3> v, List<int> t, List<Color> c,
        Vector3 a, Vector3 b, Vector3 d, Vector3 e, Color col)
    {
        int idx = v.Count;
        v.Add(a); v.Add(b); v.Add(d); v.Add(e);
        c.Add(col); c.Add(col); c.Add(col); c.Add(col);
        t.Add(idx + 0); t.Add(idx + 2); t.Add(idx + 1);
        t.Add(idx + 0); t.Add(idx + 3); t.Add(idx + 2);
    }

    static void TrySetInt(Material m, string prop, int val)
    {
        if (m.HasProperty(prop)) m.SetInt(prop, val);
    }
}

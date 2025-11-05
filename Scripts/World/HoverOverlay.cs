using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class HoverOverlay : MonoBehaviour
{
    [Header("Visual")]
    public float height = 0.12f;
    public float size = 0.98f;
    public float tileSize = 1f;

    [Header("Debug")]
    public bool debugLogs = false;

    const string ChildName = "HoverOverlay_Quad";

    GameObject quad;
    MeshRenderer mr;
    Material matInstance;
    Texture2D whiteTex; // ensure sprite-like tinting multiplies a texture

    void Awake() { Ensure(); }
    void OnEnable() { Ensure(); Hide(); }
    void OnDisable() { if (quad) quad.SetActive(false); }
    void OnDestroy()
    {
        if (quad)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(quad);
            else Destroy(quad);
#else
            Destroy(quad);
#endif
            quad = null;
        }
        if (whiteTex)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(whiteTex);
            else Destroy(whiteTex);
#else
            Destroy(whiteTex);
#endif
            whiteTex = null;
        }
        if (matInstance)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(matInstance);
            else Destroy(matInstance);
#else
            Destroy(matInstance);
#endif
            matInstance = null;
        }
    }

    public void ShowAt(Vector3 worldPos, Color c)
    {
        Ensure();
        quad.SetActive(true);

        quad.transform.position = new Vector3(worldPos.x, worldPos.y + height, worldPos.z);
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(tileSize * size, tileSize * size, 1f);

        ApplyColor(c);
    }

    public void Hide()
    {
        Ensure();
        if (quad.activeSelf) quad.SetActive(false);
    }

    // ---------------- internals ----------------
    void Ensure()
    {
        if (!quad)
        {
            var existing = transform.Find(ChildName);
            if (existing) quad = existing.gameObject;
        }

        if (!quad)
        {
            quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = ChildName;
            quad.transform.SetParent(transform, false);
            var col = quad.GetComponent<Collider>();
            if (col) DestroyImmediate(col);
        }

        if (!mr) mr = quad.GetComponent<MeshRenderer>();

        if (matInstance == null)
        {
            // Use Sprites/Default – always tints by _Color over a texture
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent"); // fallback
            }
            matInstance = new Material(shader);
            matInstance.renderQueue = 3500; // transparent queue

            // Common transparent settings (work in Built-in/URP)
            TrySetInt(matInstance, "_ZWrite", 0);
            TrySetInt(matInstance, "_Surface", 1); // URP: 0=Opaque,1=Transparent
            TrySetBlend(matInstance, 5, 10);       // SrcAlpha / OneMinusSrcAlpha

            // Ensure there is a texture to be tinted
            if (!whiteTex)
            {
                whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                whiteTex.name = "HoverOverlayWhite";
                whiteTex.SetPixel(0, 0, Color.white);
                whiteTex.Apply();
            }
            if (matInstance.HasProperty("_MainTex")) matInstance.SetTexture("_MainTex", whiteTex);

            mr.sharedMaterial = matInstance; // use same instance across play/edit to avoid leaks
        }
    }

    void ApplyColor(Color c)
    {
        if (matInstance == null) return;

        // Apply to common color property names
        if (matInstance.HasProperty("_Color")) matInstance.SetColor("_Color", c);
        if (matInstance.HasProperty("_BaseColor")) matInstance.SetColor("_BaseColor", c); // URP Unlit/2D

        // Some pipelines ignore _Color alpha unless Surface=Transparent
        TrySetInt(matInstance, "_Surface", 1);
        TrySetBlend(matInstance, 5, 10);

        // Force renderer to update now
        if (mr != null) mr.enabled = true;

        if (debugLogs)
            Debug.Log($"[HoverOverlay] Shader={matInstance.shader.name} color=({c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2})");
    }

    static void TrySetInt(Material m, string prop, int val)
    {
        if (m.HasProperty(prop)) m.SetInt(prop, val);
    }

    static void TrySetBlend(Material m, int src, int dst)
    {
        // _SrcBlend/_DstBlend not present on all shaders; ignore if missing
        if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", src);
        if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", dst);
    }
}

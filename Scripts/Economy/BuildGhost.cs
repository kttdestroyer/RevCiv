using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BuildGhost : MonoBehaviour
{
    public float height = 0.15f;
    public Vector3 scale = new Vector3(0.4f, 0.3f, 0.4f);
    public Color okColor = new Color(0.2f, 1f, 0.2f, 0.6f);
    public Color blockedColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    const string ChildName = "BuildGhost_Visual";

    GameObject ghost;
    MeshRenderer mr;

    void OnEnable()
    {
        Ensure();
        Hide();
    }

    void OnDisable()
    {
        if (ghost) ghost.SetActive(false);
    }

    void OnDestroy()
    {
        if (ghost)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(ghost);
            else Destroy(ghost);
#else
            Destroy(ghost);
#endif
            ghost = null; mr = null;
        }
    }

    public void ShowAt(Vector3 pos, bool ok)
    {
        Ensure();
        ghost.SetActive(true);
        ghost.transform.position = pos + Vector3.up * height;

        if (mr != null)
        {
            var col = ok ? okColor : blockedColor;
            var mat = mr.sharedMaterial;
            if (mat == null)
            {
                mat = CreateMat();
                mr.sharedMaterial = mat;
            }
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        }
    }

    public void Hide()
    {
        Ensure();
        if (ghost && ghost.activeSelf) ghost.SetActive(false);
    }

    void Ensure()
    {
        if (!ghost)
        {
            // Reuse existing child if present (prevents stacking)
            var t = transform.Find(ChildName);
            ghost = t ? t.gameObject : null;
        }

        if (!ghost)
        {
            ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = ChildName;
            ghost.transform.SetParent(transform, false);
            ghost.transform.localScale = scale;

            var col = ghost.GetComponent<Collider>();
            if (col) DestroyImmediate(col);

            mr = ghost.GetComponent<MeshRenderer>();
            if (!mr) mr = ghost.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateMat();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        if (!mr) mr = ghost.GetComponent<MeshRenderer>();
        ghost.transform.localScale = scale; // keep scale consistent
    }

    Material CreateMat()
    {
        var shader = Shader.Find("Unlit/Color");
        if (!shader) shader = Shader.Find("Sprites/Default");
        var m = new Material(shader) { renderQueue = 3000 };
        // transparent settings (URP-safe)
        TrySetInt(m, "_Surface", 1);
        TrySetInt(m, "_ZWrite", 0);
        TrySetInt(m, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        TrySetInt(m, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        return m;
    }

    static void TrySetInt(Material m, string prop, int val)
    {
        if (m.HasProperty(prop)) m.SetInt(prop, val);
    }
}

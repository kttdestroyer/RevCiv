using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BuildGhost : MonoBehaviour
{
    public float height = 0.15f;
    public Vector3 scale = new Vector3(0.4f, 0.3f, 0.4f);
    public Color okColor = new Color(0.2f, 1f, 0.2f, 0.6f);
    public Color blockedColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    GameObject ghost;
    MeshRenderer mr;

    void OnEnable() { Ensure(); Hide(); }
    void OnDisable() { if (ghost) ghost.SetActive(false); }

    public void ShowAt(Vector3 pos, bool ok)
    {
        Ensure();
        ghost.SetActive(true);
        ghost.transform.position = pos + Vector3.up * height;
        if (mr && mr.sharedMaterial)
        {
            var col = ok ? okColor : blockedColor;
            if (mr.sharedMaterial.HasProperty("_Color")) mr.sharedMaterial.SetColor("_Color", col);
            if (mr.sharedMaterial.HasProperty("_BaseColor")) mr.sharedMaterial.SetColor("_BaseColor", col);
        }
    }

    public void Hide()
    {
        Ensure();
        if (ghost.activeSelf) ghost.SetActive(false);
    }

    void Ensure()
    {
        if (!ghost)
        {
            ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "BuildGhost";
            ghost.transform.SetParent(transform, false);
            ghost.transform.localScale = scale;
            var col = ghost.GetComponent<Collider>();
            if (col) DestroyImmediate(col);

            mr = ghost.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Unlit/Color");
            if (!shader) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { renderQueue = 3000 };
            mr.sharedMaterial = mat;

            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
    }
}

using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [Header("Grid")]
    public int width = 16;
    public int height = 16;
    public float tileSize = 1f;
    public GameObject tilePrefab; // should have TileInfo (+ BoxCollider) and optionally BordersOverlay

    [Header("Biome defaults")]
    public string defaultBiome = "Plains";
    public float defaultTemp = 0.5f;
    public float defaultMoisture = 0.5f;

    [Header("Post-generate")]
    public bool distributeAfterGenerate = true;

    public void Generate()
    {
        // clear old
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        // build grid
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize);
                GameObject go;

                if (tilePrefab != null)
                {
                    go = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                }
                else
                {
                    // fallback quad if no prefab assigned
                    go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    go.transform.position = pos;
                    go.transform.rotation = Quaternion.Euler(90, 0, 0);
                    go.transform.localScale = Vector3.one * tileSize;
                    go.transform.SetParent(transform);
                    if (!go.GetComponent<BoxCollider>()) go.AddComponent<BoxCollider>();
                }

                var ti = go.GetComponent<TileInfo>();
                if (!ti) ti = go.AddComponent<TileInfo>();

                ti.biome = defaultBiome;
                ti.temperature = defaultTemp;
                ti.moisture = defaultMoisture;
            }
        }

        // kick resource distribution (on same GameObject if present; otherwise find one in scene)
        if (distributeAfterGenerate)
            TryDistributeResources();
    }

    void TryDistributeResources()
    {
        // Prefer a distributor on the same GO
        var local = GetComponent<ResourceDistributor>();
        if (local != null) { local.Distribute(); return; }

#if UNITY_6000_0_OR_NEWER
        var any = FindFirstObjectByType<ResourceDistributor>();
#else
        var any = FindObjectOfType<ResourceDistributor>();
#endif
        if (any != null) any.Distribute();
    }

#if UNITY_EDITOR
    [ContextMenu("Generate World")]
    void EditorGenerate()
    {
        Generate();
    }
#endif
}

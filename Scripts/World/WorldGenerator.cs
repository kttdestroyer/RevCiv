using UnityEngine;

[DisallowMultipleComponent]
public class WorldGenerator : MonoBehaviour
{
    [Header("Hex Grid (pointy-top axial)")]
    public int radiusRange = 8;
    public float hexRadius = 0.5f;
    public GameObject tilePrefab;

    [Header("Clean")]
    public bool destroyChildrenOnGenerate = true;

    TileIndex index;

    void Awake()
    {
        index = GetComponent<TileIndex>();
        if (!index) index = gameObject.AddComponent<TileIndex>();
        index.hexRadius = hexRadius;
    }

    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        if (!tilePrefab)
        {
            Debug.LogError("WorldGenerator: Tile Prefab is not assigned.");
            return;
        }

        if (destroyChildrenOnGenerate)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(child.gameObject);
                else Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        int count = 0;
        for (int r = -radiusRange; r <= radiusRange; r++)
        {
            int qMin = Mathf.Max(-radiusRange, -r - radiusRange);
            int qMax = Mathf.Min(radiusRange, -r + radiusRange);
            for (int q = qMin; q <= qMax; q++)
            {
                var pos = HexGrid.AxialToWorld(q, r, hexRadius, 0f);
                var go = Object.Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                go.name = $"Tile_{q}_{r}";

                var ti = go.GetComponent<TileInfo>();
                if (!ti) ti = go.AddComponent<TileInfo>();
                ti.q = q; ti.r = r;
                count++;
            }
        }

        // rebuild lookup
        if (!index) index = GetComponent<TileIndex>() ?? gameObject.AddComponent<TileIndex>();
        index.hexRadius = hexRadius;
        index.RebuildIndex();

        Debug.Log($"WorldGenerator: Generated hex map with ~{count} tiles (R={radiusRange}, hexRadius={hexRadius}).");
    }
}

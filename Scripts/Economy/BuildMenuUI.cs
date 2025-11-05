using UnityEngine;

public class BuildMenuUI : MonoBehaviour
{
    [Header("Building defs (press 1/2/3)")]
    public BuildingDef camp;
    public BuildingDef lumberHut;
    public BuildingDef quarry;

    [Header("Preview")]
    public BuildGhost ghost;

    [Header("Debug")]
    public bool logBlockReason = true;

    BuildingDef active;

    void Awake()
    {
        if (!ghost)
        {
#if UNITY_6000_0_OR_NEWER
            ghost = FindFirstObjectByType<BuildGhost>();
#else
            ghost = FindObjectOfType<BuildGhost>();
#endif
        }
    }

    void Update()
    {
        // hotkeys to choose building
        if (Input.GetKeyDown(KeyCode.Alpha1)) active = camp;
        if (Input.GetKeyDown(KeyCode.Alpha2)) active = lumberHut;
        if (Input.GetKeyDown(KeyCode.Alpha3)) active = quarry;

        if (!active)
        {
            if (ghost) ghost.Hide();
            return;
        }

        var cam = Camera.main;
        if (!cam) return;

        if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 500f))
        {
            if (ghost) ghost.Hide();
            return;
        }

        var tile = hit.collider.GetComponentInParent<TileInfo>();
        if (!tile)
        {
            if (ghost) ghost.Hide();
            return;
        }

        // --- validate
        bool ok = ValidateTileFor(active, tile, out string reason);

        // --- ghost
        if (ghost) ghost.ShowAt(tile.transform.position, ok);

        // --- place
        if (ok && Input.GetMouseButtonDown(0))
        {
            PlaceAt(tile, active);
        }
        else if (!ok && Input.GetMouseButtonDown(0))
        {
            if (logBlockReason) Debug.Log($"Build blocked: {reason}");
        }

        // ESC to cancel active building
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            active = null;
            if (ghost) ghost.Hide();
        }
    }

    bool ValidateTileFor(BuildingDef def, TileInfo tile, out string reason)
    {
        // must be owned
        if (tile.ownerSettlementId < 0) { reason = "Tile not owned."; return false; }

        // must be empty
        if (tile.buildingInstance) { reason = "Tile already has a building."; return false; }

        // biome requirement
        if (!string.IsNullOrEmpty(def.requiredBiomeContains))
        {
            string b = (tile.biome ?? "").ToLower();
            string sb = (tile.subBiome ?? "").ToLower();
            string req = def.requiredBiomeContains.ToLower();

            if (!(b.Contains(req) || sb.Contains(req)))
            { reason = $"Biome rule not met (need '{def.requiredBiomeContains}', have '{tile.biome}/{tile.subBiome}')."; return false; }
        }

        reason = "";
        return true;
    }

    void PlaceAt(TileInfo tile, BuildingDef def)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.position = tile.transform.position + new Vector3(0f, 0.15f, 0f);
        go.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);

        var inst = go.AddComponent<BuildingInstance>();
        inst.ApplyTo(tile, def);
    }
}

using UnityEngine;

[DisallowMultipleComponent]
public class BuildingInstance : MonoBehaviour
{
    [Header("Data")]
    public BuildingDef def;
    public TileInfo tile;

    /// <summary>
    /// Binds this instance to a tile and a building definition.
    /// Also sets the tile's building reference.
    /// </summary>
    public void ApplyTo(TileInfo targetTile, BuildingDef buildingDef)
    {
        tile = targetTile;
        def = buildingDef;

        if (tile != null)
        {
            // Clear any previous instance reference safely
            if (tile.buildingInstance != null && tile.buildingInstance != this)
            {
                var prev = tile.buildingInstance;
                tile.buildingInstance = null;
                if (prev) Destroy(prev.gameObject);
            }

            tile.buildingInstance = this;

            // Snap to the tile top so it sits visually correct
            var bc = tile.GetComponent<BoxCollider>();
            float y = (bc != null) ? bc.bounds.max.y : tile.transform.position.y + 0.15f;

            transform.position = new Vector3(
                tile.transform.position.x,
                y,
                tile.transform.position.z
            );
        }
    }

    void OnDestroy()
    {
        // If this building is removed, clear the tile link
        if (tile && tile.buildingInstance == this)
            tile.buildingInstance = null;
    }
}

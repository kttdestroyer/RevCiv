using UnityEngine;

public class BuildingInstance : MonoBehaviour
{
    public BuildingDef def;
    public TileInfo tile;

    public void ApplyTo(TileInfo t, BuildingDef d)
    {
        tile = t;
        def = d;
        t.buildingInstance = gameObject;
    }
}

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RevCiv/DataRegistry")]
public class DataRegistry : ScriptableObject
{
    public List<ResourceDef> resources = new();
    public List<BuildingDef> buildings = new();
    public List<UnitDef> units = new();

    public ResourceDef FindResource(string id)
    {
        for (int i = 0; i < resources.Count; i++)
            if (resources[i] && resources[i].id == id) return resources[i];
        return null;
    }

    public BuildingDef FindBuilding(string id)
    {
        for (int i = 0; i < buildings.Count; i++)
            if (buildings[i] && buildings[i].id == id) return buildings[i];
        return null;
    }

    public UnitDef FindUnit(string id)
    {
        for (int i = 0; i < units.Count; i++)
            if (units[i] && units[i].id == id) return units[i];
        return null;
    }
}

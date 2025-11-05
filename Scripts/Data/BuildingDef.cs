using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ResourceAmount
{
    public ResourceDef resource;
    public int amount;
}

[CreateAssetMenu(menuName = "RevCiv/BuildingDef")]
public class BuildingDef : ScriptableObject
{
    public string id;                  // e.g. "camp", "lumber_hut"
    public string displayName;
    public Sprite icon;

    [Header("Cost & Upkeep")]
    public List<ResourceAmount> buildCost = new();
    public List<ResourceAmount> upkeep = new();

    [Header("Placement Rules")]
    public string requiredBiomeContains;   // "Forest", "Hill", etc.
    public bool landOnly = true;

    [Header("Yield Bonus (adds per day to that tile)")]
    public List<ResourceAmount> yieldBonus = new();
}

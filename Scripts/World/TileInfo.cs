using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ResourceStack
{
    public ResourceDef resource;
    public int amountPerDay;
}

[ExecuteAlways]
public class TileInfo : MonoBehaviour
{
    [Header("Environment")]
    public string biome = "Plains";
    public string subBiome = "";
    [Range(0, 1)] public float temperature = 0.5f;
    [Range(0, 1)] public float moisture = 0.5f;

    [Header("Resources")]
    public List<ResourceStack> resources = new();
    public bool discovered = true;
    public bool worked = false;

    [HideInInspector] public int ownerSettlementId = -1;
    [HideInInspector] public GameObject buildingInstance;

    [Header("Gizmo (per-tile)")]
    [Tooltip("Show this tile's resource dots (useful in editor).")]
    public bool showTileDots = true;

    [Tooltip("Size of this tile's dots when shown from TileInfo.")]
    public float tileDotSize = 0.07f;

    public int GetYield(ResourceDef res)
    {
        for (int i = 0; i < resources.Count; i++)
            if (resources[i].resource == res) return resources[i].amountPerDay;
        return 0;
    }

    public void NaturalRecovery()
    {
        // placeholder for future regrowth logic
    }

    void OnDrawGizmos()
    {
        if (!showTileDots) return;
        if (resources == null || resources.Count == 0) return;

        Vector3 p = transform.position + Vector3.up * 0.25f;
        float off = 0f;
        for (int i = 0; i < resources.Count; i++)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(p + new Vector3(off, 0, 0), tileDotSize);
            off += tileDotSize * 2f;
        }
    }
}

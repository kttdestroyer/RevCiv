using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceStack
{
    [Tooltip("Optional: link to the ResourceDef asset. (SaveManager expects this field.)")]
    public ResourceDef resource;

    [Tooltip("Fallback key/name used if 'resource' is not assigned.")]
    public string resourceName;

    [Tooltip("Base yield per in-game day for this resource on this tile.")]
    public int amountPerDay = 0;

    /// <summary>
    /// Safe identifier for this resource entry. Uses ResourceDef.name if present, else the fallback string.
    /// </summary>
    public string Key => resource ? resource.name : (string.IsNullOrEmpty(resourceName) ? "" : resourceName);
}

[DisallowMultipleComponent]
public class TileInfo : MonoBehaviour
{
    [Header("World/Climate")]
    public string biome;
    public string subBiome;
    public float temperature;
    public float moisture;

    [Header("Resources (static per tile)")]
    public List<ResourceStack> resources = new List<ResourceStack>();

    [Header("Ownership / Runtime")]
    public int ownerSettlementId = -1;
    public BuildingInstance buildingInstance;
    [Tooltip("True when a worker is currently assigned to work this tile.")]
    public bool worked = false;

    [Header("Grid (hex axial coords)")]
    [Tooltip("Axial Q coordinate (pointy-top).")]
    public int q;
    [Tooltip("Axial R coordinate (pointy-top).")]
    public int r;

    // Back-compat for any consumers (mirrors q/r)
    [Obsolete("Use q/r axial coords.")]
    public int gx { get => q; set => q = value; }
    [Obsolete("Use q/r axial coords.")]
    public int gz { get => r; set => r = value; }

    [Header("Gizmos (optional)")]
    public bool drawCenterGizmo = false;
    public Color gizmoColor = Color.white;

    void OnValidate()
    {
        // Keep the fallback name in sync if a ResourceDef is assigned
        if (resources != null)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                var rs = resources[i];
                if (rs == null) continue;
                if (rs.resource != null) rs.resourceName = rs.resource.name;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawCenterGizmo) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position + Vector3.up * 0.1f, 0.05f);
    }
}

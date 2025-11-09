using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Settlement : MonoBehaviour
{
    [Header("Identity")]
    public int settlementId = -1;
    public string settlementName = "Settlement";
    public int population = 0;   // <- added to satisfy HUD

    [Header("Territory")]
    [Tooltip("How far (in hexes) this settlement initially claims.")]
    public int claimRadius = 2;

    [Header("UI/Outline (optional)")]
    public TerritoryOutline outline; // auto-found

    // -------------------------------
    // Resource model (minimal runtime)
    // -------------------------------
    // Keys are resource names (ResourceDef.name)
    Dictionary<string, int> stockpile = new Dictionary<string, int>();
    Dictionary<string, int> deltaPerDay = new Dictionary<string, int>();

    // Convenience: common keys
    public static readonly string[] DefaultResources =
        { "Food", "Wood", "Stone", "Metal", "Knowledge" };

    void Awake()
    {
        if (!outline) outline = GetComponent<TerritoryOutline>();
        EnsureDefaultKeys();
    }

    void Start()
    {
        if (settlementId < 0) settlementId = GetInstanceID() & 0x7FFFFFFF;
        ClaimInitialTerritoryHex();
        if (outline) outline.Rebuild();
    }

    // -------------------------------
    // Resource API — string overloads
    // -------------------------------
    public int Get(string resourceName) =>
        string.IsNullOrEmpty(resourceName) ? 0 :
        (stockpile.TryGetValue(resourceName, out var v) ? v : 0);

    public void Set(string resourceName, int value)
    {
        if (string.IsNullOrEmpty(resourceName)) return;
        stockpile[resourceName] = value;
    }

    public void Add(string resourceName, int delta)
    {
        if (string.IsNullOrEmpty(resourceName)) return;
        stockpile[resourceName] = Get(resourceName) + delta;
    }

    public bool Spend(string resourceName, int amount)
    {
        if (amount <= 0) return true;
        int cur = Get(resourceName);
        if (cur < amount) return false;
        stockpile[resourceName] = cur - amount;
        return true;
    }

    public int GetDelta(string resourceName) =>
        string.IsNullOrEmpty(resourceName) ? 0 :
        (deltaPerDay.TryGetValue(resourceName, out var v) ? v : 0);

    public void SetDelta(string resourceName, int delta)
    {
        if (string.IsNullOrEmpty(resourceName)) return;
        deltaPerDay[resourceName] = delta;
    }

    public void AddDelta(string resourceName, int delta)
    {
        if (string.IsNullOrEmpty(resourceName)) return;
        deltaPerDay[resourceName] = GetDelta(resourceName) + delta;
    }

    // -------------------------------
    // Resource API — ResourceDef overloads
    // -------------------------------
    public int Get(ResourceDef def) => def ? Get(def.name) : 0;
    public void Set(ResourceDef def, int value) { if (def) Set(def.name, value); }
    public void Add(ResourceDef def, int delta) { if (def) Add(def.name, delta); }
    public bool Spend(ResourceDef def, int amount) => def && Spend(def.name, amount);
    public int GetDelta(ResourceDef def) => def ? GetDelta(def.name) : 0;
    public void SetDelta(ResourceDef def, int delta) { if (def) SetDelta(def.name, delta); }
    public void AddDelta(ResourceDef def, int delta) { if (def) AddDelta(def.name, delta); }

    /// Apply one in-game day of delta to stockpile.
    public void ApplyDailyTick()
    {
        foreach (var kv in deltaPerDay)
            Add(kv.Key, kv.Value);
    }

    public void EnsureDefaultKeys()
    {
        foreach (var key in DefaultResources)
        {
            if (!stockpile.ContainsKey(key)) stockpile[key] = 0;
            if (!deltaPerDay.ContainsKey(key)) deltaPerDay[key] = 0;
        }
    }

    // -------------------------------
    // Territory claiming (hex aware)
    // -------------------------------
    public void ClaimInitialTerritoryHex()
    {
#if UNITY_6000_0_OR_NEWER
        var tiles = FindObjectsByType<TileInfo>(FindObjectsSortMode.None);
#else
        var tiles = FindObjectsOfType<TileInfo>();
#endif
        if (tiles == null || tiles.Length == 0) return;

        // Pick nearest tile to this settlement position
        TileInfo centerTile = null;
        float best = float.MaxValue;
        Vector3 p = transform.position;
        foreach (var t in tiles)
        {
            if (!t) continue;
            float d = (t.transform.position - p).sqrMagnitude;
            if (d < best) { best = d; centerTile = t; }
        }
        if (!centerTile) return;

        int cq = centerTile.q;
        int cr = centerTile.r;

        // Lookup by axial
        var byAxial = new Dictionary<Vector2Int, TileInfo>(tiles.Length);
        foreach (var t in tiles)
        {
            if (!t) continue;
            byAxial[new Vector2Int(t.q, t.r)] = t;
        }

        // Claim solid axial radius
        HexGrid.ForEachInRange(cq, cr, claimRadius, (q, r) =>
        {
            var key = new Vector2Int(q, r);
            if (byAxial.TryGetValue(key, out var tile))
                tile.ownerSettlementId = settlementId;
        });
    }
}

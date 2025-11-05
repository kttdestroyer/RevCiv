using System.Collections.Generic;
using UnityEngine;

public class Settlement : MonoBehaviour
{
    public int settlementId;
    public string settlementName = "Settlement";
    public int population = 2;
    public int workRadius = 2;

    [System.Serializable]
    public class Stock
    {
        public ResourceDef resource;
        public int amount;
    }
    public List<Stock> stockpile = new();

    [Header("Growth")]
    public ResourceDef foodResource;
    public int foodPerPopPerDay = 1;
    public int surplusNeededForGrowth = 4;
    int surplusCounter = 0;

    List<TileInfo> cachedTilesInRadius = new();

    void OnEnable() { TimeController.OnDailyTick += OnTick; }
    void OnDisable() { TimeController.OnDailyTick -= OnTick; }

    public void InitOwnership()
    {
        cachedTilesInRadius.Clear();
        var tiles = Object.FindObjectsByType<TileInfo>(FindObjectsSortMode.None);
        foreach (var ti in tiles)
        {
            if (InRadius(ti))
            {
                ti.ownerSettlementId = settlementId;
                cachedTilesInRadius.Add(ti);
            }
        }
    }

    bool InRadius(TileInfo ti)
    {
        var a = new Vector2(transform.position.x, transform.position.z);
        var b = new Vector2(ti.transform.position.x, ti.transform.position.z);
        return Vector2.Distance(a, b) <= workRadius + 0.1f;
    }

    void OnTick()
    {
        int pop = Mathf.Max(population, 0);

        // 1) Food consumption / growth
        if (foodResource)
        {
            int need = pop * foodPerPopPerDay;
            int have = Get(foodResource);
            int delta = have - need;

            if (delta >= 0)
            {
                Add(foodResource, -need);
                surplusCounter += delta;
            }
            else
            {
                int take = Mathf.Min(have, need);
                Add(foodResource, -take);
                if (population > 0) population -= 1;
                surplusCounter = 0;
            }

            if (surplusCounter >= surplusNeededForGrowth)
            {
                population += 1;
                surplusCounter = 0;
            }
        }

        // 2) Tile production
        int maxWorked = pop;
        int workedSoFar = 0;
        foreach (var ti in cachedTilesInRadius)
        {
            if (!ti.worked) continue;
            if (workedSoFar >= maxWorked) break;

            foreach (var rs in ti.resources)
            {
                int amount = rs.amountPerDay;
                amount = Mathf.RoundToInt(amount * 1.5f);  // worked multiplier

                // building bonus
                var bi = ti.buildingInstance ? ti.buildingInstance.GetComponent<BuildingInstance>() : null;
                if (bi && bi.def != null && bi.def.yieldBonus != null)
                {
                    foreach (var yb in bi.def.yieldBonus)
                        if (yb.resource == rs.resource)
                            amount += yb.amount;
                }

                Add(rs.resource, amount);
            }

            workedSoFar++;
        }
    }

    public int Get(ResourceDef r)
    {
        foreach (var s in stockpile)
            if (s.resource == r) return s.amount;
        return 0;
    }

    public void Add(ResourceDef r, int delta)
    {
        foreach (var s in stockpile)
        {
            if (s.resource == r)
            {
                s.amount = Mathf.Max(0, s.amount + delta);
                return;
            }
        }
        stockpile.Add(new Stock { resource = r, amount = Mathf.Max(0, delta) });
    }
}

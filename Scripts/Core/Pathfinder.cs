using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public static Pathfinder I;

    void Awake()
    {
        if (I != null && I != this) Destroy(gameObject);
        else I = this;
    }

    public List<TileInfo> GetNeighbors(TileInfo t, float range = 1.5f)
    {
        var all = Object.FindObjectsByType<TileInfo>(FindObjectsSortMode.None);
        var res = new List<TileInfo>();
        Vector3 p = t.transform.position;
        foreach (var n in all)
        {
            if (n == t) continue;
            if (Vector3.Distance(p, n.transform.position) <= range + 0.05f)
                res.Add(n);
        }
        return res;
    }

    public List<TileInfo> FindPath(TileInfo start, TileInfo goal)
    {
        if (!start || !goal) return null;

        var open = new List<TileInfo> { start };
        var came = new Dictionary<TileInfo, TileInfo>();
        var g = new Dictionary<TileInfo, float> { [start] = 0f };
        var f = new Dictionary<TileInfo, float> { [start] = Heu(start, goal) };

        while (open.Count > 0)
        {
            TileInfo current = open[0];
            float bestF = f[current];
            for (int i = 1; i < open.Count; i++)
            {
                float fi = f.ContainsKey(open[i]) ? f[open[i]] : float.PositiveInfinity;
                if (fi < bestF)
                {
                    current = open[i];
                    bestF = fi;
                }
            }

            if (current == goal)
                return Reconstruct(came, current);

            open.Remove(current);
            var neighbors = GetNeighbors(current);
            foreach (var nb in neighbors)
            {
                float cost = g[current] + StepCost(current, nb);
                if (!g.ContainsKey(nb) || cost < g[nb])
                {
                    came[nb] = current;
                    g[nb] = cost;
                    f[nb] = cost + Heu(nb, goal);
                    if (!open.Contains(nb)) open.Add(nb);
                }
            }
        }

        return null;
    }

    float Heu(TileInfo a, TileInfo b) => Vector3.Distance(a.transform.position, b.transform.position);

    float StepCost(TileInfo a, TileInfo b)
    {
        float c = 1f;
        if (!string.IsNullOrEmpty(b.subBiome))
        {
            string sb = b.subBiome.ToLower();
            if (sb.Contains("hill")) c += 1f;
            if (sb.Contains("forest")) c += 1f;
            if (sb.Contains("water") || sb.Contains("ocean")) c = 9999f;
        }
        return c;
    }

    List<TileInfo> Reconstruct(Dictionary<TileInfo, TileInfo> came, TileInfo cur)
    {
        var list = new List<TileInfo> { cur };
        while (came.ContainsKey(cur))
        {
            cur = came[cur];
            list.Insert(0, cur);
        }
        return list;
    }
}

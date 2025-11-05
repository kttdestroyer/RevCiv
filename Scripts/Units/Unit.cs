using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public UnitDef def;
    public TileInfo currentTile;
    public List<TileInfo> path = new();
    int pathIndex = 0;
    int movesLeft = 0;

    void OnEnable()
    {
        TimeController.OnDailyTick += OnTick;
        movesLeft = def ? def.movePoints : 3;
    }

    void OnDisable()
    {
        TimeController.OnDailyTick -= OnTick;
    }

    public void SetTile(TileInfo t)
    {
        currentTile = t;
        transform.position = t.transform.position + Vector3.up * 0.2f;
    }

    public void SetPath(List<TileInfo> newPath)
    {
        path = newPath ?? new List<TileInfo>();
        pathIndex = 0;
    }

    void OnTick()
    {
        movesLeft = def ? def.movePoints : 3;
        while (movesLeft > 0 && path != null && pathIndex < path.Count)
        {
            var next = path[pathIndex];
            if (next == currentTile)
            {
                pathIndex++;
                continue;
            }

            int stepCost = 1;
            movesLeft -= stepCost;
            SetTile(next);
            pathIndex++;
        }
    }
}

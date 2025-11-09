using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TileIndex : MonoBehaviour
{
    public float groundY = 0f;               // y where tiles sit
    public float hexRadius = 0.5f;           // must match WorldGenerator
    public Dictionary<Vector2Int, TileInfo> map = new Dictionary<Vector2Int, TileInfo>();

    public void RebuildIndex()
    {
        map.Clear();
#if UNITY_6000_0_OR_NEWER
        var tiles = FindObjectsByType<TileInfo>(FindObjectsSortMode.None);
#else
        var tiles = Object.FindObjectsOfType<TileInfo>();
#endif
        foreach (var t in tiles)
        {
            var key = new Vector2Int(t.q, t.r);
            if (!map.ContainsKey(key)) map.Add(key, t);
        }
    }

    public TileInfo GetTile(Vector2Int qr)
    {
        map.TryGetValue(qr, out var t);
        return t;
    }
}

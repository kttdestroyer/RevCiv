using UnityEngine;

[CreateAssetMenu(menuName = "RevCiv/UnitDef")]
public class UnitDef : ScriptableObject
{
    public string id;               // "worker"
    public string displayName;      // "Worker"
    public int movePoints = 4;      // per day
    public int carryCap = 10;
    public int upkeepPerDay = 0;
    public bool canWorkTiles = true;
}

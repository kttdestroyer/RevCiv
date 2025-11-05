using UnityEngine;

public class WorkerController : MonoBehaviour
{
    public Unit unit;
    public bool assignedToWork = false;
    public TileInfo workTarget;

    public void AssignWork(TileInfo t)
    {
        workTarget = t;
        assignedToWork = true;

        if (unit.currentTile != t)
        {
            var p = Pathfinder.I != null ? Pathfinder.I.FindPath(unit.currentTile, t) : null;
            unit.SetPath(p);
        }
    }

    void Update()
    {
        if (assignedToWork && unit.currentTile == workTarget)
        {
            workTarget.worked = true;
            assignedToWork = false;
        }
    }
}

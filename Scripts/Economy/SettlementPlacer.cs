using UnityEngine;

public class SettlementPlacer : MonoBehaviour
{
    [Header("Placement")]
    public GameObject settlementPrefab;
    public ResourceDef foodResource;

    [Header("Options")]
    public bool spawnWorkerOnPlace = true;
    public UnitDef workerDef;
    [Tooltip("Minimum distance between settlements.")]
    public float minSettlementSpacing = 2.0f;

    [Tooltip("Show a floating message if placement is blocked.")]
    public bool showBlockMessage = true;

    int nextId = 0;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var cam = Camera.main;
            if (!cam) return;

            if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 500f))
                return;

            var ti = hit.collider.GetComponentInParent<TileInfo>();
            if (!ti) return;

            // Spacing rule
            if (!CanPlaceAt(ti.transform.position, out string reason))
            {
                if (showBlockMessage) Debug.Log(reason);
                return;
            }

            // Place settlement
            var go = Instantiate(settlementPrefab, ti.transform.position + Vector3.up * 0.1f, Quaternion.identity);
            var s = go.GetComponent<Settlement>();
            s.settlementId = nextId++;
            s.foodResource = foodResource;
            s.InitOwnership();

            // Optional: spawn worker
            if (spawnWorkerOnPlace && workerDef != null)
            {
                var uGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                uGo.transform.localScale *= 0.6f;

                var unit = uGo.AddComponent<Unit>();
                unit.def = workerDef;

                var wc = uGo.AddComponent<WorkerController>();
                wc.unit = unit;

                unit.SetTile(ti);
            }
        }
    }

    bool CanPlaceAt(Vector3 pos, out string reason)
    {
        reason = "";
#if UNITY_6000_0_OR_NEWER
        var all = FindObjectsByType<Settlement>(FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<Settlement>();
#endif
        foreach (var s in all)
        {
            float d = Vector3.Distance(pos, s.transform.position);
            if (d < minSettlementSpacing)
            {
                reason = $"Placement blocked: too close to settlement '{s.settlementName}' (distance {d:0.0} < {minSettlementSpacing:0.0}).";
                return false;
            }
        }
        return true;
    }
}

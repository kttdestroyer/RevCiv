using System.Linq;
using UnityEngine;

public class SelectionController : MonoBehaviour
{
    public Camera cam;

    [Header("Selection")]
    public Unit selectedUnit;
    public Settlement selectedSettlement;

    [Header("Hover")]
    public HoverOverlay hoverOverlay;
    public Color hoverOk = new Color(0.2f, 1f, 0.2f, 0.55f);
    public Color hoverBlocked = new Color(1f, 0.25f, 0.25f, 0.55f);
    public Color hoverNeutral = new Color(1f, 1f, 1f, 0.35f);

    [Header("Raycast")]
    public LayerMask raycastMask = ~0;
    public float overlapProbeRadius = 0.9f;

    [Header("Debug")]
    public bool debugHover = false;

    void Awake()
    {
        if (!cam) cam = Camera.main;
#if UNITY_6000_0_OR_NEWER
        if (!hoverOverlay) hoverOverlay = FindFirstObjectByType<HoverOverlay>();
#else
        if (!hoverOverlay) hoverOverlay = FindObjectOfType<HoverOverlay>();
#endif
    }

    void Update()
    {
        if (!cam) cam = Camera.main;

        UpdateHover();

        if (Input.GetMouseButtonDown(0))
            HandleLeftClick();

        if (Input.GetMouseButtonDown(1) && selectedUnit)
        {
            var tile = GetTileUnderCursor();
            if (!tile) return;
            var p = Pathfinder.I != null ? Pathfinder.I.FindPath(selectedUnit.currentTile, tile) : null;
            selectedUnit.SetPath(p);
        }
    }

    void UpdateHover()
    {
        if (!hoverOverlay) return;

        var tile = GetTileUnderCursor(out var worldPoint);

        Color c = hoverNeutral;
        string reason = "neutral: no unit selected";
        if (selectedUnit)
        {
            if (tile)
            {
                bool owned = tile.ownerSettlementId >= 0;
                c = owned ? hoverOk : hoverBlocked;
                reason = owned ? "OK: owned tile" : "BLOCKED: not owned";
            }
            else
            {
                c = hoverBlocked;
                reason = "BLOCKED: no tile under cursor";
            }
        }

        if (tile) hoverOverlay.ShowAt(tile.transform.position, c);
        else if (worldPoint != Vector3.zero) hoverOverlay.ShowAt(worldPoint, c);
        else hoverOverlay.Hide();

        if (debugHover)
        {
            string sel = selectedUnit ? "unit=YES" : "unit=NO";
            string tileInfo = tile ? $"tile=YES owner={tile.ownerSettlementId}" : "tile=NO";
            Debug.Log($"[Hover] {sel} | {tileInfo} | {reason} | color={c}");
        }
    }

    void HandleLeftClick()
    {
        if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 500f, raycastMask))
        {
            var u = hit.collider.GetComponentInParent<Unit>();
            if (u)
            {
                selectedUnit = u;
                return;
            }

            var s = hit.collider.GetComponentInParent<Settlement>();
            if (s)
            {
                selectedSettlement = s;
                return;
            }
        }

        if (selectedUnit)
        {
            var tile = GetTileUnderCursor();
            if (tile)
            {
                var wc = selectedUnit.GetComponent<WorkerController>();
                if (wc != null) wc.AssignWork(tile);
            }
        }
    }

    TileInfo GetTileUnderCursor() => GetTileUnderCursor(out _);

    TileInfo GetTileUnderCursor(out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 500f, raycastMask);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                hitPoint = h.point;

                var tile = h.collider.GetComponentInParent<TileInfo>();
                if (tile) return tile;

                var u = h.collider.GetComponentInParent<Unit>();
                if (u && u.currentTile) return u.currentTile;

                var s = h.collider.GetComponentInParent<Settlement>();
                if (s)
                {
                    var downTile = RaycastDownToTile(h.point + Vector3.up * 2f);
                    if (downTile) return downTile;
                }
            }

            var near = Physics.OverlapSphere(hits[0].point + Vector3.up * 0.1f, overlapProbeRadius, raycastMask);
            if (near != null && near.Length > 0)
            {
                var candidate = near.Select(c => c.GetComponentInParent<TileInfo>())
                                    .FirstOrDefault(t => t != null);
                if (candidate) return candidate;
            }

            var fallback = RaycastDownToTile(hits[0].point + Vector3.up * 2f);
            if (fallback) return fallback;
        }

        return null;
    }

    TileInfo RaycastDownToTile(Vector3 from)
    {
        if (Physics.Raycast(from, Vector3.down, out var downHit, 10f, raycastMask))
            return downHit.collider.GetComponentInParent<TileInfo>();
        return null;
    }
}

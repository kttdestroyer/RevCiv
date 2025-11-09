using UnityEngine;

[DisallowMultipleComponent]
public class SelectionController : MonoBehaviour
{
    [Header("References")]
    public Camera sceneCamera;
    public HoverOverlay hoverOverlay;
    public TileIndex tileIndex; // assign MapRoot (has TileIndex)

    [Header("Hover Colors")]
    public Color hoverIdle = new Color(1f, 1f, 1f, 0.65f);
    public Color hoverOk = new Color(0.2f, 1f, 0.2f, 0.65f);

    [Header("Selection (runtime)")]
    public Unit selectedUnit;
    public Settlement selectedSettlement;

    void Awake()
    {
        if (!sceneCamera) sceneCamera = Camera.main;

        // Non-deprecated find with version guards
#if UNITY_6000_0_OR_NEWER
        if (!tileIndex) tileIndex = Object.FindFirstObjectByType<TileIndex>();
        if (!hoverOverlay) hoverOverlay = Object.FindFirstObjectByType<HoverOverlay>();
#else
        // Legacy fallback for pre-6000
        if (!tileIndex) tileIndex = Object.FindObjectOfType<TileIndex>();
        if (!hoverOverlay) hoverOverlay = Object.FindObjectOfType<HoverOverlay>();
#endif
    }

    void Update()
    {
        HandleHover();
        HandleClicks();
    }

    void HandleHover()
    {
        if (!sceneCamera || !tileIndex)
        {
            if (hoverOverlay) hoverOverlay.Hide();
            return;
        }

        var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
        if (HexPicker.RayToAxial(sceneCamera, ray, tileIndex.groundY, tileIndex.hexRadius, out var qr, out _))
        {
            var tile = tileIndex.GetTile(qr);
            if (tile)
            {
                var c = selectedUnit ? hoverOk : hoverIdle;
                if (hoverOverlay) hoverOverlay.ShowAt(tile, c);
                return;
            }
        }
        if (hoverOverlay) hoverOverlay.Hide();
    }

    void HandleClicks()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            if (tileIndex && HexPicker.RayToAxial(sceneCamera, ray, tileIndex.groundY, tileIndex.hexRadius, out var qr, out _))
            {
                var tile = tileIndex.GetTile(qr);
                if (tile)
                {
                    ClearSelection();
                    Debug.Log($"[Select] Tile q={tile.q} r={tile.r}");
                    return;
                }
            }
            ClearSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) ClearSelection();
    }

    public void ClearSelection()
    {
        selectedUnit = null;
        selectedSettlement = null;
        Debug.Log("[Select] Cleared");
    }
}

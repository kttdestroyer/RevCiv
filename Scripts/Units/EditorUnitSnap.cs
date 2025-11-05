using UnityEngine;

public class EditorUnitSnap : MonoBehaviour
{
    public Unit unit;
    public TileInfo tile;

    void OnGUI()
    {
        if (!Application.isPlaying) return;
        if (unit && tile)
        {
            if (GUI.Button(new Rect(10, 10, 160, 30), "Snap Unit To Tile"))
                unit.SetTile(tile);
        }
    }
}

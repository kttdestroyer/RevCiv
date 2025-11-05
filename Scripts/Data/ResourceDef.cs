using UnityEngine;

[CreateAssetMenu(menuName = "RevCiv/ResourceDef")]
public class ResourceDef : ScriptableObject
{
    [Tooltip("Unique id, e.g. 'food', 'wood'")]
    public string id;
    public string displayName;
    public Sprite icon;
    [Min(0)] public int baseYield = 1;
}

using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ResourceDistributor : MonoBehaviour
{
    [Header("Profiles & Data")]
    public BiomeResourceProfile profile;
    public List<ResourceDef> allResources = new();

    [Header("Gizmo Dots")]
    [Tooltip("Show resource dots without needing to press R. Works in Edit and Play.")]
    public bool showGizmos = true;

    [Tooltip("Size of the resource dots.")]
    public float gizmoIconSize = 0.12f;

    [Tooltip("Enable keyboard toggle (R) in Play mode. Requires old Input Manager or Both.")]
    public bool enableKeyboardToggle = true;

    [Tooltip("Key used to toggle dots on/off at runtime.")]
    public KeyCode toggleIconsKey = KeyCode.R;

    [Header("Runtime")]
    public bool autoRunOnStart = true;

    private bool runtimeToggled = false;  // only used when enableKeyboardToggle = true
    private readonly List<TileInfo> tiles = new();

    void Start()
    {
        if (Application.isPlaying && autoRunOnStart)
            Distribute();
    }

    public void Distribute()
    {
        tiles.Clear();
#if UNITY_6000_0_OR_NEWER
        tiles.AddRange(Object.FindObjectsByType<TileInfo>(FindObjectsSortMode.None));
#else
        tiles.AddRange(Object.FindObjectsOfType<TileInfo>());
#endif

        int seed = (int)(Random.value * 999999) + (profile ? profile.seedOffset : 0);

        foreach (var tile in tiles)
        {
            if (!tile) continue;
            tile.resources.Clear();

            foreach (var res in allResources)
            {
                if (!res) continue;

                float weight = GetWeight(tile.biome, res);
                if (weight <= 0f) continue;

                float noiseScale = profile ? profile.perlinScale : 0.1f;
                float nx = (tile.transform.position.x + seed) * noiseScale;
                float ny = (tile.transform.position.z + seed) * noiseScale;
                float noise = Mathf.PerlinNoise(nx, ny); // 0..1

                int amount = Mathf.RoundToInt(weight * (0.2f + noise));
                if (amount > 0)
                    tile.resources.Add(new ResourceStack { resource = res, amountPerDay = amount });
            }
        }
    }

    float GetWeight(string biome, ResourceDef res)
    {
        if (!profile || profile.weights == null) return 0f;

        float total = 0f;
        for (int i = 0; i < profile.weights.Length; i++)
        {
            var w = profile.weights[i];
            if (w.resource != res) continue;
            if (!string.IsNullOrEmpty(w.biomeContains) &&
                !string.IsNullOrEmpty(biome) &&
                biome.ToLower().Contains(w.biomeContains.ToLower()))
            {
                total += w.weight;
            }
        }
        return total;
    }

    void Update()
    {
        // Runtime toggle with keyboard (optional)
        if (Application.isPlaying && enableKeyboardToggle && Input.GetKeyDown(toggleIconsKey))
        {
            runtimeToggled = !runtimeToggled;
        }
    }

    void OnDrawGizmos()
    {
        // Decide visibility
        bool visible = showGizmos || (Application.isPlaying && runtimeToggled);
        if (!visible) return;

        // Ensure we have tiles to iterate (works in Edit mode too)
        if (tiles.Count == 0)
        {
#if UNITY_6000_0_OR_NEWER
            tiles.AddRange(Object.FindObjectsByType<TileInfo>(FindObjectsSortMode.None));
#else
            tiles.AddRange(Object.FindObjectsOfType<TileInfo>());
#endif
        }

        foreach (var t in tiles)
        {
            if (!t || t.resources == null || t.resources.Count == 0) continue;

            Vector3 p = t.transform.position + Vector3.up * 0.1f;
            float off = 0f;
            for (int i = 0; i < t.resources.Count; i++)
            {
                // One color for all for now; can map per resource later
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(p + new Vector3(off, 0, 0), gizmoIconSize);
                off += gizmoIconSize * 1.8f;
            }
        }
    }
}

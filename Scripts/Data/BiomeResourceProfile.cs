using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RevCiv/BiomeResourceProfile")]
public class BiomeResourceProfile : ScriptableObject
{
    [Serializable]
    public class BiomeWeight
    {
        public string biomeContains; // e.g. "Forest", "Hill", "Plains"
        public ResourceDef resource;
        [Range(0f, 5f)] public float weight = 1f;
    }

    public BiomeWeight[] weights;
    [Range(0f, 1f)] public float perlinScale = 0.1f;
    public int seedOffset = 1337;
}

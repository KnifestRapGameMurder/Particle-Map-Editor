#if UNITY_EDITOR
using TriInspector;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareFoldoutGroup(Constants.Dev)]
    [CreateAssetMenu(menuName = nameof(IslandMeshGeneratingSettings))]
    public class IslandMeshGeneratingSettings : ScriptableObject
    {
        public Texture2D islandTexture;

        //public int areaSize = 100;
        [Range(1, 5)] public int pixelsPerUnit = 1;
        [Range(0, 3)] public float borderRadius = 1f;
        [Range(0, 3)] public float blurRadius = 1f;
        [Range(-2, 2)] public int blurPower = 0;
        [Range(-2, 2)] public int meshResolutionPower = 0;
        [Range(1, 5)] public float islandHeight;

        public bool resize = true;
        public bool normalizeColor = true;
        public bool addBorders = true;
        public bool blur = true;

        [Group(Constants.Dev)] public float updateDelay = 2;
        [Group(Constants.Dev), Range(0, 0.5f)] public float colorTolerance = 0.15f;
    }
}
#endif
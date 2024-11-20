using Unity.Burst;
using Unity.Mathematics;

namespace Flexus.ParticleMapEditor.Editor
{
    [BurstCompile]
    public struct ParticleData
    {
        public float2 CurrentPosition;
        public float Radius;
        public bool IsLocked;
    }
    
    [BurstCompile]
    public struct LevelObjectData
    {
        public float2 Position;
        public float Radius;
    }
}
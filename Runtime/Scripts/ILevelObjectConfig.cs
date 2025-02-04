using UnityEngine;

namespace Flexus.ParticleMapEditor
{
    public interface ILevelObjectConfig : IParticleType
    {
        string Name { get; }
        float Radius { get; }
        Transform Prefab { get; }
    }
}
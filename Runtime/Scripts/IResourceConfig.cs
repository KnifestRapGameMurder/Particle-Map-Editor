using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Flexus.ParticleMapEditor
{
    public interface IResourceConfig
    {
        string Name { get; }
        float ColliderRadius { get; }
        float MeshScale { get; }
        List<Mesh> Meshes { get; }
        List<Material> Materials { get; }

#if UNITY_EDITOR
        [HideInInspector]
        public UnityEvent Validated { get; }
#endif
    }
}

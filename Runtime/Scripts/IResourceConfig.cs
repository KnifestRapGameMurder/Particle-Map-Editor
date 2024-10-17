using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Flexus.ParticleMapEditor
{
    public interface IResourceConfig
    {
        string Name { get; set; }
        float ColliderRadius { get; set; }
        float MeshScale { get; set; }
        Mesh Mesh { get; set; }
        List<Material> Materials { get; set; }
    }
}

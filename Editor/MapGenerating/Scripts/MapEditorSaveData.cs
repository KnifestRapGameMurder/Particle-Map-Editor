using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    [Serializable]
    public class MapEditorSaveData
    {
        public List<EditorParticleArgs> Particles;
        public Dictionary<Vector2Int, Color> Texture;
        public float AreaSize;
        public Vector2 Density;
        public float Damp;
    }

    [Serializable]
    public struct EditorParticleArgs
    {
        public Vector2 Position;
    }
}
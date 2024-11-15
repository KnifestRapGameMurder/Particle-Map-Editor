using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using NUnit.Framework;
using TriInspector;

namespace Flexus.ParticleMapEditor.Editor
{
    public class MapEditorConfig : ScriptableObject
    {
        [HideInInspector]
        public List<ParticleArgs> Particles;
        [HideInInspector]
        public List<TexturePixelArgs> Texture;
        public float AreaSize;
        [MinMaxSlider(0, 1)]
        public Vector2 Density;
        public float Damp;
        public ParticleTypes TypesConfig;
        public List<ParticleSettings.ResLockArgs> ResLock;
        public List<LevelObjectArgs> levelObjects;

        public void SetTexture(Dictionary<Vector2Int, Color> texture)
        {
            Texture = texture.Select(_ => new TexturePixelArgs { Position = _.Key, Color = _.Value }).ToList();
        }

        public Dictionary<Vector2Int, Color> GetTexture()
        {
            return Texture.ToDictionary(_ => _.Position, _ => _.Color);
        }

        [Serializable]
        public struct ParticleArgs
        {
            public Vector2 Position;
        }
        
        [Serializable]
        public struct LevelObjectArgs
        {
            public string name;
            public Vector2 position;
        }

        [Serializable]
        public struct TexturePixelArgs
        {
            public Vector2Int Position;
            public Color Color;
        }
    }

}
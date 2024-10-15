using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TriInspector;
using System;
using JetBrains.Annotations;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareBoxGroup(Constants.ParticleControls)]
    [DeclareBoxGroup(Constants.VisualizationControls)]
    [DeclareBoxGroup(Constants.PaintingControls)]
    [DeclareFoldoutGroup(Constants.Dev)]
    [CreateAssetMenu(fileName = Constants.Settings, menuName = Constants.MapGenerating + "/" + Constants.Settings)]
    public class ParticleSettings : ScriptableObject
    {
        [Group(Constants.ParticleControls)] public float AreaSize = 50;
        [Group(Constants.ParticleControls)][SerializeField] private ParticleTypes _types;
        [Group(Constants.ParticleControls)][MinMaxSlider(0, 1)] public Vector2 Density = new Vector2(0.85f, 0.9f);
        [Group(Constants.ParticleControls)][Range(0, 1)] public float Damp = 0.5f;
        [Group(Constants.ParticleControls)] public bool Update = true;

        [Group(Constants.VisualizationControls)] public bool DrawResourses;
        [Group(Constants.VisualizationControls)][Range(0.1f, 2f)] public float CameraDistance;
        [Group(Constants.VisualizationControls)][Range(0, 45)] public float CameraRotationSpeed;

        [Group(Constants.PaintingControls)]
        [Range(0f, 0.5f)]
        public float BrushSize = 0.1f;

        [Group(Constants.PaintingControls)]
        [SerializeField, Dropdown("_typeNames")]
        private string _type;

        [Group(Constants.Dev)] 
        public int InitialCount;
        [Group(Constants.Dev)] 
        public int SubSteps = 1;
        [Group(Constants.Dev)] 
        public int SpawnPerFrame = 10;
        [Group(Constants.Dev)] 
        public Texture2D ParticleColors;
        [Group(Constants.Dev)] 
        public Mesh Mesh;
        [Group(Constants.Dev)] 
        public float CellSize = 5f;
        [Group(Constants.Dev)] 
        public float RepaintTimeStep;
        //[Group(Constants.Dev)] 
        //public Color VoidColor;
        //[Group(Constants.Dev)] 
        //public float VoidParticleRadius;
        //[Group(Constants.Dev)] 
        //public Color EmptyGroundColor;
        //[Group(Constants.Dev)] 
        //public float EmptyGroundParticleRadius;
        [Group(Constants.Dev)] 
        public float ColorCompareTolerance;
        [Group(Constants.Dev)] 
        public Material VoidMaterial;
        [Group(Constants.Dev)] 
        public Material ResMaterialSource;
        [Group(Constants.Dev)]
        [SerializeField]
        private List<NonResourceParticleArgs> _nonResourceParticles = new()
        {
            new NonResourceParticleArgs("_Void", Color.black, 3f),
            new NonResourceParticleArgs("_Border", Color.grey, 0.5f),
            new NonResourceParticleArgs("_Ground", Color.green, 0.5f),
        };

        [Serializable]
        public struct NonResourceParticleArgs
        {
            public string Name;
            public Color Color;
            public float Radius;

            public NonResourceParticleArgs(string name, Color color, float radius)
            {
                Name = name;
                Color = color;
                Radius = radius;
            }
        }

        [UsedImplicitly]
        private List<string> _typeNames => _nonResourceParticles.Select(_ => _.Name).Concat(Types.Select(_ => _.Name)).ToList();

        public float AreaCoeffMin => Density.x;
        public float AreaCoeffMax => Density.y;
        public List<ParticleType> Types => _types.Types;
        public List<NonResourceParticleArgs> NonResourceParticles => _nonResourceParticles;

        public Color BrushColor
        {
            get
            {
                if (_nonResourceParticles.Any(_ => _.Name.Equals(_type)))
                    return _nonResourceParticles.First(_ => _.Name.Equals(_type)).Color;

                return Types.First(_ => _.Name.Equals(_type)).Color;
            }
        }
    }
}
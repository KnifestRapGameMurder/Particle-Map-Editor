#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TriInspector;
using System;
using UnityEditor;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareBoxGroup(Constants.ParticleControls)]
    [DeclareBoxGroup(Constants.VisualizationControls)]
    [DeclareBoxGroup(Constants.PaintingControls)]
    [DeclareFoldoutGroup(Constants.Dev)]
    [CreateAssetMenu(fileName = nameof(ParticleSettings), menuName = Constants.NameSpace + nameof(ParticleSettings))]
    public class ParticleSettings : ScriptableObject
    {
        [Group(Constants.ParticleControls), GUIColor("$" + nameof(_particleControlsGroupColor)), InlineEditor]
        [SerializeField]
        private ParticleTypes _types;

        [Group(Constants.ParticleControls), GUIColor("$" + nameof(_particleControlsGroupColor)), Min(1)]
        public float AreaSize = 50;

        [Group(Constants.ParticleControls), GUIColor("$" + nameof(_particleControlsGroupColor)), MinMaxSlider(0, 1)]
        public Vector2 Density = new Vector2(0.85f, 0.9f);

        [Group(Constants.ParticleControls), Range(0, 1), GUIColor("$" + nameof(_particleControlsGroupColor))]
        public float Damp = 0.5f;

        [Group(Constants.ParticleControls), GUIColor("$" + nameof(_particleControlsGroupColor))]
        public bool Update = true;

        [Group(Constants.ParticleControls),
         GUIColor("$" + nameof(_particleControlsGroupColor)),
         TableList(Draggable = false, HideAddButton = true, HideRemoveButton = true, AlwaysExpanded = true),]
        public List<ResLockArgs> ResLock;

        #region ResLock

        [Serializable]
        public class ResLockArgs
        {
            [HideInInspector] public ParticleType Type;
            [GUIColor("$" + nameof(ResColor))] public bool IsLocked;

            [ShowInInspector, GUIColor("$" + nameof(ResColor))]
            private string ResName => HasType ? Type.Name : "NO RES CONFIG";

            private Color ResColor => HasType ? Type.Color : default;
            private bool HasType => Type != null && Type.ResourceConfig != null;

            public override string ToString()
            {
                return $"{nameof(ResLockArgs)} {{ Type = {Type.Name}/{Type.Id}, IsLocked = {IsLocked} }}";
            }
        }

        private void OnValidate()
        {
            UpdateResLock();
            EditorUtility.SetDirty(this);
        }

        [Group(Constants.Dev)]
        [Button]
        public void UpdateResLock()
        {
            ResLock = _types.Types.OrderBy(_ => _.Name).Select(_ => new ResLockArgs
            {
                Type = _,
                IsLocked = ResLock != null && ResLock.Any(current => current.Type.Id == _.Id && current.IsLocked),
            }).ToList();
        }

        public bool IsResLocked(ParticleType type) => ResLock.Any(_ => _.Type.Id == type.Id && _.IsLocked);

        #endregion

        [Group(Constants.ParticleControls), GUIColor("$" + nameof(_particleControlsGroupColor)), InlineEditor]
        [SerializeField]
        public LevelObjectsConfig levelObjects;
        
        [Group(Constants.VisualizationControls), GUIColor("$" + nameof(_visualizationControlsGroupColor))]
        public bool drawResources = true;

        [Group(Constants.PaintingControls), Range(0f, 0.5f), GUIColor("$" + nameof(InspectorBrushColor))]
        public float brushSize = 0.1f;

        [Group(Constants.PaintingControls), GUIColor("$" + nameof(InspectorBrushColor)), Dropdown(nameof(TypeNames))]
        [SerializeField]
        private string typeName;
        
        #region Dev

        [Group(Constants.Dev)] public int InitialCount;
        [Group(Constants.Dev)] public int SubSteps = 1;
        [Group(Constants.Dev)] public float screenPointRayMaxDistance = 1000f;
        [Group(Constants.Dev)] public int SpawnPerFrame = 10;
        [Group(Constants.Dev)] public Texture2D ParticleColors;
        [Group(Constants.Dev)] public Mesh Mesh;
        [Group(Constants.Dev)] public float CellSize = 5f;
        [Group(Constants.Dev)] public float RepaintTimeStep;
        [Group(Constants.Dev)] public float ColorCompareTolerance;
        [Group(Constants.Dev)] public Material VoidMaterial;
        [Group(Constants.Dev)] public Material ResMaterialSource;
        [Group(Constants.Dev)] public KeyCodes keyCodes;

        [Serializable]
        public struct KeyCodes
        {
            public KeyCode brush ;
            public KeyCode bucket;
            public KeyCode crop;
            public KeyCode painting;
            public KeyCode addParticle;
        }
        
        [Group(Constants.Dev)] [SerializeField]
        private List<NonResourceParticleArgs> _nonResourceParticles = new()
        {
            new NonResourceParticleArgs("_Void", Color.black, 3f),
            new NonResourceParticleArgs("_Border", Color.grey, 0.5f),
            new NonResourceParticleArgs("_Ground", Color.green, 0.5f),
        };

        [Group(Constants.Dev)] [SerializeField]
        private Color _particleControlsGroupColor;

        [Group(Constants.Dev)] [SerializeField]
        private Color _visualizationControlsGroupColor;

        [Group(Constants.Dev)] [SerializeField]
        private Color _paintingControlsGroupColor;

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

        #endregion

        private List<string> TypeNames =>
            _nonResourceParticles.Select(p => p.Name).Concat(Types.Select(p => p.Name)).ToList();

        public float MinDensity => Density.x;
        public float MaxDensity => Density.y;

        public ParticleTypes TypesConfig
        {
            get => _types;
            set => _types = value;
        }

        public List<ParticleType> Types => TypesConfig.Types;
        public List<NonResourceParticleArgs> NonResourceParticles => _nonResourceParticles;

        public Color BrushColor =>
            _nonResourceParticles.Any(particle => particle.Name.Equals(typeName))
                ? _nonResourceParticles.First(particle => particle.Name.Equals(typeName)).Color
                : Types.First(type => type.Name.Equals(this.typeName)).Color;

        private Color InspectorBrushColor
        {
            get
            {
                var color = BrushColor;
                return color.grayscale < 0.3f ? Color.white : color;
            }
        }
    }
}
#endif
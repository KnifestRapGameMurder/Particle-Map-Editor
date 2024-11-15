using System.Collections.Generic;
using UnityEngine;
using TriInspector;
using System;

namespace Flexus.ParticleMapEditor.Editor
{
    public interface IParticleType
    {
        string Id { get; }
    }
    
    [DeclareHorizontalGroup("ResourceConfig", Sizes = new[] { 200f, 50f })]
    [DeclareFoldoutGroup("Particle")]
    [DeclareBoxGroup("Particle/Random", Title = "Randomization")]
    [DeclareFoldoutGroup("Resource")]
    [System.Serializable]
    public class ParticleType:IParticleType
    {
        [HideInInspector]
        [SerializeField] private string _guid;

        public string Id => _guid;

        public ParticleType()
        {
            //Debug.LogWarning("ParticleType");
            UpdateId();
        }

        public void CheckAndFixId()
        {
            //Debug.Log($"ParticleType.CheckAndFixId: {_guid}");

            if(string.IsNullOrEmpty(_guid))
                UpdateId();

            //Debug.Log($"AfterCheck: {_guid}");
        }

        public void UpdateId()
        {
            _guid = Guid.NewGuid().ToString();
            //Debug.LogWarning($"{nameof(ParticleType)}.{nameof(SetNewId)}: Id = {Id}");
        }

        private IEnumerable<TriDropdownItem<ScriptableObject>> ResourceConfigAssets 
            => EditorUtils.GetAllResourceConfigAssets();

        [Group("ResourceConfig"), HideLabel, Dropdown(nameof(ResourceConfigAssets))]
        [SerializeField] private ScriptableObject _resourceConfigAsset;

        [Group("ResourceConfig")]
        [Button("Open"), HideIf(nameof(_resourceConfigAsset), null)]
        private void OpenResConfig()
        {
            EditorUtils.OpenScriptableObjectInInspector(_resourceConfigAsset);
        }

        [Group("Particle")]
        public Color Color = Color.black;
        [Group("Particle"), Range(0, 1)]
        public float Offset;
        [Group("Particle/Random"), MinMaxSlider(0.5f, 1.5f)]
        public Vector2 RadiusRandomization = new Vector2(0.9f, 1.1f);
        [Group("Particle/Random")]
        public bool ImpactsOnResource = true;
        [Group("Resource"), Range(0, 1)]
        public float RandomPosition;
        [Group("Resource"), Range(0, 1)]
        public float RandomRotation;
        [Group("Resource"), Range(0, 1)]
        public float RandomScale;
        [Group("Resource"), MinMaxSlider(0.5f, 1.5f)]
        public Vector2 RandomScaleRange = new Vector2(0.9f, 1.1f);

        public IResourceConfig ResourceConfig => _resourceConfigAsset as IResourceConfig;
        public string Name => ResourceConfig.Name;
        public float Radius => ResourceConfig.ColliderRadius;
        public Mesh ResMesh => ResourceConfig.Mesh;
        public List<Material> ResMaterial => ResourceConfig.Materials;
        //public float ResScale => _resourseConfig.MeshScale;
        public float ResScale
        {
            get
            {
                if (ResourceConfig == null)
                {
                    Debug.Log(Color);
                    return 1f;
                }

                return ResourceConfig.MeshScale;
            }
        }
    }
}
﻿using System.Collections.Generic;
using UnityEngine;
using TriInspector;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareHorizontalGroup("ResourceConfig", Sizes = new[] { 200f, 50f })]
    [DeclareFoldoutGroup("Particle")]
    [DeclareBoxGroup("Particle/Random", Title = "Randomization")]
    [DeclareFoldoutGroup("Resource")]
    [System.Serializable]
    public class ParticleType
    {
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

        private TriValidationResult ValidateTexture()
        {
            if (_resourceConfigAsset == null) return TriValidationResult.Error("Tex is null");
            return TriValidationResult.Valid;
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

        public ParticleType()
        {
            //Debug.LogWarning("ParticleType");
        }
    }
}
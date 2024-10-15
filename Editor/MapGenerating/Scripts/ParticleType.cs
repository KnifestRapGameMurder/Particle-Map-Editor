using System.Collections.Generic;
using UnityEngine;
using TriInspector;
using UnityEditor;
using System.Linq;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareFoldoutGroup("Particle")]
    [DeclareBoxGroup("Particle/Random", Title = "Randomization")]
    [DeclareFoldoutGroup("Resource")]
    [System.Serializable]
    public class ParticleType
    {
        private IEnumerable<TriDropdownItem<ScriptableObject>> FindAllResoourceConfigs()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}", null);
            var paths = guids.Select(_ => AssetDatabase.GUIDToAssetPath(_));
            var assets = paths.Select(_ => AssetDatabase.LoadAssetAtPath<ScriptableObject>(_)).Where(_=>_ is IResourceConfig);
            
            if(assets == null || assets.Count() == 0)
            {
                return new TriDropdownList<ScriptableObject>
                {
                    {"NULL", null }
                };
            }
            
            return assets.Select(_ => new TriDropdownItem<ScriptableObject> { Text = _.name, Value = _ });
        }

        [HideLabel, Dropdown(nameof(FindAllResoourceConfigs))]
        [SerializeField] private ScriptableObject _resourseConfigAsset;

        private TriValidationResult ValidateTexture()
        {
            if (_resourseConfigAsset == null) return TriValidationResult.Error("Tex is null");
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

        public IResourceConfig ResourceConfig => _resourseConfigAsset as IResourceConfig;
        public string Name => ResourceConfig.Name;
        public float Radius => ResourceConfig.ColliderRadius;
        public Mesh ResMesh => ResourceConfig.Meshes[0];
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
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using TriInspector;
using UnityEditor;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareBoxGroup(SaveLoad, HideTitle = true)]
    [DeclareHorizontalGroup(SaveLoadResourceConfig, Sizes = new[] { 200f, 50f })]
    [DeclareFoldoutGroup(SaveLoadSettings, Title = "Settings")]
    [DeclareHorizontalGroup(SaveLoadButtons)]
    [DeclareFoldoutGroup(Constants.Dev)]
    public class ResourceTypeEditHandler : MonoBehaviour
    {
        private const string SaveLoad = "SaveLoad";
        private const string SaveLoadResourceConfig = "SaveLoad/ResourceConfig";
        private const string SaveLoadSettings = "SaveLoad/Settings";
        private const string SaveLoadButtons = "SaveLoad/Buttons";

        [HideInInspector]
        public string ResourceConfigsPath;

        private IEnumerable<TriDropdownItem<ScriptableObject>> ResourceConfigAssets
            => EditorUtils.GetAllResourceConfigAssets();

        [Group(SaveLoadResourceConfig), HideLabel, Dropdown(nameof(ResourceConfigAssets)), PropertySpace(SpaceAfter = 10)]
        [SerializeField] private ScriptableObject _resourceConfigAsset;

        [Group(SaveLoadResourceConfig), PropertySpace(SpaceAfter = 10)]
        [Button("Open"), HideIf(nameof(_resourceConfigAsset), null)]
        private void OpenResConfig()
        {
            EditorUtils.OpenScriptableObjectInInspector(_resourceConfigAsset);
        }

        [Group(SaveLoadSettings)]
        [SerializeField] private bool _useName = true;
        [Group(SaveLoadSettings)]
        [SerializeField] private bool _useColliderRadius = true;
        [Group(SaveLoadSettings)]
        [SerializeField] private bool _useMeshScale = true;
        [Group(SaveLoadSettings)]
        [SerializeField] private bool _useMesh = true;
        [Group(SaveLoadSettings)]
        [SerializeField] private bool _useMaterials = true;
        [Group(Constants.Dev)]
        [SerializeField] private MeshFilter _meshFilter;
        [Group(Constants.Dev)]
        [SerializeField] private MeshRenderer _meshRenderer;
        [Group(Constants.Dev)]
        [SerializeField] private SphereCollider _sphereCollider;
        [Group(Constants.Dev)]
        [SerializeField] private Transform _meshTransform;

        private IResourceConfig _resourceConfig => _resourceConfigAsset as IResourceConfig;
        private bool HasResourceConfig => _resourceConfig != null;

        [Group(SaveLoadButtons)]
        [Button]
        private void Save()
        {
            if (!HasResourceConfig)
                return;

            if(_useName) _resourceConfig.Name = name;
            if(_useColliderRadius) _resourceConfig.ColliderRadius = _sphereCollider.radius;
            if (_useMeshScale) _resourceConfig.MeshScale = _meshTransform.localScale.x;
            if (_useMesh) _resourceConfig.Mesh = _meshFilter.sharedMesh;
            if (_useMaterials) _resourceConfig.Materials = _meshRenderer.sharedMaterials.ToList();

            EditorUtility.SetDirty(_resourceConfigAsset);
        }

        [Group(SaveLoadButtons)]
        [Button]
        private void Load()
        {
            if (!HasResourceConfig)
                return;

            if (_useName) name = _resourceConfig.Name;
            if (_useColliderRadius) _sphereCollider.radius = _resourceConfig.ColliderRadius;
            if (_useMeshScale) _meshTransform.localScale = _resourceConfig.MeshScale * Vector3.one;
            if (_useMesh) _meshFilter.sharedMesh = _resourceConfig.Mesh;
            if (_useMaterials) _meshRenderer.sharedMaterials = _resourceConfig.Materials.ToArray();

            EditorUtility.SetDirty(gameObject);        
            EditorUtility.SetDirty(_sphereCollider);        
            EditorUtility.SetDirty(_meshTransform);        
            EditorUtility.SetDirty(_meshFilter);        
            EditorUtility.SetDirty(_meshRenderer);        
        }

        //public void SetResourceConfig(IResourceConfig resourceConfig)
        //{
        //    _resourceConfigAsset = resourceConfig as ScriptableObject;
        //}

        //[HideIf(nameof(HasResourceConfig))]
        //[Button]
        //private void CreateNewType()
        //{
        //    _resourceConfig = ScriptableObject.CreateInstance<IResourceConfig>();
        //    _resourceConfig.Meshes.Add(_meshFilter.sharedMesh);
        //    _meshRenderer.sharedMaterials.ForEach(_ => _resourceConfig.Materials.Add(_));
        //    DataSaveLoadTools.SaveAsset(ResourceConfigsPath, name, _resourceConfig);
        //    EditorUtility.SetDirty(this);
        //}
    }
}

#endif
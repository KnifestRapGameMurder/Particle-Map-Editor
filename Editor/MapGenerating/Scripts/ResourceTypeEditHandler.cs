using OdinSerializer.Utilities;
using System.Collections;
using System.Collections.Generic;
using TriInspector;
using UnityEditor;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareFoldoutGroup(Constants.Dev)]
    public class ResourceTypeEditHandler : MonoBehaviour
    {
        [HideInInspector]
        public string ResourceConfigsPath;

        [InlineEditor, Required]
        [SerializeField] private IResourceConfig _resourceConfig;
        [Group(Constants.Dev)]
        [SerializeField] private MeshFilter _meshFilter;
        [Group(Constants.Dev)]
        [SerializeField] private MeshRenderer _meshRenderer;
        [Group(Constants.Dev)]
        [SerializeField] private SphereCollider _sphereCollider;
        [Group(Constants.Dev)]
        [SerializeField] private Transform _meshTransform;

        private bool HasResourceConfig => _resourceConfig != null;

        private void OnValidate()
        {
            if (_resourceConfig != null)
            {
                _resourceConfig.Validated.AddListener(UpdateHandler);
                UpdateHandler();
            }
            //EditorUtility.SetDirty(_resourceConfig);
        }

        //[Button("Update")]
        public void UpdateHandler()
        {
            if (_resourceConfig == null)
                return;

            if(_resourceConfig.Meshes != null && _resourceConfig.Meshes.Count > 0)
            {
                if(_meshFilter.sharedMesh != _resourceConfig.Meshes[0])
                    _meshFilter.sharedMesh = _resourceConfig.Meshes[0];

                _meshRenderer.sharedMaterials = _resourceConfig.Materials.ToArray();
            }

            _sphereCollider.radius = _resourceConfig.ColliderRadius;
            _meshTransform.localScale = _resourceConfig.MeshScale * Vector3.one;
            name = _resourceConfig.Name;
        }

        public void SetResourceConfig(IResourceConfig resourceConfig)
        {
            _resourceConfig = resourceConfig;
            _resourceConfig.Validated.AddListener(UpdateHandler);
        }

        [HideIf(nameof(HasResourceConfig))]
        [Button]
        private void CreateNewType()
        {
            //_resourceConfig = ScriptableObject.CreateInstance<IResourceConfig>();
            //_resourceConfig.Meshes.Add(_meshFilter.sharedMesh);
            //_meshRenderer.sharedMaterials.ForEach(_ => _resourceConfig.Materials.Add(_));
            //DataSaveLoadTools.SaveAsset(ResourceConfigsPath, name, _resourceConfig);
            //EditorUtility.SetDirty(this);
        }
    }
}

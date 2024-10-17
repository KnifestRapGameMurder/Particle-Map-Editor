using UnityEditor;
using UnityEngine;
using TriInspector;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareFoldoutGroup(Constants.Dev)]
    [DeclareBoxGroup("Create")]
    public class ResourceEditor : MonoBehaviour
    {
        [Group(Constants.Dev)]
        [SerializeField] private string _resourceConfigsPath;
        [Group(Constants.Dev)]
        [SerializeField] private ResourceTypeEditHandler _editHandlerPrefab;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        [Space, ValidateInput(nameof(ValidateTexture)), Group("Create")]
        [ShowInInspector] private string _newResourceTypeName;
        [Group("Create")]
        [ShowInInspector] private Mesh _mesh;
        [Group("Create")]
        [ShowInInspector] private Material _material;

        private bool HasNewTypeName => !string.IsNullOrEmpty(_newResourceTypeName);

        private TriValidationResult ValidateTexture()
        {
            if (string.IsNullOrEmpty(_newResourceTypeName))
                return TriValidationResult.Error("Name is empty");

            return TriValidationResult.Valid;
        }

        //[Group("Create"), TriInspector.EnableIf(nameof(HasNewTypeName))]
        //[Button]
        //private void CreateNewType()
        //{
        //    IResourceConfig resourceConfig = ScriptableObject.CreateInstance<IResourceConfig>();
        //    resourceConfig.Meshes.Add(_mesh);
        //    resourceConfig.Materials.Add(_material);
        //    DataSaveLoadTools.SaveAsset(_resourceConfigsPath, _newResourceTypeName, resourceConfig);
        //    ResourceTypeEditHandler editHandler = PrefabUtility.InstantiatePrefab(_editHandlerPrefab, transform) as ResourceTypeEditHandler;
        //    EditorUtility.SetDirty(editHandler);
        //    editHandler.SetResourceConfig(resourceConfig);
        //    editHandler.ResourceConfigsPath = _resourceConfigsPath;
        //    editHandler.UpdateHandler();
        //    EditorUtility.SetDirty(editHandler);
        //    Selection.activeObject = editHandler;
        //}
    }
}
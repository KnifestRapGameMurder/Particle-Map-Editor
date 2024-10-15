using UnityEngine;
using TriInspector;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareFoldoutGroup(Constants.Dev)]
    [DeclareBoxGroup("Editor Documents")]
    [DeclareBoxGroup("Map")]
    public class MapEditorSaveManager : MonoBehaviour
    {
        public const string EditorFileExtension = "phme";
        public const string GameFileExtension = "phm";

        [Group(Constants.Dev)]
        [SerializeField] private VerletParticleGenerator _generator;
        [Group(Constants.Dev)]
        [SerializeField] private ResoursePainter _painter;
        [Group(Constants.Dev)]
        [SerializeField] private string _mapEditorDocumentsPath;
        [Group(Constants.Dev)]
        [SerializeField] private string _mapConfigsPath;

        private string _loadedDataPath;

        private string DefaultEditorFilePath => $"{Application.dataPath}/{_mapEditorDocumentsPath}";

        [Group("Editor Documents")]
        [Button]
        private void Save()
        {
            DataSaveLoadTools.SaveDataToFile(BuildSaveData, EditorFileExtension, DefaultEditorFilePath);
        }

        [Group("Editor Documents")]
        [Button]
        private void Load()
        {
            if(DataSaveLoadTools.TryLoadDataFromFile(EditorFileExtension, out MapEditorSaveData data, DefaultEditorFilePath))
                ApplyEditorData(data);
        }

        private enum ExportType
        {
            NewFile,
            Existing
        }

        //[Group("Map")]
        //[ShowInInspector] private ExportType _exportType;
        //[Group("Map"), ShowIf(nameof(_exportType), ExportType.NewFile), ValidateInput(nameof(ValidateName))]
        //[ShowInInspector] private string _exportFileName;
        //[Group("Map"), ShowIf(nameof(_exportType), ExportType.Existing), Required]
        private IMapConfig _mapConfig => _mapConfigAsset as IMapConfig;

        private IEnumerable<TriDropdownItem<ScriptableObject>> FindAllMapConfigs()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}", null);
            var paths = guids.Select(_ => AssetDatabase.GUIDToAssetPath(_));
            var assets = paths.Select(_ => AssetDatabase.LoadAssetAtPath<ScriptableObject>(_)).Where(_ => _ is IMapConfig);

            if (assets == null || assets.Count() == 0)
            {
                return new TriDropdownList<ScriptableObject>
                {
                    {"NULL", null }
                };
            }

            return assets.Select(_ => new TriDropdownItem<ScriptableObject> { Text = _.name, Value = _ });
        }

        [Group("Map"), Dropdown(nameof(FindAllMapConfigs))]
        [SerializeField] private ScriptableObject _mapConfigAsset;

        //private bool CanExport => (_exportType is ExportType.NewFile && !string.IsNullOrEmpty(_exportFileName))
        //    || (_exportType is ExportType.Existing && _mapConfig != null);
        private bool CanExport => _mapConfig != null;

        //private TriValidationResult ValidateName()
        //{
        //    if (string.IsNullOrEmpty(_exportFileName))
        //        return TriValidationResult.Error("Name is empty");

        //    return TriValidationResult.Valid;
        //}

        //private TriValidationResult ValidateExport()
        //{
        //    if (string.IsNullOrEmpty(_exportFileName))
        //        return TriValidationResult.Warning("Export NOT available in edit mode.");

        //    return TriValidationResult.Valid;
        //}

        private bool IsEditMode => _generator.Particles.Count == 0;

        public struct MapResourceData
        {
            public byte Resource;
            public Vector2 Position;
            public float Rotation;
            public float Scale;
            public float Height;
        }

        [Group("Map"), EnableIf(nameof(CanExport)), InfoBox("Export NOT available in edit mode", TriMessageType.Warning, nameof(IsEditMode))]
        [Button]
        private void Export()
        {
            if (_generator.Particles.Count == 0)
                return;

            List<MapResourceData> resources = new();
            List<IResourceConfig> usedResourceConfigs = new();

            foreach (Particle particle in _generator.Particles)
            {
                if (particle.Type == null)
                    continue;

                IResourceConfig particleResConfig = particle.Type.ResourceConfig;

                if (!usedResourceConfigs.Contains(particleResConfig))
                    usedResourceConfigs.Add(particleResConfig);

                MapResourceData resource = new()
                {
                    Resource = ((byte)usedResourceConfigs.IndexOf(particleResConfig)),
                    Position = particle.CurrentPosition + particle.RandomAmount * particle.ParticleRadius * particle.RandomDirection,
                    Rotation = particle.RandomRotationCoeff * particle.RandomRotation * 360f,
                    Scale = Mathf.Lerp(1f, particle.RandomScaleValue.Remap(0, 1, particle.RandomScaleRange.x, particle.RandomScaleRange.y), particle.RandomScaleCoeff),
                    Height = 1f,
                };

                resources.Add(resource);
            }

            List<IMapConfig.MapArgs.ResourceTypeArgs> resourceTypesArgs = usedResourceConfigs.Select(_ => new IMapConfig.MapArgs.ResourceTypeArgs()
            {
                Resource = _,
                Capacity = 1,
                Level = 1
            }).ToList();

            ExportArgs args = new ExportArgs
            {
                UsedResourceConfigs = resourceTypesArgs.ToArray(),
                ResourceDatas = resources.ToArray(),
            };

            //if (_exportType is ExportType.NewFile)
            //    ExportToNewFile(args);
            //else
                ExportToExistingMap(args);
        }

        private struct ExportArgs
        {
            public IMapConfig.MapArgs.ResourceTypeArgs[] UsedResourceConfigs;
            public MapResourceData[] ResourceDatas;
        }

        private void ExportToExistingMap(ExportArgs args)
        {
            SetArgsToMap(args, _mapConfig);
            EditorUtility.SetDirty(_mapConfigAsset);
        }

        //private void ExportToNewFile(ExportArgs args)
        //{
        //    MapConfig map = ScriptableObject.CreateInstance<MapConfig>();
        //    SetArgsToMap(args, map);
        //    DataSaveLoadTools.SaveAsset(_mapConfigsPath, _exportFileName, map);
        //}

        private static void SetArgsToMap(ExportArgs args, IMapConfig map)
        {
            map.SetMap(new IMapConfig.MapArgs
            {
                UsedResourceConfigs = args.UsedResourceConfigs,
                ResourceConfigIndexes = args.ResourceDatas.Select(_ => _.Resource).ToArray(),
                ResourcePositions = args.ResourceDatas.Select(_ => _.Position).ToArray(),
                ResourceRotations = args.ResourceDatas.Select(_ => _.Rotation).ToArray(),
                ResourceScales = args.ResourceDatas.Select(_ => _.Scale).ToArray(),
                ResourceHeights = args.ResourceDatas.Select(_ => _.Height).ToArray(),
            });
        }

        private MapEditorSaveData BuildSaveData()
        {
            MapEditorSaveData data = new MapEditorSaveData();
            data.AreaSize = _generator.Settings.AreaSize;
            data.Density = _generator.Settings.Density;
            data.Damp = _generator.Settings.Damp;
            data.Texture = _painter.GetTexureColors();
            data.Particles = _generator.Particles.Select(_ => new EditorParticleArgs() { Position = _.CurrentPosition }).ToList();

            return data;
        }

        private void ApplyEditorData(MapEditorSaveData data)
        {
            if (data.AreaSize != 0)
            {
                _generator.Settings.AreaSize = data.AreaSize;
                _generator.Settings.Density = data.Density;
                _generator.Settings.Damp = data.Damp;
            }

            if(data.Texture != null)
                _painter.SetTexturePixels(data.Texture);

            if (data.Particles != null)
            {
                _generator.Particles.Clear();
                data.Particles.ForEach(_ => _generator.AddParticle(_.Position));
                _generator.PaintParticles();
            }
        }
    }
}
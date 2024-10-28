using UnityEngine;
using TriInspector;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareFoldoutGroup(Constants.Dev)]
    [DeclareBoxGroup(nameof(MapEditorConfig))]
    [DeclareBoxGroup(nameof(MapEditorConfig) + "/Save", HideTitle = true)]
    [DeclareBoxGroup(nameof(MapEditorConfig) + "/Load", HideTitle = true)]
    [DeclareBoxGroup("Map")]
    public class MapEditorSaveManager : MonoBehaviour
    {
        [Group(Constants.Dev)]
        [SerializeField] private VerletParticleGenerator _generator;
        [Group(Constants.Dev)]
        [SerializeField] private ResoursePainter _painter;
        [Group(Constants.Dev)]
        [SerializeField] private string _mapEditorConfigsPath = "ParticleMapEditor/Editor/Maps";

        private string _loadedDataPath;

        [Group(nameof(MapEditorConfig) + "/Save")]
        [SerializeField] private string _saveName;

        [Group(nameof(MapEditorConfig) + "/Save"), DisableIf(nameof(IsEditMode))]
        [Button]
        private void Save()
        {
            EditorUtils.SaveAsset(_mapEditorConfigsPath, _saveName, BuildSaveData());
        }

        [Group(nameof(MapEditorConfig) + "/Load"), Dropdown(nameof(GetAllMapEditorConfigs))]
        [SerializeField] private MapEditorConfig _mapToLoad;

        private IEnumerable<TriDropdownItem<MapEditorConfig>> GetAllMapEditorConfigs()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(MapEditorConfig)}", null);
            var paths = guids.Select(_ => AssetDatabase.GUIDToAssetPath(_));
            var assets = paths.Select(_ => AssetDatabase.LoadAssetAtPath<MapEditorConfig>(_));

            if (assets == null || assets.Count() == 0)
            {
                return new TriDropdownList<MapEditorConfig>
                {
                    {"NO CONFIGS FOUND", null }
                };
            }

            return assets.Select(_ => new TriDropdownItem<MapEditorConfig> { Text = _.name, Value = _ });
        }

        [Group(nameof(MapEditorConfig) + "/Load"), DisableIf(nameof(IsEditMode))]
        [Button]
        private void Load()
        {
            if(_mapToLoad != null)
                ApplyEditorData(_mapToLoad);
        }

        private enum ExportType
        {
            NewFile,
            Existing
        }

        private IMapConfig _mapConfig => _mapConfigAsset as IMapConfig;

        private IEnumerable<TriDropdownItem<ScriptableObject>> MapConfigAssets
            => EditorUtils.GetAllMapConfigAssets();

        [Group("Map"), Dropdown(nameof(MapConfigAssets))]
        [SerializeField] private ScriptableObject _mapConfigAsset;

        private bool CanExport => _mapConfig != null && !IsEditMode;
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

        private MapEditorConfig BuildSaveData()
        {
            MapEditorConfig data = ScriptableObject.CreateInstance<MapEditorConfig>();
            ParticleSettings settings = _generator.Settings;
            data.AreaSize = settings.AreaSize;
            data.Density = settings.Density;
            data.Damp = settings.Damp;
            data.TypesConfig = settings.TypesConfig;
            data.ResLock = settings.ResLock;
            data.SetTexture(_painter.GetTexureColors());
            data.Particles = _generator.Particles.Select(_ => new MapEditorConfig.ParticleArgs() { Position = _.CurrentPosition }).ToList();

            return data;
        }

        private void ApplyEditorData(MapEditorConfig data)
        {
            ParticleSettings settings = _generator.Settings;

            if (data.AreaSize != 0)
                settings.AreaSize = data.AreaSize;

            if(data.Density != Vector2.zero)
                settings.Density = data.Density;

            if(data.Damp != 0) 
                settings.Damp = data.Damp;

            if(data.TypesConfig != null)
                settings.TypesConfig = data.TypesConfig;

            if(data.ResLock != null)
                foreach (var loaded in data.ResLock)
                    foreach (var current in settings.ResLock)
                        if (loaded.Type.Id == current.Type.Id)
                            current.IsLocked = loaded.IsLocked;

            if(data.Texture != null)
                _painter.SetTexturePixels(data.GetTexture());

            if (data.Particles != null)
            {
                _generator.Particles.Clear();
                data.Particles.ForEach(_ => _generator.AddParticle(_.Position));
                _generator.PaintParticles();
            }
        }
    }
}
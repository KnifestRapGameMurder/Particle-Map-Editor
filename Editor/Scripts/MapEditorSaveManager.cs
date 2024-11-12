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
        [Group(Constants.Dev)] [SerializeField]
        private VerletParticleGenerator _generator;

        [Group(Constants.Dev)] [SerializeField]
        private ResoursePainter _painter;

        [Group(Constants.Dev)] [SerializeField]
        private IslandMeshGenerator meshGenerator;
        
        [Group(Constants.Dev)] [SerializeField]
        private string _mapEditorConfigsPath = "ParticleMapEditor/Editor/Maps";

        private string _loadedDataPath;

        [Group(nameof(MapEditorConfig) + "/Save")] [SerializeField]
        private string _saveName;

        [Group(nameof(MapEditorConfig) + "/Save"), DisableIf(nameof(IsEditMode))]
        [Button]
        private void Save()
        {
            EditorUtils.SaveAsset(_mapEditorConfigsPath, _saveName, BuildSaveData());
        }

        [Group(nameof(MapEditorConfig) + "/Load"), Dropdown(nameof(GetAllMapEditorConfigs))] [SerializeField]
        private MapEditorConfig _mapToLoad;

        private IEnumerable<TriDropdownItem<MapEditorConfig>> GetAllMapEditorConfigs()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(MapEditorConfig)}", null);
            var paths = guids.Select(_ => AssetDatabase.GUIDToAssetPath(_));
            var assets = paths.Select(_ => AssetDatabase.LoadAssetAtPath<MapEditorConfig>(_));

            if (assets == null || assets.Count() == 0)
            {
                return new TriDropdownList<MapEditorConfig>
                {
                    { "NO CONFIGS FOUND", null }
                };
            }

            return assets.Select(_ => new TriDropdownItem<MapEditorConfig> { Text = _.name, Value = _ });
        }

        [Group(nameof(MapEditorConfig) + "/Load"), DisableIf(nameof(IsEditMode))]
        [Button]
        private void Load()
        {
            if (_mapToLoad != null)
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

        [Group("Map"), Dropdown(nameof(MapConfigAssets))] [SerializeField]
        private ScriptableObject _mapConfigAsset;

        private bool CanExport => _mapConfig != null && !IsEditMode;
        private bool IsEditMode => _generator.Particles.Count == 0;

        private struct MapResourceData
        {
            public byte Resource;
            public Vector2 Position;
            public float Rotation;
            public float Scale;
            public float Height;
        }

        [Group("Map"), EnableIf(nameof(CanExport)),
         InfoBox("Export NOT available in edit mode", TriMessageType.Warning, nameof(IsEditMode))]
        [Button]
        private void Export()
        {
            if (_generator.Particles.Count == 0)
                return;

            List<MapResourceData> resources = new();
            List<IResourceConfig> usedResourceConfigs = new();

            foreach (var particle in _generator.Particles)
            {
                if (particle.Type == null)
                    continue;

                var particleResConfig = particle.Type.ResourceConfig;

                if (!usedResourceConfigs.Contains(particleResConfig))
                    usedResourceConfigs.Add(particleResConfig);

                MapResourceData resource = new()
                {
                    Resource = ((byte)usedResourceConfigs.IndexOf(particleResConfig)),
                    Position = particle.CurrentPosition +
                               particle.RandomAmount * particle.ParticleRadius * particle.RandomDirection,
                    Rotation = particle.RandomRotationCoeff * particle.RandomRotation * 360f,
                    Scale = Mathf.Lerp(1f,
                        particle.RandomScaleValue.Remap(0, 1, particle.RandomScaleRange.x, particle.RandomScaleRange.y),
                        particle.RandomScaleCoeff),
                    Height = 1f,
                };

                resources.Add(resource);
            }

            var resourceTypesArgs = usedResourceConfigs.Select(resourceConfig =>
                new IMapConfig.MapArgs.ResourceTypeArgs()
                {
                    Resource = resourceConfig,
                    Capacity = 1,
                    Level = 1
                }).ToList();

            var args = new ExportArgs
            {
                UsedResourceConfigs = resourceTypesArgs.ToArray(),
                ResourceData = resources.ToArray(),
                IslandMesh = meshGenerator.IslandMesh,
            };

            ExportToExistingMap(args);
        }

        private struct ExportArgs
        {
            public IMapConfig.MapArgs.ResourceTypeArgs[] UsedResourceConfigs;
            public MapResourceData[] ResourceData;
            public Mesh IslandMesh;
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
                ResourceConfigIndexes = args.ResourceData.Select(r => r.Resource).ToArray(),
                ResourcePositions = args.ResourceData.Select(r => r.Position).ToArray(),
                ResourceRotations = args.ResourceData.Select(r => r.Rotation).ToArray(),
                ResourceScales = args.ResourceData.Select(r => r.Scale).ToArray(),
                ResourceHeights = args.ResourceData.Select(r => r.Height).ToArray(),
                IslandMesh = args.IslandMesh,
            });
        }

        private MapEditorConfig BuildSaveData()
        {
            var data = ScriptableObject.CreateInstance<MapEditorConfig>();
            var settings = _generator.Settings;
            data.AreaSize = settings.AreaSize;
            data.Density = settings.Density;
            data.Damp = settings.Damp;
            data.TypesConfig = settings.TypesConfig;
            data.ResLock = settings.ResLock;
            data.SetTexture(_painter.GetTexureColors());
            data.Particles = _generator.Particles
                .Select(p => new MapEditorConfig.ParticleArgs() { Position = p.CurrentPosition }).ToList();

            return data;
        }

        private void ApplyEditorData(MapEditorConfig data)
        {
            var settings = _generator.Settings;

            if (data.AreaSize != 0)
                settings.AreaSize = data.AreaSize;

            if (data.Density != Vector2.zero)
                settings.Density = data.Density;

            if (data.Damp != 0)
                settings.Damp = data.Damp;

            if (data.TypesConfig != null)
                settings.TypesConfig = data.TypesConfig;

            if (data.ResLock != null)
                foreach (var loaded in data.ResLock)
                foreach (var current in settings.ResLock.Where(current => loaded.Type.Id == current.Type.Id))
                    current.IsLocked = loaded.IsLocked;

            if (data.Texture != null)
                _painter.SetTexturePixels(data.GetTexture());

            if (data.Particles != null)
            {
                _generator.Particles.Clear();
                data.Particles.ForEach(p => _generator.AddParticle(p.Position));
                _generator.PaintParticles();
            }
        }
    }
}
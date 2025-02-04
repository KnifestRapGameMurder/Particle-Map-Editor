#if UNITY_EDITOR
using System.Collections;
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

        [Group(Constants.Dev)] [SerializeField]
        private MapEditorConfig autoSaveConfig;
        
        private string _loadedDataPath;

        [Group(nameof(MapEditorConfig) + "/Save")] [SerializeField]
        private string saveName;

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            ApplyEditorData(autoSaveConfig);
        }

        private void OnDestroy()
        {
            ApplyDataToConfig(autoSaveConfig);
            EditorUtility.SetDirty(autoSaveConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Group(nameof(MapEditorConfig) + "/Save"), DisableIf(nameof(IsEditMode))]
        [Button]
        // ReSharper disable once UnusedMember.Local
        private void Save()
        {
            EditorUtils.SaveAsset(_mapEditorConfigsPath, saveName, BuildSaveData());
        }

        [Group(nameof(MapEditorConfig) + "/Load"), Dropdown(nameof(GetAllMapEditorConfigs))] [SerializeField]
        private MapEditorConfig mapToLoad;

        private IEnumerable<TriDropdownItem<MapEditorConfig>> GetAllMapEditorConfigs()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(MapEditorConfig)} a:assets", null);
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath);
            var assets = paths.Select(AssetDatabase.LoadAssetAtPath<MapEditorConfig>);
            var mapEditorConfigs = assets as MapEditorConfig[] ?? assets.ToArray();
            
            if (!mapEditorConfigs.Any())
            {
                return new TriDropdownList<MapEditorConfig>
                {
                    { "NO CONFIGS FOUND", null }
                };
            }

            return mapEditorConfigs.Select(map => new TriDropdownItem<MapEditorConfig> { Text = map.name, Value = map });
        }

        [Group(nameof(MapEditorConfig) + "/Load"), DisableIf(nameof(IsEditMode))]
        [Button]
        // ReSharper disable once UnusedMember.Local
        private void Load()
        {
            if (mapToLoad != null)
                ApplyEditorData(mapToLoad);
        }

        private IMapConfig MapConfig => _mapConfigAsset as IMapConfig;

        private IEnumerable<TriDropdownItem<ScriptableObject>> MapConfigAssets
            => EditorUtils.GetAllMapConfigAssets();

        [Group("Map"), Dropdown(nameof(MapConfigAssets))] [SerializeField]
        private ScriptableObject _mapConfigAsset;

        private bool CanExport => MapConfig != null && !IsEditMode;
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
                LevelObjects = _generator.LevelObjects.Select(lo => new IMapConfig.MapArgs.LevelObjectArgs
                    { LevelObjectConfig = lo.config, Position = lo.CurrentPosition }).ToArray(),
            };

            ExportToExistingMap(args);
        }

        private struct ExportArgs
        {
            public IMapConfig.MapArgs.ResourceTypeArgs[] UsedResourceConfigs;
            public MapResourceData[] ResourceData;
            public Mesh IslandMesh;
            public IMapConfig.MapArgs.LevelObjectArgs[] LevelObjects;
        }

        private void ExportToExistingMap(ExportArgs args)
        {
            SetArgsToMap(args, MapConfig);
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
                LevelObjects = args.LevelObjects,
            });
        }

        private MapEditorConfig BuildSaveData()
        {
            var config = ScriptableObject.CreateInstance<MapEditorConfig>();
            ApplyDataToConfig(config);
            return config;
        }

        private void ApplyDataToConfig(MapEditorConfig config)
        {
            var settings = _generator.Settings;
            config.AreaSize = settings.AreaSize;
            config.Density = settings.Density;
            config.Damp = settings.Damp;
            config.TypesConfig = settings.TypesConfig;
            config.ResLock = settings.ResLock;
            config.SetTexture(_painter.GetTexureColors());
            config.Particles = _generator.Particles
                .Select(p => new MapEditorConfig.ParticleArgs() { Position = p.CurrentPosition }).ToList();
            config.levelObjects = _generator.LevelObjects.Select(lo => new MapEditorConfig.LevelObjectArgs()
                { position = lo.CurrentPosition, config = lo.config }).ToList();

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

            if (data.levelObjects != null)
                foreach (var levelObject in _generator.LevelObjects)
                foreach (var loadedLevelObject in data.levelObjects.Where(loadedLevelObject =>
                             levelObject.config.Id == loadedLevelObject.config.Id))
                {
                    levelObject.CurrentPosition = loadedLevelObject.position;
                    break;
                }
        }
    }
}
#endif
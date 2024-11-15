using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using NUnit.Framework;
using TriInspector;

namespace Flexus.ParticleMapEditor.Editor
{
    [DeclareBoxGroup("Add-Remove")]
    [DeclareFoldoutGroup(Constants.Dev)]
    public class VerletParticleGenerator : MonoBehaviour
    {
        [InlineEditor] [SerializeField] private ParticleSettings _settings;

        [Group(Constants.Dev)] [SerializeField]
        private Transform _area;

        [Group(Constants.Dev)] [SerializeField]
        private Transform _rotated;

        [Group(Constants.Dev)] [SerializeField]
        private Transform _cameraControl;

        [Group(Constants.Dev)] [SerializeField]
        private ParticleGeneratorUI _ui;

        private float _lastPaintTime;

        private readonly Dictionary<Vector2Int, List<Particle>> _grid = new();
        private readonly Dictionary<float, float> _radiusToSquare = new();
        private readonly Dictionary<ParticleType, Material> _typeMaterials = new();
        private readonly Dictionary<string, bool> _isResLocked = new();
        private readonly List<LevelObject> _levelObjects = new();
        
        public List<Particle> Particles { get; } = new();

        public ParticleSettings Settings => _settings;
        private float AreaCoeffMin => _settings.AreaCoeffMin;
        private float AreaCoeffMax => _settings.AreaCoeffMax;
        private int Count => _settings.InitialCount;
        private float Damp => _settings.Damp;
        private bool IsUpdating => _settings.Update;
        private float AreaSize => _settings.AreaSize;
        private List<ParticleType> Types => _settings.Types;
        private int SubSteps => _settings.SubSteps;
        private int SpawnPerFrame => _settings.SpawnPerFrame;
        private Texture2D ParticleColors => _settings.ParticleColors;
        private Mesh Mesh => _settings.Mesh;
        private float CellSize => _settings.CellSize;
        private float RepaintTimeStep => _settings.RepaintTimeStep;
        private bool DrawResources => _settings.DrawResourses;

        private IEnumerator Start()
        {
            Settings.UpdateResLock();

            foreach (var type in Types)
            {
                _radiusToSquare[type.Radius] = Mathf.PI * type.Radius * type.Radius;
                _typeMaterials[type] = new Material(_settings.ResMaterialSource) { color = type.Color };
            }

            foreach (var config in _settings.levelObjects.levelObjects)
            {
                var levelObject = new LevelObject()
                {
                    config = config,
                    instance = Instantiate(config.prefab, transform),
                };
                _levelObjects.Add(levelObject);
                _isResLocked[config.Id] = true;
                yield return null;
            }

            for (var i = 0; i < Count;)
            {
                for (var j = 0; j < SpawnPerFrame && i < Count; j++, i++) AddParticle();
                yield return null;
            }
        }

        private void Update()
        {
            if (IsUpdating)
            {
                if (Time.time > _lastPaintTime + RepaintTimeStep)
                {
                    PaintParticles();
                    _lastPaintTime = Time.time;
                }

                var subDeltaTime = Time.deltaTime / SubSteps;

                for (var i = 0; i < SubSteps; i++)
                {
                    KeepParticlesInsideSquare();
                    HandleCollisions();
                    UpdatePosition(subDeltaTime);
                }
            }

            UpdateVisuals();
        }

        [Group("Add-Remove")] public int AmountToAdd;

        [Group("Add-Remove"), Button]
        // ReSharper disable once UnusedMember.Local
        private void AddAmount()
        {
            for (var i = 0; i < AmountToAdd; i++) AddParticle();
        }

        [Group("Add-Remove"), Button]
        private void RemoveAmount()
        {
            for (var i = 0; i < AmountToAdd; i++) RemoveParticle();
        }

        private void AddParticle() => AddParticle(GetRandomPointInArea());

        public void AddParticle(Vector2 position, bool ignoreResLock = false)
        {
            Particle particle = new()
            {
                CurrentPosition = position,
                PreviousPosition = position,
                Mesh = Mesh,
                RandomDirection = Random.insideUnitCircle,
                RandomRotation = Random.value,
                RandomScaleValue = Random.value,
                RandomRadius = Random.value
            };
            Particles.Add(particle);
            PaintParticle(particle, ignoreResLock);
        }

        private Vector2 GetRandomPointInArea() => new(
            Random.Range(AreaSize * -0.5f, AreaSize * 0.5f),
            Random.Range(AreaSize * -0.5f, AreaSize * 0.5f));

        private void RemoveParticle()
        {
            if (Particles.Count > 0) Particles.RemoveAt(0);
        }


        private bool IsResLocked(IParticleType type) =>
            type != null && _isResLocked.ContainsKey(type.Id) && _isResLocked[type.Id];

        public void PaintParticles()
        {
            float totalArea = 0;
            Settings.ResLock.ForEach(res => _isResLocked[res.Type.Id] = res.IsLocked);

            foreach (var t in Particles)
            {
                if (!IsResLocked(t.Type)) PaintParticle(t);

                if (!_radiusToSquare.ContainsKey(t.ParticleRadius))
                    _radiusToSquare[t.ParticleRadius] = Mathf.PI * t.ParticleRadius * t.ParticleRadius;

                totalArea += _radiusToSquare[t.ParticleRadius];
            }

            var squareArea = AreaSize * AreaSize;
            var minArea = squareArea * AreaCoeffMin;
            var maxArea = squareArea * AreaCoeffMax;
            var avgParticleArea = totalArea / Particles.Count;

            if (totalArea < minArea)
            {
                var diff = (minArea - totalArea) / avgParticleArea;
                for (var i = 0; i < diff; i++) AddParticle();
            }
            else if (totalArea > maxArea)
            {
                var diff = (totalArea - maxArea) / avgParticleArea;
                for (var i = 0; i < diff; i++) RemoveParticle();
            }

            var resCount = Particles.Count(t => t.Type != null);
            _ui.SetResCount(resCount);

            _ui.SetResCount(Particles
                .Where(p => p.Type != null)
                .GroupBy(p => p.Type)
                .OrderBy(p => p.Key.Name)
                .Select(p => new ParticleGeneratorUI.ResCountArgs
                {
                    ResName = p.Key.Name,
                    ResColor = p.Key.Color,
                    ResCount = p.Count(),
                }));
        }

        private void PaintParticle(Particle particle, bool ignoreResLock = false)
        {
            var pixel = PositionToPixel(particle.CurrentPosition);
            var color = ParticleColors.GetPixel(pixel.x, pixel.y);
            particle.Type = null;
            particle.Material = _settings.VoidMaterial;

            var isNonResource = false;

            foreach (var args in _settings.NonResourceParticles)
            {
                if (!color.CompareColorsWithTolerance(args.Color, _settings.ColorCompareTolerance)) continue;

                particle.BaseRadius = args.Radius;
                isNonResource = true;
                break;
            }

            if (isNonResource) return;

            var type = Types.OrderBy(t => (t.Color - color).ToVector3().sqrMagnitude).First();

            if (!ignoreResLock && IsResLocked(type)) return;

            particle.Material = _typeMaterials[type];
            particle.BaseRadius = type.Radius;
            particle.Type = type;
        }

        private Vector2Int PositionToPixel(Vector2 position)
        {
            var normalPos = position / AreaSize + Vector2.one * 0.5f;
            return new Vector2Int((int)(normalPos.x * ParticleColors.width),
                (int)(normalPos.y * ParticleColors.height));
        }

        private void PopulateGrid()
        {
            _grid.Clear();
            foreach (var particle in Particles)
            {
                var cell = GetGridCell(particle.CurrentPosition);
                if (!_grid.ContainsKey(cell)) _grid[cell] = new List<Particle>();
                _grid[cell].Add(particle);
            }
        }

        private Vector2Int GetGridCell(Vector2 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / CellSize), Mathf.FloorToInt(position.y / CellSize));
        }

        private void UpdatePosition(float deltaTime)
        {
            foreach (var particle in Particles.Where(particle => !IsResLocked(particle.Type)))
                particle.UpdatePosition(deltaTime, Damp);
        }

        private void KeepParticlesInsideSquare()
        {
            var minBounds = Vector2.one * (AreaSize * -0.5f);
            var maxBounds = Vector2.one * (AreaSize * 0.5f);

            foreach (var particle in Particles.Where(particle => !IsResLocked(particle.Type)))
                particle.KeepInsideSquare(minBounds, maxBounds);
        }

        private void HandleCollisions()
        {
            PopulateGrid();
            foreach (var cell in _grid.Keys)
            {
                var cellParticles = _grid[cell];
                foreach (var neighborCell in GetNeighboringCells(cell))
                    if (_grid.TryGetValue(neighborCell, out var neighborParticles))
                        CheckCollisionsBetweenCells(cellParticles, neighborParticles);
            }

            foreach (var particle in Particles)
            foreach (var levelObject in _levelObjects)
                ResolveCollision(particle, levelObject);
        }

        /// <summary>
        /// returns all cels around including this cell
        /// </summary>
        private static List<Vector2Int> GetNeighboringCells(Vector2Int cell)
        {
            var neighbors = new List<Vector2Int>();
            for (var x = -1; x <= 1; x++)
            for (var y = -1; y <= 1; y++)
                neighbors.Add(new Vector2Int(cell.x + x, cell.y + y));
            return neighbors;
        }

        private void CheckCollisionsBetweenCells(List<Particle> cell1Particles, List<Particle> cell2Particles)
        {
            foreach (var particle1 in cell1Particles)
            foreach (var particle2 in cell2Particles.Where(particle2 => particle1 != particle2))
                ResolveCollision(particle1, particle2);
        }

        private void ResolveCollision(IParticle particle1, IParticle particle2)
        {
            var collisionAxis = particle1.CurrentPosition - particle2.CurrentPosition;
            var dist = collisionAxis.magnitude;
            var minDist = particle1.Radius + particle2.Radius;

            if (dist > minDist) return;

            // Normalize the collision axis to get the direction of the collision
            var n = collisionAxis / dist;
            var delta = minDist - dist;

            var isLocked1 = IsResLocked(particle1.Type);
            var isLocked2 = IsResLocked(particle2.Type);

            // Adjust positions to resolve overlap
            if (isLocked1 == isLocked2)
            {
                particle1.CurrentPosition += 0.5f * delta * n;
                particle2.CurrentPosition -= 0.5f * delta * n;
            }
            else if (isLocked1) particle2.CurrentPosition -= delta * n;
            else particle1.CurrentPosition += delta * n;
        }

        private void UpdateVisuals()
        {
            _area.localScale = Vector3.one * AreaSize;
            Particle.VisualUpdateArgs args = new()
                { AreaSize = 1, DrawRes = DrawResources, Rotation = Quaternion.identity };
            Particles.ForEach(p => p.UpdateVisual(args));
        }
    }
}
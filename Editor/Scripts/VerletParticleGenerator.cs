using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using TriInspector;
using UnityEngine.Profiling;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Flexus.ParticleMapEditor.Editor
{
    [BurstCompile]
    public struct ParticleData
    {
        public float2 CurrentPosition;
        public float Radius;
        public bool IsLocked;
    }

    [BurstCompile]
    public struct GridCollisionJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<ParticleData> Particles;
        [Unity.Collections.ReadOnly] public NativeParallelMultiHashMap<int2, int> Grid;
        [Unity.Collections.ReadOnly] public float CellSize;

        public void Execute(int index)
        {
            var particle1 = Particles[index];
            int2 gridCell = GetGridCell(particle1.CurrentPosition, CellSize);

            // Iterate over the current cell and neighboring cells
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int2 neighborCell = new int2(gridCell.x + x, gridCell.y + y);

                    if (Grid.TryGetFirstValue(neighborCell, out int neighborIndex, out var iterator))
                    {
                        do
                        {
                            if (neighborIndex == index) continue; // Skip self-collision

                            var particle2 = Particles[neighborIndex];

                            // Early exit if both particles are locked
                            if (particle1.IsLocked && particle2.IsLocked)
                                continue;

                            // Collision detection and resolution
                            float2 collisionAxis = particle1.CurrentPosition - particle2.CurrentPosition;
                            float sqrDist = math.lengthsq(collisionAxis);
                            float minDist = particle1.Radius + particle2.Radius;
                            float sqrMinDist = minDist * minDist;

                            if (sqrDist <= sqrMinDist)
                            {
                                float dist = math.sqrt(sqrDist);
                                float2 n = collisionAxis / dist;
                                float delta = minDist - dist;

                                if (particle1.IsLocked == particle2.IsLocked)
                                {
                                    particle1.CurrentPosition += 0.5f * delta * n;
                                    particle2.CurrentPosition -= 0.5f * delta * n;
                                }
                                else if (particle1.IsLocked)
                                {
                                    particle2.CurrentPosition -= delta * n;
                                }
                                else
                                {
                                    particle1.CurrentPosition += delta * n;
                                }

                                // Update particle states
                                Particles[index] = particle1;
                                Particles[neighborIndex] = particle2;
                            }
                        } while (Grid.TryGetNextValue(out neighborIndex, ref iterator));
                    }
                }
            }
        }

        private static int2 GetGridCell(float2 position, float cellSize)
        {
            return new int2((int)math.floor(position.x / cellSize), (int)math.floor(position.y / cellSize));
        }
    }

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

        public List<Particle> Particles { get; } = new();
        public List<LevelObject> LevelObjects { get; } = new();

        public ParticleSettings Settings => _settings;
        private float MinDensity => _settings.MinDensity;
        private float MaxDensity => _settings.MaxDensity;
        private int Count => _settings.InitialCount;
        private float Damp => _settings.Damp;
        private bool IsUpdating => _settings.Update;
        private float AreaSize => _settings.AreaSize;
        private List<ParticleType> Types => _settings.Types;
        private int SubSteps => _settings.SubSteps;
        private int SpawnPerFrame => _settings.SpawnPerFrame;
        private Texture2D ParticleColors => _settings.ParticleColors;
        private Mesh DefaultMesh => _settings.Mesh;
        private float CellSize => _settings.CellSize;
        private float RepaintTimeStep => _settings.RepaintTimeStep;
        private bool DrawResources => _settings.drawResources;

        private IEnumerator Start()
        {
            Settings.UpdateResLock();

            _settings.VoidMaterial.enableInstancing = true;
            foreach (var type in Types)
            {
                _radiusToSquare[type.Radius] = Mathf.PI * type.Radius * type.Radius;
                _typeMaterials[type] = new Material(_settings.ResMaterialSource)
                    { color = type.Color, enableInstancing = true };
            }

            foreach (var config in _settings.levelObjects.levelObjects)
            {
                var levelObject = new LevelObject()
                {
                    config = config,
                    instance = Instantiate(config.prefab, transform),
                };
                LevelObjects.Add(levelObject);
                _isResLocked[config.Id] = true;
            }

            yield return null;

            for (var i = 0; i < Count;)
            {
                for (var j = 0; j < SpawnPerFrame && i < Count; j++, i++) AddParticle();
                yield return null;
            }

            Debug.Log("Particles added");
        }

        private void Update()
        {
            if (IsUpdating)
            {
                if (Time.time > _lastPaintTime + RepaintTimeStep)
                {
                    Profiler.BeginSample("PaintParticles");
                    PaintParticles();
                    Profiler.EndSample();
                    _lastPaintTime = Time.time;
                }

                var subDeltaTime = Time.deltaTime / SubSteps;

                for (var i = 0; i < SubSteps; i++)
                {
                    Profiler.BeginSample("KeepParticlesInsideSquare");
                    KeepParticlesInsideSquare();
                    Profiler.EndSample();
                    Profiler.BeginSample("HandleCollisions");
                    HandleCollisionsWithGridJobs();
                    Profiler.EndSample();
                    Profiler.BeginSample("UpdatePosition");
                    UpdatePosition(subDeltaTime);
                    Profiler.EndSample();
                }
            }

            Profiler.BeginSample("UpdateVisuals");
            UpdateVisuals();
            Profiler.EndSample();
        }

        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
        //
        //     int gridSize = (int)(AreaSize / CellSize);
        //     float cellSize = CellSize;
        //     float height = 0.2f;
        //     // Draw grid lines
        //     for (int x = -gridSize; x <= gridSize; x++)
        //     {
        //         // Vertical lines
        //         Vector3 start = new Vector3(x * cellSize, height, -gridSize * cellSize);
        //         Vector3 end = new Vector3(x * cellSize, height, gridSize * cellSize);
        //         Gizmos.DrawLine(start, end);
        //     }
        //
        //     for (int z = -gridSize; z <= gridSize; z++)
        //     {
        //         // Horizontal lines
        //         Vector3 start = new Vector3(-gridSize * cellSize, height, z * cellSize);
        //         Vector3 end = new Vector3(gridSize * cellSize, height, z * cellSize);
        //         Gizmos.DrawLine(start, end);
        //     }
        // }

        [Group("Add-Remove")] public int AmountToAdd;

        [Group("Add-Remove"), Button]
        // ReSharper disable once UnusedMember.Local
        private void AddAmount()
        {
            for (var i = 0; i < AmountToAdd; i++) AddParticle();
        }

        [Group("Add-Remove"), Button]
        // ReSharper disable once UnusedMember.Local
        private void RemoveAmount()
        {
            for (var i = 0; i < AmountToAdd; i++) RemoveParticle();
        }

        private void AddParticle()
        {
            AddParticle(GetRandomPointInArea());
            //Debug.Log("AddParticle");
        }

        public void AddParticle(Vector2 position, bool ignoreResLock = false)
        {
            Particle particle = new()
            {
                CurrentPosition = position,
                PreviousPosition = position,
                Mesh = DefaultMesh,
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
            //Debug.Log("RemoveParticle");
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
            var minArea = squareArea * MinDensity;
            var maxArea = squareArea * MaxDensity;
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
            foreach (var cell in _grid.Values) cell.Clear(); // Clear cell lists only

            foreach (var particle in Particles)
            {
                var cell = GetGridCell(particle.CurrentPosition, CellSize);
                if (!_grid.TryGetValue(cell, out var cellParticles))
                {
                    cellParticles = new List<Particle>();
                    _grid[cell] = cellParticles;
                }

                cellParticles.Add(particle);
            }
        }


        private static Vector2Int GetGridCell(Vector2 position, float cellSize)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.y / cellSize));
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

        private void HandleCollisionsWithGridJobs()
        {
            // Convert particles to NativeArray
            NativeArray<ParticleData> particleArray = new NativeArray<ParticleData>(Particles.Count, Allocator.TempJob);
            for (int i = 0; i < Particles.Count; i++)
            {
                var particle = Particles[i];
                particleArray[i] = new ParticleData
                {
                    CurrentPosition = particle.CurrentPosition,
                    Radius = particle.Radius,
                    IsLocked = IsResLocked(particle.Type)
                };
            }

            // Populate the grid
            var grid = PopulateGrid(particleArray, CellSize);

            // Schedule the collision job
            var job = new GridCollisionJob
            {
                Particles = particleArray,
                Grid = grid,
                CellSize = CellSize
            };

            JobHandle handle = job.Schedule(particleArray.Length, 64); // Batch size 64
            handle.Complete();

            // Copy results back to original particles
            for (int i = 0; i < Particles.Count; i++)
            {
                var updatedParticle = particleArray[i];
                Particles[i].CurrentPosition = updatedParticle.CurrentPosition;
            }

            // Dispose of NativeArrays and Grid
            particleArray.Dispose();
            grid.Dispose();
        }
        
        private NativeParallelMultiHashMap<int2, int> PopulateGrid(NativeArray<ParticleData> particles, float cellSize)
        {
            var grid = new NativeParallelMultiHashMap<int2, int>(particles.Length, Allocator.TempJob);

            for (int i = 0; i < particles.Length; i++)
            {
                int2 cell = GetGridCell(particles[i].CurrentPosition, cellSize);
                grid.Add(cell, i);
            }

            return grid;
        }

        private static int2 GetGridCell(float2 position, float cellSize)
        {
            return new int2((int)math.floor(position.x / cellSize), (int)math.floor(position.y / cellSize));
        }

        private void HandleCollisions()
        {
            Profiler.BeginSample("PopulateGrid");
            PopulateGrid();
            Profiler.EndSample();

            Profiler.BeginSample("CheckCollisionsBetweenParticles");
            foreach (var cell in _grid.Keys)
            {
                var cellParticles = _grid[cell];

                if (!cellParticles.Any()) continue;

                Profiler.BeginSample("maxRadius");
                var maxRadius = cellParticles.Max(p => p.Radius);
                Profiler.EndSample();

                var searchRadius = Mathf.FloorToInt(maxRadius / CellSize) + 1;
                foreach (var neighborCell in GetNeighboringCells(cell, searchRadius))
                    if (_grid.TryGetValue(neighborCell, out var neighborParticles))
                        CheckCollisionsBetweenCells(cellParticles, neighborParticles);
            }

            Profiler.EndSample();

            Profiler.BeginSample("CheckCollisionsWithLevelObjects");
            foreach (var levelObject in LevelObjects)
            {
                var objectCell = GetGridCell(levelObject.CurrentPosition, CellSize);
                var searchRadius = Mathf.CeilToInt(levelObject.Radius / CellSize);
                foreach (var neighborCell in GetNeighboringCells(objectCell, searchRadius))
                {
                    if (!_grid.TryGetValue(neighborCell, out var neighborParticles)) continue;
                    foreach (var particle in neighborParticles) ResolveCollision(levelObject, particle);
                }
            }

            Profiler.EndSample();
        }

        /// <summary>
        /// Returns all cells around including this cell
        /// </summary>
        private static Vector2Int[] GetNeighboringCells(Vector2Int cell, int radius = 1)
        {
            Profiler.BeginSample("GetNeighboringCellsArray");

            int neighborCount = (radius * 2 + 1) * (radius * 2 + 1);
            var neighbors = new Vector2Int[neighborCount];
            int index = 0;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    neighbors[index++] = new Vector2Int(cell.x + x, cell.y + y);
                }
            }

            Profiler.EndSample();
            return neighbors;
        }


        private void CheckCollisionsBetweenCells(List<Particle> cell1Particles, List<Particle> cell2Particles)
        {
            Profiler.BeginSample("CheckCollisionsBetweenCells");
            for (var i = 0; i < cell1Particles.Count; i++)
            {
                var particle1 = cell1Particles[i];
                for (var index = 0; index < cell2Particles.Count; index++)
                {
                    var particle2 = cell2Particles[index];
                    if (particle1 == particle2) continue;
                    Profiler.BeginSample("ResolveCollision");
                    ResolveCollision(particle1, particle2);
                    Profiler.EndSample();
                }
            }

            Profiler.EndSample();
        }

        private void ResolveCollision(IParticle particle1, IParticle particle2)
        {
            // Compute the collision axis and squared distance to avoid costly sqrt
            var collisionAxis = particle1.CurrentPosition - particle2.CurrentPosition;
            var sqrDist = collisionAxis.sqrMagnitude;
            var minDist = particle1.Radius + particle2.Radius;
            var sqrMinDist = minDist * minDist;

            // Early exit if particles are not colliding
            if (sqrDist > sqrMinDist) return;

            // Normalize the collision axis to get the direction of the collision
            var dist = Mathf.Sqrt(sqrDist); // Compute only when needed
            var n = collisionAxis / dist;
            var delta = minDist - dist;

            var isLocked1 = IsResLocked(particle1.Type);
            var isLocked2 = IsResLocked(particle2.Type);

            // Adjust positions to resolve overlap
            if (isLocked1 == isLocked2)
            {
                var halfDelta = 0.5f * delta; // Avoid redundant multiplication
                particle1.CurrentPosition += halfDelta * n;
                particle2.CurrentPosition -= halfDelta * n;
            }
            else if (isLocked1)
            {
                particle2.CurrentPosition -= delta * n;
            }
            else
            {
                particle1.CurrentPosition += delta * n;
            }
        }

        private Dictionary<ParticleType, List<Matrix4x4>> _modelsToRender = new();
        private Dictionary<Material, List<Matrix4x4>> _particlesToRender = new();

        private void UpdateVisuals()
        {
            if (_area.localScale != Vector3.one * AreaSize)
                _area.localScale = Vector3.one * AreaSize;

            Particle.VisualUpdateArgs args = new()
                { AreaSize = 1, DrawRes = DrawResources, Rotation = Quaternion.identity };

            _modelsToRender.Clear();
            _particlesToRender.Clear();

            for (var index = 0; index < Particles.Count; index++)
            {
                var renderArgs = Particles[index].UpdateVisual(args);
                if (!DrawResources)
                {
                    if (!_particlesToRender.ContainsKey(renderArgs.Material))
                        _particlesToRender[renderArgs.Material] = new List<Matrix4x4>();
                    _particlesToRender[renderArgs.Material].Add(renderArgs.Matrix);
                }
                else if (renderArgs.HasType)
                {
                    if (!_modelsToRender.ContainsKey(renderArgs.Type))
                        _modelsToRender[renderArgs.Type] = new List<Matrix4x4>();
                    _modelsToRender[renderArgs.Type].Add(renderArgs.Matrix);
                }
            }

            if (DrawResources)
            {
                foreach (var type in _modelsToRender.Keys)
                {
                    var renderParams = new RenderParams
                        { shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On };
                    var resMaterial = type.ResMaterial;
                    var instanceData = _modelsToRender[type];
                    for (int i = 0; i < resMaterial.Count; i++)
                    {
                        renderParams.material = resMaterial[Mathf.Min(i, resMaterial.Count - 1)];
                        Graphics.RenderMeshInstanced(renderParams, type.ResMesh, i, instanceData, instanceData.Count);
                    }
                }
            }
            else
            {
                foreach (var material in _particlesToRender.Keys)
                {
                    var renderParams = new RenderParams(material);
                    var instanceData = _particlesToRender[material];
                    Graphics.RenderMeshInstanced(renderParams, DefaultMesh, 0, instanceData, instanceData.Count);
                }
            }
        }
    }
}
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using TriInspector;

namespace Flexus.ParticleMapEditor.Editor
{

    [DeclareBoxGroup("Add-Remove")]
    [DeclareFoldoutGroup(Constants.Dev)]
    public class VerletParticleGenerator : MonoBehaviour
    {
        //public float Seed;
        //public Vector2 pos;

        //[Button]
        //public void PrintRandVector()
        //{
        //    print(PseudoRandomInsideUnitCircle.GetVector(Seed));
        //    print(pos.GetHashCode());   
        //}

        [InlineEditor]
        [SerializeField] private ParticleSettings _settings;
        [Group(Constants.Dev)]
        [SerializeField] private Transform _area;
        [Group(Constants.Dev)]
        [SerializeField] private Transform _rotated;
        [Group(Constants.Dev)]
        [SerializeField] private Transform _camera;
        [Group(Constants.Dev)]
        [SerializeField] private Transform _cameraControl;
        [Group(Constants.Dev)]
        [SerializeField] private ParticleGeneratorUI _ui;

        private List<Particle> _particles = new();
        private Dictionary<Vector2Int, List<Particle>> _grid = new();
        private Dictionary<float, float> _radiusToSquare = new();
        private Dictionary<ParticleType, Material> _typeMaterials = new();
        private float _lastPaintTime;
        private float _areaRotationAngle;

        public ParticleSettings Settings => _settings;
        public float AreaCoeffMin => _settings.AreaCoeffMin;
        public float AreaCoeffMax => _settings.AreaCoeffMax;
        public int Count => _settings.InitialCount;
        public float Damp => _settings.Damp;
        public bool IsUpdating => _settings.Update;
        public float AreaSize => _settings.AreaSize;
        public List<ParticleType> Types => _settings.Types;
        public int SubSteps => _settings.SubSteps;
        public int SpawnPerFrame => _settings.SpawnPerFrame;
        public Texture2D ParticleColors => _settings.ParticleColors;
        public Mesh Mesh => _settings.Mesh;
        public float CellSize => _settings.CellSize;
        public float RepaintTimeStep => _settings.RepaintTimeStep;
        public bool DrawResourses => _settings.DrawResourses;
        public float CameraDistance => _settings.CameraDistance;
        public float CameraRotationSpeed => _settings.CameraRotationSpeed;
        public List<Particle> Particles => _particles;

        private IEnumerator Start()
        {
            foreach (var type in Types)
            {
                _radiusToSquare[type.Radius] = Mathf.PI * type.Radius * type.Radius;
                _typeMaterials[type] = new Material(_settings.ResMaterialSource);
                _typeMaterials[type].color = type.Color;
            }

            for (int i = 0; i < Count;)
            {
                for (int j = 0; j < SpawnPerFrame && i < Count; j++, i++)
                {
                    AddParticle();
                }

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

                float subDeltaTime = Time.deltaTime / SubSteps;

                for (int i = 0; i < SubSteps; i++)
                {
                    KeepParticlesInsideSquare();
                    HandleCollisions();  // Call collision handling
                    UpdatePosition(subDeltaTime);
                }
            }

            UpdateVisuals();
        }

        [Group("Add-Remove")] public int AmountToAdd;

        [Group("Add-Remove"), Button]
        private void AddAmount()
        {
            for (int i = 0; i < AmountToAdd; i++)
                AddParticle();
        }

        [Group("Add-Remove"), Button]
        private void RemoveAmount()
        {
            for (int i = 0; i < AmountToAdd; i++)
                RemoveParticle();
        }

        private void AddParticle()
        {
            AddParticle(GetRandomPointInArea());
        }

        public void AddParticle(Vector2 position)
        {
            Particle particle = new();
            _particles.Add(particle);

            particle.CurrentPosition = position;
            particle.PreviousPosition = particle.CurrentPosition;
            particle.Mesh = Mesh;
            particle.RandomDirection = Random.insideUnitCircle;
            particle.RandomRotation = Random.value;
            particle.RandomScaleValue = Random.value;
            particle.RandomRadius = Random.value;
            PaintParticle(particle);
        }

        private Vector2 GetRandomPointInArea() => new Vector2(
                Random.Range(AreaSize * -0.5f, AreaSize * 0.5f),
                Random.Range(AreaSize * -0.5f, AreaSize * 0.5f));

        private void RemoveParticle()
        {
            if (_particles.Count > 0)
                _particles.RemoveAt(0);
        }

        //[Button]
        public void PaintParticles()
        {
            float totalArea = 0;

            for (int i = 0; i < _particles.Count; i++)
            {
                PaintParticle(_particles[i]);

                if (!_radiusToSquare.ContainsKey(_particles[i].ParticleRadius))
                    _radiusToSquare[_particles[i].ParticleRadius] = Mathf.PI * _particles[i].ParticleRadius * _particles[i].ParticleRadius;

                totalArea += _radiusToSquare[_particles[i].ParticleRadius];
            }

            float squareArea = AreaSize * AreaSize;
            float minArea = squareArea * AreaCoeffMin;
            float maxArea = squareArea * AreaCoeffMax;
            float avgParticleArea = totalArea / _particles.Count;

            if (totalArea < minArea)
            {
                float diff = (minArea - totalArea) / avgParticleArea;

                for (int i = 0; i < diff; i++)
                    AddParticle();
            }
            else if (totalArea > maxArea)
            {
                float diff = (totalArea - maxArea) / avgParticleArea;

                for (int i = 0; i < diff; i++)
                    RemoveParticle();
            }

            int resCount = 0;

            for (int i = 0; i < _particles.Count; i++)
                if (_particles[i].Type != null)
                    resCount++;

            _ui.SetResCount(resCount);

            //Debug.Log(totalArea + "/ " + squareArea + " = " + (totalArea / squareArea));
        }

        private void PaintParticle(Particle particle)
        {
            Vector2Int pixel = PositionToPixel(particle.CurrentPosition);
            Color color = ParticleColors.GetPixel(pixel.x, pixel.y);
            particle.Type = null;
            particle.Material = _settings.VoidMaterial;

            bool isNonResource = false;

            foreach (var args in _settings.NonResourceParticles)
            {
                if(color.CompareColorsWithTolerance(args.Color, _settings.ColorCompareTolerance))
                {
                    particle.BaseRadius = args.Radius;
                    isNonResource = true;

                    break;
                }
            }

            if(!isNonResource)
            {
                ParticleType type = Types.OrderBy(_ => ColorToVector3(_.Color - color).sqrMagnitude).First();
                particle.Material = _typeMaterials[type];
                particle.BaseRadius = type.Radius;
                particle.Type = type;
            }
        }

        Vector2Int PositionToPixel(Vector2 position)
        {
            Vector2 normalPos = position / AreaSize + Vector2.one * 0.5f;
            return new Vector2Int((int)(normalPos.x * ParticleColors.width), (int)(normalPos.y * ParticleColors.height));
        }

        Vector3 ColorToVector3(Color color)
        {
            return new Vector3(color.r, color.g, color.b);
        }

        private void PopulateGrid()
        {
            _grid.Clear();

            foreach (var particle in _particles)
            {
                Vector2Int cell = GetGridCell(particle.CurrentPosition);

                if (!_grid.ContainsKey(cell))
                    _grid[cell] = new List<Particle>();

                _grid[cell].Add(particle);
            }
        }

        private Vector2Int GetGridCell(Vector2 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / CellSize),
                Mathf.FloorToInt(position.y / CellSize)
            );
        }

        private void UpdatePosition(float deltaTime)
        {
            _particles.ForEach(_ => _.UpdatePosition(deltaTime, Damp));
        }

        private void KeepParticlesInsideSquare()
        {
            Vector2 minBounds = Vector2.one * AreaSize * -0.5f;
            Vector2 maxBounds = Vector2.one * AreaSize * 0.5f;
            _particles.ForEach(_ => _.KeepInsideSquare(minBounds, maxBounds));
        }

        private void HandleCollisions()
        {
            PopulateGrid();

            foreach (var cell in _grid.Keys)
            {
                List<Particle> cellParticles = _grid[cell];

                foreach (var neighbor in GetNeighboringCells(cell))
                    if (_grid.ContainsKey(neighbor))
                        CheckCollisionsBetweenCells(cellParticles, _grid[neighbor]);
            }
        }

        private List<Vector2Int> GetNeighboringCells(Vector2Int cell)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();

            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    neighbors.Add(new Vector2Int(cell.x + x, cell.y + y));

            return neighbors;
        }

        private void CheckCollisionsBetweenCells(List<Particle> cell1Particles, List<Particle> cell2Particles)
        {
            foreach (var particle1 in cell1Particles)
                foreach (var particle2 in cell2Particles)
                    if (particle1 != particle2)
                        ResolveCollision(particle1, particle2);
        }

        private void ResolveCollision(Particle particle1, Particle particle2)
        {
            Vector2 collisionAxis = particle1.CurrentPosition - particle2.CurrentPosition;
            float dist = collisionAxis.magnitude;
            float minDist = particle1.ParticleRadius + particle2.ParticleRadius;

            if (dist < minDist) // Collision detected
            {
                // Normalize the collision axis to get the direction of the collision
                Vector2 n = collisionAxis / dist;
                float delta = minDist - dist;

                // Adjust positions to resolve overlap
                particle1.CurrentPosition += 0.5f * delta * n;
                particle2.CurrentPosition -= 0.5f * delta * n;
            }
        }

        private void UpdateVisuals()
        {
            _areaRotationAngle += CameraRotationSpeed * Time.deltaTime;
            _area.localScale = Vector3.one * AreaSize;
            Quaternion rotation = Quaternion.Euler(0, _areaRotationAngle, 0);
            _rotated.localRotation = rotation;
            _camera.localPosition = new Vector3(AreaSize * -0.4f, AreaSize, 0);
            _cameraControl.localScale = Vector3.one * CameraDistance;

            Particle.VisualUpdateArgs args = new() { AreaSize = 1, DrawRes = DrawResourses, Rotation = rotation };

            //for (int i = 0; i < _particles.Count; i++)
            //{
            //    Particle particle = _particles[i];
            //    particle.RandomAmount = 
            //}
            _particles.ForEach(_ => _.UpdateVisual(args));
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    public interface IParticle
    {
        Vector2 CurrentPosition { get; set; }
        float Radius { get; }
        IParticleType Type { get; }
    }
    
    [System.Serializable]
    public class LevelObject:IParticle
    {
        public LevelObjectConfig config;
        public Transform instance;

        public Vector2 CurrentPosition
        {
            get => new(instance.position.x, instance.position.z);
            set => instance.position = new Vector3(value.x, 0f, value.y);
        }
        public float Radius => config.radius;
        public IParticleType Type => config;
    }
    
    [System.Serializable]
    public class Particle : IParticle
    {
        public Vector2 CurrentPosition;
        public Vector2 PreviousPosition;
        public Vector2 Acceleration;
        /// <summary>
        /// From res collider radius
        /// </summary>
        public float BaseRadius = 0.5f;
        public Material Material;
        public Mesh Mesh;

        /// <summary>
        /// Random.value
        /// </summary>
        public float RandomRadius;
        public ParticleType Type;
        public Vector2 RandomDirection;
        public float RandomRotation;
        public float RandomScaleValue; // Amount of mixing with randomized scale

        public float Offset => Type != null ? Type.Offset : 0f;
        public Vector2 RandomRadiusRange => Type.RadiusRandomization;
        public float RandRadiusCoeff => RandomRadius.Remap(0, 1, RandomRadiusRange.x, RandomRadiusRange.y);
        /// <summary>
        /// collider radius with particle radius randomization
        /// </summary>
        public float ResourceRadius => Type == null ? BaseRadius : BaseRadius * RandRadiusCoeff;
        /// <summary>
        /// collider radius with particle radius randomization + offset
        /// final radius of particle!
        /// </summary>
        public float ParticleRadius => ResourceRadius + Offset;
        public float RandomAmount => Type.RandomPosition;
        public float RandomRotationCoeff => Type.RandomRotation;
        public float RandomScaleCoeff => Type.RandomScale;
        public Mesh ResMesh => Type.ResMesh;
        public List<Material> ResMaterial => Type.ResMaterial;
        public Vector2 RandomScaleRange => Type.RandomScaleRange; // Min-max scale when randomized
        public float ResScale => Type.ImpactsOnResource ? Type.ResScale * RandRadiusCoeff : Type.ResScale;

        public void UpdatePosition(float deltaTime, float damp)
        {
            var velocity = (CurrentPosition - PreviousPosition);
            PreviousPosition = CurrentPosition;
            CurrentPosition += velocity * damp;

            if (CurrentPosition.x is not float.NaN && CurrentPosition.y is not float.NaN) return;
            
            if (PreviousPosition.x is float.NaN || PreviousPosition.y is float.NaN)
                PreviousPosition = Random.insideUnitCircle;
                
            CurrentPosition = PreviousPosition;
            //PreviousPosition = Vector2.zero;
        }

        public void KeepInsideSquare(Vector2 minBounds, Vector2 maxBounds)
        {
            // X-axis constraint
            if (CurrentPosition.x - ParticleRadius < minBounds.x)
            {
                CurrentPosition.x = minBounds.x + ParticleRadius;
            }
            else if (CurrentPosition.x + ParticleRadius > maxBounds.x)
            {
                CurrentPosition.x = maxBounds.x - ParticleRadius;
            }

            // Y-axis constraint
            if (CurrentPosition.y - ParticleRadius < minBounds.y)
            {
                CurrentPosition.y = minBounds.y + ParticleRadius;
            }
            else if (CurrentPosition.y + ParticleRadius > maxBounds.y)
            {
                CurrentPosition.y = maxBounds.y - ParticleRadius;
            }
        }

        public ParticleRenderArgs UpdateVisual(VisualUpdateArgs args)
        {
            bool randomize = args.DrawRes && Type != null;
            var scale = Vector3.one / args.AreaSize;
            var position2D = CurrentPosition;

            if(randomize)
                position2D += RandomAmount * ParticleRadius * RandomDirection;

            var position = new Vector3(position2D.x, 0, position2D.y) / args.AreaSize;
            position = args.Rotation * position;
            var rotation = args.Rotation;

            if(randomize)
                rotation = Quaternion.Euler(0, RandomRotationCoeff * RandomRotation * 360f, 0) * rotation;

            if (!args.DrawRes) // Draw simple circles
            {
                scale *= ParticleRadius;
                scale *= 2f;
                scale.y = 0.01f;
                var matrix = Matrix4x4.TRS(position, rotation, scale);
                //var renderParams = new RenderParams(Material);
                //Graphics.RenderMesh(renderParams, Mesh, 0, matrix);
                return new ParticleRenderArgs(Material, matrix);
            }
            else if(Type != null) 
            {
                scale *= ResScale;
                float randomScaleCoeff = RandomScaleValue.Remap(0, 1, RandomScaleRange.x, RandomScaleRange.y);
                scale *= Mathf.Lerp(1f, randomScaleCoeff, RandomScaleCoeff);
                var matrix = Matrix4x4.TRS(position, rotation, scale);
                // var renderParams = new RenderParams { shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On };
                // for (int i = 0; i < ResMaterial.Count; i++)
                // {
                //     renderParams.material = ResMaterial[Mathf.Min(i, ResMaterial.Count - 1)];
                //     Graphics.RenderMesh(renderParams, ResMesh, i, matrix);
                // }
                return new ParticleRenderArgs(Type, matrix);
            }

            return default;
        }

        public struct VisualUpdateArgs
        {
            public float AreaSize;
            public bool DrawRes;
            public Quaternion Rotation;
        }
        
        public struct ParticleRenderArgs
        {
            public Matrix4x4 Matrix;
            public bool HasType;
            public ParticleType Type;
            public Material Material;

            public ParticleRenderArgs(Material material, Matrix4x4 matrix)
            {
                Matrix = matrix;
                HasType = false;
                Type = null;
                Material = material;
            }
            
            public ParticleRenderArgs(ParticleType type, Matrix4x4 matrix)
            {
                Matrix = matrix;
                HasType = true;
                Type = type;
                Material = null;
            }
        }
        
        public float Radius => ParticleRadius;
        IParticleType IParticle.Type => Type;
        Vector2 IParticle.CurrentPosition { get => CurrentPosition; set => CurrentPosition = value; }
    }
}
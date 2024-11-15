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
            Vector2 velocity = (CurrentPosition - PreviousPosition);
            PreviousPosition = CurrentPosition;
            CurrentPosition += velocity * damp;

            if (CurrentPosition.x is float.NaN || CurrentPosition.y is float.NaN)
            {
                CurrentPosition = Vector2.zero;
                PreviousPosition = Vector2.zero;
            }
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

        public void UpdateVisual(VisualUpdateArgs args)
        {
            bool randomize = args.DrawRes && Type != null;
            Vector3 scale = Vector3.one / args.AreaSize;
            Vector2 position2D = CurrentPosition;

            if(randomize)
                position2D += RandomAmount * ParticleRadius * RandomDirection;

            Vector3 position = new Vector3(position2D.x, 0, position2D.y) / args.AreaSize;
            position = args.Rotation * position;
            Quaternion rotation = args.Rotation;

            if(randomize)
                rotation = Quaternion.Euler(0, RandomRotationCoeff * RandomRotation * 360f, 0) * rotation;

            if (!args.DrawRes)
            {
                scale *= ParticleRadius;
                scale *= 2f;
                scale.y = 0.01f;
                Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
                Graphics.DrawMesh(Mesh, matrix, Material, 0, null, 0, null, false, false);
            }
            else if(Type != null) 
            {
                scale *= ResScale;
                float randomScaleCoeff = RandomScaleValue.Remap(0, 1, RandomScaleRange.x, RandomScaleRange.y);
                scale *= Mathf.Lerp(1f, randomScaleCoeff, RandomScaleCoeff);
                Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);

                for (int i = 0; i < ResMaterial.Count; i++)
                    Graphics.DrawMesh(ResMesh, matrix, ResMaterial[Mathf.Min(i, ResMaterial.Count - 1)], 0, null, i);
            }
        }

        public struct VisualUpdateArgs
        {
            public float AreaSize;
            public bool DrawRes;
            public Quaternion Rotation;
        }

        public float Radius => ParticleRadius;
        IParticleType IParticle.Type => Type;
        Vector2 IParticle.CurrentPosition { get => CurrentPosition; set => CurrentPosition = value; }
    }
}
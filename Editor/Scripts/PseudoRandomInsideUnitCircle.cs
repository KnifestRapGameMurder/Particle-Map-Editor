using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    public static class PseudoRandomInsideUnitCircle
    {
        public static Vector2 GetVector(float input)
        {
            // Seed the random generator using the input value
            System.Random random = new System.Random(input.GetHashCode());

            // Generate a random angle
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;

            // Generate a random radius (between 0 and 1) with uniform distribution in the unit circle
            float radius = Mathf.Sqrt((float)random.NextDouble());

            // Return the vector based on polar coordinates
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            return new Vector2(x, y);
        }
    }
}
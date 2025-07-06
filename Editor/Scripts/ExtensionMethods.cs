#if UNITY_EDITOR
using UnityEngine;

namespace Flexus.ParticleMapEditor.Editor
{
    public static class ExtensionMethods
    {
        //public static Vector2Int Remap(this Vector2Int value, Vector2Int fromMin, Vector2Int fromMax, Vector2Int toMin, Vector2Int toMax)
        //{
        //    int remappedX = Remap(value.x, fromMin.x, fromMax.x, toMin.x, toMax.x);
        //    int remappedY = Remap(value.y, fromMin.y, fromMax.y, toMin.y, toMax.y);

        //    return new Vector2Int(remappedX, remappedY);
        //}

        // // Compare two colors with tolerance
        // public static bool CompareColorsWithTolerance(this Color color1, Color color2, float tolerance)
        // {
        //     // Calculate the squared difference between the colors for performance
        //     float diffR = (color1.r - color2.r) * (color1.r - color2.r);
        //     float diffG = (color1.g - color2.g) * (color1.g - color2.g);
        //     float diffB = (color1.b - color2.b) * (color1.b - color2.b);
        //     //float diffA = (color1.a - color2.a) * (color1.a - color2.a);
        //
        //     // Calculate the squared distance
        //     float distanceSquared = diffR + diffG + diffB /*+ diffA*/;
        //
        //     // Return true if the squared distance is within the squared tolerance
        //     return distanceSquared <= tolerance * tolerance;
        // }
    }
}
#endif
Shader "Custom/LitIslandHeightColor"
{
    Properties
    {
        _AboveColor ("Above Color", Color) = (0.4, 0.8, 0.3, 1.0) // Color for areas above the threshold
        _BelowColor ("Below Color", Color) = (0.2, 0.4, 0.8, 1.0) // Color for areas below the threshold
        _HeightThreshold ("Height Threshold", Float) = 0.0 // Y value for threshold
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard

        struct Input
        {
            float3 worldPos; // World position to compare against the threshold
        };

        fixed4 _AboveColor;
        fixed4 _BelowColor;
        float _HeightThreshold;
        half _Glossiness;
        half _Metallic;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Choose color based on the Y value of the world position
            fixed4 color = (IN.worldPos.y > _HeightThreshold) ? _AboveColor : _BelowColor;

            // Apply the selected color to the output
            o.Albedo = color.rgb;

            // Set metallic and smoothness for lighting
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

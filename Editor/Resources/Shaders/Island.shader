Shader "Custom/IslandHeightShader"
{
    Properties
    {
        _AboveColor ("Above Color", Color) = (0.4, 0.8, 0.3, 1.0)    // Color for areas above the threshold
        _BelowColor ("Below Color", Color) = (0.2, 0.4, 0.8, 1.0)    // Color for areas below the threshold
        _HeightThreshold ("Height Threshold", Float) = 0.0           // Y value for threshold
        _MainTexAbove ("Above Texture", 2D) = "white" {}             // Texture for areas above the threshold
        _MainTexBelow ("Below Texture", 2D) = "white" {}             // Texture for areas below the threshold
        _Smoothness ("Smoothness", Range(0,1)) = 0.5                 // Smoothness for lighting
        _Metallic ("Metallic", Range(0,1)) = 0.0                     // Metallic value for lighting
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard

        struct Input
        {
            float3 worldPos;            // World position to compare against the threshold
            float2 uv_MainTexAbove;     // UV for above texture
            float2 uv_MainTexBelow;     // UV for below texture
        };

        fixed4 _AboveColor;
        fixed4 _BelowColor;
        float _HeightThreshold;
        float _Smoothness;
        float _Metallic;
        sampler2D _MainTexAbove;
        sampler2D _MainTexBelow;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Determine if we're above or below the height threshold
            bool isAbove = IN.worldPos.y > _HeightThreshold;
            fixed4 baseColor = isAbove ? _AboveColor : _BelowColor;
            fixed4 texColor = isAbove ? tex2D(_MainTexAbove, IN.uv_MainTexAbove) : tex2D(_MainTexBelow, IN.uv_MainTexBelow);

            // Apply base color and texture
            o.Albedo = baseColor.rgb * texColor.rgb;

            // Set metallic and smoothness for softer lighting
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

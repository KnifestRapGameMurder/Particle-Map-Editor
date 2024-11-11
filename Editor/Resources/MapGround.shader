Shader "Unlit/ReceiveShadowsWithMovingOtherTex"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}  // Main texture
        _OtherTex ("Other Texture", 2D) = "white" {} // Other texture
        _MaskTex ("Mask Texture", 2D) = "black" {}   // Mask texture
        _Color ("Tint Color", Color) = (1, 1, 1, 1)  // Color tint for main texture
        _OtherColor ("Other Tint Color", Color) = (1, 1, 1, 1) // Color tint for other texture
        _MoveSpeed ("Other Texture Move Speed", float) = 1.0  // Speed of the moving texture
    }
    SubShader
    {
        Tags { "LightMode"="ForwardBase" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "AutoLight.cginc" // Include for shadow support

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uvMain : TEXCOORD0;
                float2 uvOther : TEXCOORD1;
                float2 uvMask : TEXCOORD2;
                float4 pos : SV_POSITION;
                LIGHTING_COORDS(3, 4) // Using TEXCOORD3 and TEXCOORD4 for lighting/shadow coords
                UNITY_FOG_COORDS(5)   // Moving fog to TEXCOORD5 to avoid overlap
            };

            sampler2D _MainTex;
            sampler2D _OtherTex;
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            float4 _OtherTex_ST;
            float4 _MaskTex_ST;
            float4 _Color;
            float4 _OtherColor;
            float _MoveSpeed; // Speed of the other texture's movement

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uvMain = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvMask = TRANSFORM_TEX(v.uv, _MaskTex);

                // Apply time-based movement to the other texture
                float2 timeOffset = float2(_Time.y * _MoveSpeed, _Time.y * _MoveSpeed); // Calculate offset based on time
                o.uvOther = TRANSFORM_TEX(v.uv, _OtherTex) + timeOffset; // Apply the offset to the UVs

                // Transfer shadow information to the fragment shader using TEXCOORD3
                TRANSFER_VERTEX_TO_FRAGMENT(o);

                // Handle fog using TEXCOORD5
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the mask texture
                fixed4 maskCol = tex2D(_MaskTex, i.uvMask);

                // Check if the mask color is black (all RGB channels are close to zero)
                bool isBlack = ((maskCol.r + maskCol.g + maskCol.b) / 3.0f) < 0.2f;

                // Sample the other texture (with movement) and main texture
                fixed4 otherCol = tex2D(_OtherTex, i.uvOther) * _OtherColor;
                fixed4 mainCol = tex2D(_MainTex, i.uvMain) * _Color;

                // Use the mask to blend between otherTex (if mask is black) and mainTex
                fixed4 finalCol = isBlack ? otherCol : mainCol;

                // Apply shadow attenuation
                fixed atten = LIGHT_ATTENUATION(i);

                // Multiply the texture color by the shadow attenuation
                finalCol.rgb *= atten;

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalCol);
                return finalCol;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

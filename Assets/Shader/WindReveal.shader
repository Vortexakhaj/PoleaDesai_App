Shader "UI/WindReveal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Progress ("Reveal Progress", Range(0,1)) = 0
        _IsHiding ("Is Hiding", Float) = 0
        
        // Direction is now controlled by script, so we just use a raw number here.
        _Direction ("Direction", Float) = 3
        
        _EdgeWidth ("Edge Roughness", Range(0, 0.5)) = 0.1
        _NoiseTex ("Wind Noise Texture", 2D) = "white" {}
        _Speed ("Wind Speed", Float) = 1.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                half2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float _Progress;
            float _IsHiding;
            float _Direction;
            float _EdgeWidth;
            float _Speed;

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // Animate noise UVs
                float2 noiseUV = IN.texcoord + float2(_Time.y * _Speed * 0.1, _Time.y * _Speed * 0.05);
                float noise = tex2D(_NoiseTex, noiseUV).r;

                // Calculate the wind threshold
                float threshold = _Progress * (1.0 + _EdgeWidth) + (noise * _EdgeWidth);

                // --- DYNAMIC DIRECTION LOGIC ---
                float baseCoord;
                
                if (_Direction == 0) baseCoord = IN.texcoord.x;                                            // Left to Right
                else if (_Direction == 1) baseCoord = 1.0 - IN.texcoord.x;                                 // Right to Left
                else if (_Direction == 2) baseCoord = IN.texcoord.y;                                      // Bottom to Top
                else if (_Direction == 3) baseCoord = 1.0 - IN.texcoord.y;                                 // Top to Bottom
                else if (_Direction == 4) baseCoord = (IN.texcoord.x + IN.texcoord.y) * 0.5;              // Bottom-Left to Top-Right
                else if (_Direction == 5) baseCoord = (IN.texcoord.x + (1.0 - IN.texcoord.y)) * 0.5;      // Top-Left to Bottom-Right
                else if (_Direction == 6) baseCoord = ((1.0 - IN.texcoord.x) + IN.texcoord.y) * 0.5;      // Bottom-Right to Top-Left
                else baseCoord = ((1.0 - IN.texcoord.x) + (1.0 - IN.texcoord.y)) * 0.5;                   // Top-Right to Bottom-Left

                // Calculate distance from threshold
                float dist = baseCoord - threshold;
                float softEdge = 0.05;
                
                // Determine visibility based on whether we are revealing or hiding
                float visibility = smoothstep(softEdge, -softEdge, dist); // Revealing
                if (_IsHiding > 0.5) {
                    visibility = smoothstep(-softEdge, softEdge, dist); // Hiding (Reverse)
                }

                color.a *= visibility;
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

                return color;
            }
        ENDCG
        }
    }
}
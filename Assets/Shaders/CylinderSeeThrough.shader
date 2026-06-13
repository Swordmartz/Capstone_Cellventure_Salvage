Shader "Custom/CylinderSeeThrough"
{
    Properties
    {
        _BaseColor      ("Wall Color",          Color)      = (0.8, 0.5, 0.4, 1)
        _BaseMap        ("Texture",             2D)         = "white" {}
        _Smoothness     ("Smoothness",          Range(0,1)) = 0.3
        _PlayerPos      ("Player Position",     Vector)     = (0,0,0,0)
        _CameraPos      ("Camera Position",     Vector)     = (0,10,0,0)
        _RevealRadius   ("Reveal Radius",       Float)      = 3
        _EdgeSoftness   ("Edge Softness",       Range(0,2)) = 0.5
        _CylinderBottom ("Reveal Bottom Offset",Float)      = 0.0
        _CylinderTop    ("Reveal Top Offset",   Float)      = 4.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Smoothness;
                float4 _PlayerPos;
                float4 _CameraPos;
                float  _RevealRadius;
                float  _EdgeSoftness;
                float  _CylinderBottom;
                float  _CylinderTop;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ── Cylindrical reveal — camera-facing side only ───────────────
                // The cut is a vertical cylinder (Y-axis aligned) centered on the
                // player, but only opens on the side facing the camera.
                // The far wall always stays solid.

                float3 fragToPlayer = IN.positionWS - _PlayerPos.xyz;

                // XZ distance drives the cylinder radius
                float dist = length(float2(fragToPlayer.x, fragToPlayer.z));

                // Height bounds: offsets from player Y
                float fragY      = IN.positionWS.y;
                float bottomEdge = _PlayerPos.y + _CylinderBottom;
                float topEdge    = _PlayerPos.y + _CylinderTop;
                bool  inHeight   = (fragY >= bottomEdge) && (fragY <= topEdge);

                if (inHeight)
                {
                    // dot(camToFrag, normal) < 0 means normal faces toward camera
                    // i.e. this is the front/camera-facing side of the cylinder
                    float3 camToFrag = normalize(IN.positionWS - _CameraPos.xyz);
                    float  facing    = dot(camToFrag, normalize(IN.normalWS));

                    if (facing < 0.0)
                    {
                        float edge = smoothstep(
                            _RevealRadius - _EdgeSoftness,
                            _RevealRadius + _EdgeSoftness,
                            dist);

                        clip(edge - 0.01);
                    }
                }

                // ── Lighting ──────────────────────────────────────────────────
                half4  texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4  color    = texColor * _BaseColor;
                float3 normalWS = normalize(IN.normalWS);

                Light  mainLight = GetMainLight();
                half   NdotL     = max(0.2, dot(normalWS, mainLight.direction));
                color.rgb       *= NdotL * mainLight.color;

                color.rgb        = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}

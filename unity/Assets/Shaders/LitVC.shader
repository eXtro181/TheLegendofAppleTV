// URP-PBR mit Vertexfarbe (Tönung/Schmutz der Fassaden) und optionaler Regionsfärbung für Figuren.
Shader "LU/LitVC"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        _Smoothness("Smoothness", Range(0,1)) = 0.3
        _Metallic("Metallic", Range(0,1)) = 0
        _MetallicGlossMap("Metallic (R) Smoothness (A)", 2D) = "white" {}
        _BumpScale("Normal Scale", Float) = 1
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _OcclusionStrength("Occlusion", Range(0,1)) = 1
        _OcclusionMap("Occlusion", 2D) = "white" {}
        [HDR] _EmissionColor("Emission", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        _VCStrength("Vertex Color Strength", Range(0,1)) = 1
        [Toggle(_REGIONS)] _Regions("Character Regions", Float) = 0
        [Toggle(_METALLICSPECGLOSSMAP)] _UseMS("Use Metal/Smooth Map", Float) = 0
        [Toggle(_NORMALMAP)] _UseNormal("Use Normal Map", Float) = 0
        [Toggle(_OCCLUSIONMAP)] _UseOcc("Use Occlusion Map", Float) = 0
        [Toggle(_EMISSION)] _UseEmission("Use Emission", Float) = 0
        [HideInInspector] _Surface("__surface", Float) = 0
        [HideInInspector] _SrcBlend("__src", Float) = 1
        [HideInInspector] _DstBlend("__dst", Float) = 0
        [HideInInspector] _ZWrite("__zw", Float) = 1
        [HideInInspector] _Cull("__cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _METALLICSPECGLOSSMAP
            #pragma shader_feature_local _OCCLUSIONMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _REGIONS
            #pragma shader_feature_local _SURFACE_TYPE_TRANSPARENT
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicGlossMap);
            TEXTURE2D(_BumpMap);
            TEXTURE2D(_OcclusionMap);
            TEXTURE2D(_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _Smoothness;
                half _Metallic;
                half _BumpScale;
                half _OcclusionStrength;
                half4 _EmissionColor;
                half _VCStrength;
            CBUFFER_END
            #ifdef _REGIONS
                float4 _RegionColors[6];
            #endif

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings {
                float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; half3 normalWS:TEXCOORD2;
                half4 tangentWS:TEXCOORD3; half4 color:TEXCOORD4; half fogFactor:TEXCOORD5;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.normalWS = n.normalWS;
                real sgn = v.tangentOS.w * GetOddNegativeScale();
                o.tangentWS = half4(n.tangentWS, sgn);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap); o.color = v.color; o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                #ifdef _REGIONS
                    int r = clamp((int)round(i.color.r * 6.375), 0, 5);
                    albedo.rgb *= _RegionColors[r].rgb * 2.0;
                #else
                    albedo.rgb *= lerp(half3(1,1,1), i.color.rgb, _VCStrength);
                #endif
                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo.rgb; s.alpha = albedo.a;
                #ifdef _METALLICSPECGLOSSMAP
                    half4 ms = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_BaseMap, i.uv);
                    s.metallic = ms.r * _Metallic; s.smoothness = ms.a * _Smoothness;
                #else
                    s.metallic = _Metallic; s.smoothness = _Smoothness;
                #endif
                s.specular = half3(0,0,0);
                #ifdef _NORMALMAP
                    s.normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, i.uv), _BumpScale);
                #else
                    s.normalTS = half3(0,0,1);
                #endif
                #ifdef _OCCLUSIONMAP
                    s.occlusion = LerpWhiteTo(SAMPLE_TEXTURE2D(_OcclusionMap, sampler_BaseMap, i.uv).g, _OcclusionStrength);
                #else
                    s.occlusion = 1;
                #endif
                #ifdef _EMISSION
                    s.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, i.uv).rgb * _EmissionColor.rgb;
                #endif

                InputData d = (InputData)0;
                d.positionWS = i.positionWS;
                half3 bit = cross(i.normalWS, i.tangentWS.xyz) * i.tangentWS.w;
                #ifdef _NORMALMAP
                    d.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(s.normalTS, half3x3(i.tangentWS.xyz, bit, i.normalWS)));
                #else
                    d.normalWS = NormalizeNormalPerPixel(i.normalWS);
                #endif
                d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                d.fogCoord = i.fogFactor;
                d.bakedGI = SampleSH(d.normalWS);
                d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask = half4(1,1,1,1);
                half4 c = UniversalFragmentPBR(d, s);
                c.rgb = MixFog(c.rgb, d.fogCoord);
                return c;
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

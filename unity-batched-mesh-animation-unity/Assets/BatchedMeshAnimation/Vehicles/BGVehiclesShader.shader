Shader "BGVehiclesShader"
{
    Properties
    {
        _PositionRandom ("Position Random", Vector) = (5, 2, 60, 0)
        _AnimationRange ("Animation Range", Float) = 1000
        _AnimationSpeedRange ("Animation Speed(km/h)", Vector) = (90, 120, 0, 0)
        _VehicleCount ("Vehicle Count (This should be match)", Float) = 100
        
        [Header(Color)][Space(5)]
        _BaseColor ("Base color", Color) = (0.65, 0.65, 0.65, 1)
        _BaseMap ("Base Map", 2D) = "white" { }
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.7
        
        _ColorGlass ("Color Glass", Color) = (1, 1, 1, 1)
        _MetallicGlass ("Metallic (Glass)", Range(0, 1)) = 1
        _SmoothnessGlass ("Smoothness (Glass)", Range(0, 1)) = 1

        _ColorFrontLight ("Color Front Light", Color) = (1, 1, 1, 1)
        _ColorBackLight ("Color Back Light", Color) = (1, 0, 0, 1)

        _Brightness ("Brightness", Float) = 6


        _AmbientIntensity ("Ambient Intensity", Float) = 1

        [Header(Reflection)]
        _ReflectionColor ("Reflection Color", Color) = (1, 1, 1, 1)
        _ReflectionIntensity ("Reflection Intensity", Float) = 2
        _ReflectionFresnelPow ("Reflection Fresnel Power", Float) = 3

        [Header(Mask Map)]
        _MaskMap ("Mask Map (rg: normal.xy, b: smoothness)", 2D) = "bump" { }
        _BumpMapScale ("Normal map scale", Range(0, 4)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.7
        _roughnessMultiply ("Roughness Multiply", Float) = 8
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "./BGVehiclesMeshAnimator.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half3 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1; // position random x,y
                float2 uv2 : TEXCOORD2; // position random z, vehicle id
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varying
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float fogCoord : TEXCOORD2;
                half4 surfaceColor : TEXCOORD5; // smoothness
                half2 surfaceData : TEXCOORD6; // metallic, smoothness

                float3 positionWS : TEXCOORD3;
                float3 normalWS : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;

                half3 _PositionRandom;
                half _AnimationRange;
                half2 _AnimationSpeedRange;
                half _VehicleCount;
                
                half _AmbientIntensity;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;

                half4 _ColorGlass;
                half _MetallicGlass;
                half _SmoothnessGlass;

                half3 _ColorFrontLight;
                half3 _ColorBackLight;
                half _Brightness;

                half3 _ReflectionColor;
                half _ReflectionIntensity;
                half _ReflectionFresnelPow;
                half _roughnessMultiply;
            CBUFFER_END

            Varying vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varying OUT = (Varying)0;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Vehicle mesh animation
                ANIMATE_VEHICLE_VERTEX_OUTPUT(IN.positionOS.xyz, IN.uv1.xy, IN.uv2.xy, _PositionRandom, _AnimationRange, _AnimationSpeedRange.xy, _VehicleCount, UNITY_MATRIX_M._14_24_34, IN.positionOS.xyz);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = vertexInput.positionCS;

                // Data for reflections in fragment shader
                OUT.positionWS = vertexInput.positionWS;
                half3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.normalWS = normalWS;

                // Surface Data
                half isBodyOrBackLight = step(0.5, IN.uv0.x);
                half isLight = step(0.5, IN.uv0.y);

                half3 diffuse = dot(normalWS, half3(0, 1, 0)) * 0.5 + 0.5; // half lambert from top
                half3 ambient = SampleSH(OUT.normalWS) * _AmbientIntensity;
                half3 emission = lerp(_ColorFrontLight, _ColorBackLight, isBodyOrBackLight) * isLight * _Brightness;
                half3 color = lerp(_ColorGlass.rgb, IN.color * _BaseColor.rgb, isBodyOrBackLight);
                color *= diffuse;
                color += ambient + emission;
                OUT.surfaceColor = half4(color, 1);

                half metallic = lerp(_MetallicGlass, _Metallic, saturate(isBodyOrBackLight + isLight));
                half smoothness = lerp(_SmoothnessGlass, _Smoothness, isBodyOrBackLight);

                OUT.surfaceData = half2(metallic, smoothness);

                OUT.uv0 = TRANSFORM_TEX(IN.uv0, _BaseMap);

                // Fog
                OUT.fogCoord = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varying IN) : SV_Target
            {
                half4 color = IN.surfaceColor;
                color *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv0);
                half metallic = IN.surfaceData.r;
                half smoothness = IN.surfaceData.g;

                // Surface Data
                float3 positionWS = IN.positionWS;
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                half3 normalWS = normalize(IN.normalWS);

                // Fresnel
                half nv = dot(viewDirWS, normalWS);
                half fresnel = saturate(1 - nv);
                fresnel = pow(fresnel, _ReflectionFresnelPow);

                // Reflection
                float3 worldRefl = reflect(-viewDirWS, normalWS);
                half4 skyData = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, worldRefl, (1 - smoothness) * _roughnessMultiply);
                half3 skyColor = DecodeHDREnvironment(skyData, unity_SpecCube0_HDR);
                skyColor = skyColor * _ReflectionColor.rgb * _ReflectionIntensity;
                color.rgb = lerp(skyColor * fresnel + color.rgb, skyColor * color.rgb, metallic);

                // Fog
                color.rgb = MixFog(color.rgb, IN.fogCoord);

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "./BGVehiclesMeshAnimator.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half3 color : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1; // position random x,y
                float2 uv2 : TEXCOORD2; // position random z, vehicle id
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            // SurfaceInput.hlsl contains this
            // TEXTURE2D(_BaseMap);
            // SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                
                half3 _PositionRandom;
                half _AnimationRange;
                half2 _AnimationSpeedRange;
                half _VehicleCount;
                
                half _AmbientIntensity;
                half4 _BaseColor;
                half _Metallic;
                half _Smoothness;

                half4 _ColorGlass;
                half _MetallicGlass;
                half _SmoothnessGlass;

                half3 _ColorFrontLight;
                half3 _ColorBackLight;
                half _Brightness;

                half3 _ReflectionColor;
                half _ReflectionIntensity;
                half _ReflectionFresnelPow;
                half _roughnessMultiply;
            CBUFFER_END

            // Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
            // For Directional lights, _LightDirection is used when applying shadow Normal Bias.
            // For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
            float3 _LightDirection;
            float3 _LightPosition;

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                // Vehicle mesh animation
                ANIMATE_VEHICLE_VERTEX_OUTPUT(input.positionOS.xyz, input.uv1.xy, input.uv2.xy, _PositionRandom, _AnimationRange, _AnimationSpeedRange.xy, _VehicleCount, UNITY_MATRIX_M._14_24_34, input.positionOS.xyz);

                output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, 0.5);
                return 0;
            }
            ENDHLSL
        }
    }
}
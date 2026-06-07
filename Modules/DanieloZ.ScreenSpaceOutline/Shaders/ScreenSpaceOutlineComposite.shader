Shader "Outline/ScreenSpaceOutlineComposite"
{
    Properties
    {
        [Header(Visible Outline)]
        _VisibleOutlineColor ("Visible Outline Color", Color) = (1, 1, 1, 1)
        _VisibleOutlineAlpha ("Visible Outline Alpha", Range(0, 1)) = 1
        _VisibleOutlineThicknessPixels ("Visible Outline Thickness Pixels", Range(1, 16)) = 4
        _EnableVisibleOutline ("Enable Visible Outline", Range(0, 1)) = 1

        [Header(Occluded Outline)]
        _OccludedOutlineColor ("Occluded Outline Color", Color) = (1, 0, 0, 1)
        _OccludedOutlineAlpha ("Occluded Outline Alpha", Range(0, 1)) = 1
        _OccludedOutlineThicknessPixels ("Occluded Outline Thickness Pixels", Range(1, 16)) = 4
        _EnableOccludedOutline ("Enable Occluded Outline", Range(0, 1)) = 1

        [Header(Occluded Silhouette)]
        _OccludedSilhouetteColor ("Occluded Silhouette Color", Color) = (0, 0.5, 1, 1)
        _OccludedSilhouetteTextureMix ("Occluded Silhouette Color / Object Visual", Range(0, 1)) = 0
        _OccludedSilhouetteAlpha ("Occluded Silhouette Overlay Alpha", Range(0, 1)) = 0.35
        _EnableOccludedSilhouette ("Enable Occluded Silhouette", Range(0, 1)) = 1

        [Header(Distance Scaling)]
        _UseDistanceThicknessScale ("Use Distance Thickness Scale", Range(0, 1)) = 0
        _DistanceThicknessMultiplier ("Distance Thickness Multiplier", Range(0.05, 2)) = 1

        [Header(Debug)]
        _DebugView ("Debug View: 0 Off, 1 Visible, 2 Raw Occluded, 3 Final Occluded", Range(0, 3)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Overlay"
        }

        // Pass 0: Visible Outline
        Pass
        {
            Name "VisibleOutlineComposite"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragVisible

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _VisibleOutlineColor;
            float _VisibleOutlineAlpha;
            float _VisibleOutlineThicknessPixels;
            float _EnableVisibleOutline;

            float _UseDistanceThicknessScale;
            float _DistanceThicknessMultiplier;

            float _DebugView;

            float SampleBlitMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).r;
            }

            float GetDilatedBlitMask(float2 uv, float thicknessPixels)
            {
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float result = 0.0;

                int thickness = (int)thicknessPixels;

                for (int x = -16; x <= 16; x++)
                {
                    for (int y = -16; y <= 16; y++)
                    {
                        if (abs(x) > thickness || abs(y) > thickness)
                            continue;

                        float2 offset = float2(x, y) * texelSize;
                        result = max(result, SampleBlitMask(uv + offset));
                    }
                }

                return result;
            }

            half4 FragVisible(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                float visibleMask = SampleBlitMask(uv);

                if (_DebugView > 0.5 && _DebugView < 1.5)
                {
                    return half4(1, 0, 0, visibleMask);
                }

                if (_EnableVisibleOutline < 0.5)
                {
                    return half4(0, 0, 0, 0);
                }

                float visibleThickness = _VisibleOutlineThicknessPixels;

                if (_UseDistanceThicknessScale > 0.5)
                {
                    visibleThickness *= _DistanceThicknessMultiplier;
                }

                visibleThickness = max(1.0, visibleThickness);

                float dilatedVisible = GetDilatedBlitMask(uv, visibleThickness);
                float visibleOutline = saturate(dilatedVisible - visibleMask);

                float alpha = visibleOutline * _VisibleOutlineAlpha * _VisibleOutlineColor.a;

                return half4(_VisibleOutlineColor.rgb, alpha);
            }

            ENDHLSL
        }

        // Pass 1: Occluded Silhouette + Occluded Outline
        Pass
        {
            Name "OccludedComposite"

            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragOccluded

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_VisibleOutlineMaskTexture);
            SAMPLER(sampler_VisibleOutlineMaskTexture);

            TEXTURE2D_X(_OccludedObjectVisualTexture);
            SAMPLER(sampler_OccludedObjectVisualTexture);

            float4 _OccludedOutlineColor;
            float _OccludedOutlineAlpha;
            float _OccludedOutlineThicknessPixels;
            float _EnableOccludedOutline;

            float4 _OccludedSilhouetteColor;
            float _OccludedSilhouetteTextureMix;
            float _OccludedSilhouetteAlpha;
            float _EnableOccludedSilhouette;

            float _UseDistanceThicknessScale;
            float _DistanceThicknessMultiplier;

            float _DebugView;

            float SampleOccludedRaw(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).r;
            }

            float SampleVisibleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(
                    _VisibleOutlineMaskTexture,
                    sampler_VisibleOutlineMaskTexture,
                    uv
                ).r;
            }

            float SampleFinalOccludedMask(float2 uv)
            {
                float rawOccluded = SampleOccludedRaw(uv);
                float visible = SampleVisibleMask(uv);

                return saturate(rawOccluded - visible);
            }

            float GetDilatedFinalOccludedMask(float2 uv, float thicknessPixels)
            {
                float2 texelSize = 1.0 / _ScreenParams.xy;
                float result = 0.0;

                int thickness = (int)thicknessPixels;

                for (int x = -16; x <= 16; x++)
                {
                    for (int y = -16; y <= 16; y++)
                    {
                        if (abs(x) > thickness || abs(y) > thickness)
                            continue;

                        float2 offset = float2(x, y) * texelSize;
                        result = max(result, SampleFinalOccludedMask(uv + offset));
                    }
                }

                return result;
            }

            half4 FragOccluded(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                float rawOccluded = SampleOccludedRaw(uv);
                float visible = SampleVisibleMask(uv);
                float finalOccluded = saturate(rawOccluded - visible);

                if (_DebugView > 1.5 && _DebugView < 2.5)
                {
                    return half4(0, 0.4, 1, rawOccluded);
                }

                if (_DebugView > 2.5)
                {
                    return half4(0, 1, 1, finalOccluded);
                }

                float occludedThickness = _OccludedOutlineThicknessPixels;

                if (_UseDistanceThicknessScale > 0.5)
                {
                    occludedThickness *= _DistanceThicknessMultiplier;
                }

                occludedThickness = max(1.0, occludedThickness);

                float dilatedOccluded = GetDilatedFinalOccludedMask(
                    uv,
                    occludedThickness
                );

                float occludedOutline = saturate(dilatedOccluded - finalOccluded);

                // Не даём скрытому outline залезать на видимую часть объекта.
                occludedOutline *= saturate(1.0 - visible);

                float silhouetteAlpha =
                    finalOccluded *
                    _OccludedSilhouetteAlpha *
                    step(0.5, _EnableOccludedSilhouette);

                float outlineAlpha =
                    occludedOutline *
                    _OccludedOutlineAlpha *
                    _OccludedOutlineColor.a *
                    step(0.5, _EnableOccludedOutline);

                float finalAlpha = max(silhouetteAlpha, outlineAlpha);

                float outlineWeight = step(0.001, outlineAlpha);

                float3 silhouetteColor = _OccludedSilhouetteColor.rgb;

                if (_OccludedSilhouetteTextureMix > 0.001)
                {
                    float3 objectVisual = SAMPLE_TEXTURE2D_X(
                        _OccludedObjectVisualTexture,
                        sampler_OccludedObjectVisualTexture,
                        uv
                    ).rgb;

                    silhouetteColor = lerp(
                        silhouetteColor,
                        objectVisual,
                        _OccludedSilhouetteTextureMix
                    );
                }

                float3 finalColor = lerp(
                    silhouetteColor,
                    _OccludedOutlineColor.rgb,
                    outlineWeight
                );

                return half4(finalColor, finalAlpha);
            }

            ENDHLSL
        }
    }
}

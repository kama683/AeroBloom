Shader "AeroBloom/SoftCloud"
{
    Properties
    {
        [Header(Base)]
        _BaseColor ("Tint", Color) = (0.94, 0.97, 1, 0.45)
        _Emission ("Emission", Color) = (0.35, 0.55, 0.85, 0.15)

        [Header(Softness)]
        _CenterAlpha ("Center Alpha", Range(0, 1)) = 0.38
        _EdgeAlpha ("Edge Alpha Boost", Range(0, 2)) = 0.85
        _FresnelPower ("Fresnel Power", Range(0.5, 6)) = 2.4
        _Softness ("Extra Softness", Range(0.1, 3)) = 1.35

    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float4 positionWS : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Emission;
                float  _CenterAlpha;
                float  _EdgeAlpha;
                float  _FresnelPower;
                float  _Softness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = float4(posInputs.positionWS, 1);
                OUT.normalWS     = normalize(nrmInputs.normalWS);
                OUT.viewDirWS    = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.fogFactor    = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(IN.viewDirWS);
                float  ndv = saturate(dot(n, v));

                // Softer in center, slightly brighter at grazing angles (cloud rim)
                float fresnel = pow(1.0 - ndv, _FresnelPower);
                float alpha = saturate(_CenterAlpha + fresnel * _EdgeAlpha * _Softness);
                alpha *= _BaseColor.a;

                half3 col = _BaseColor.rgb + _Emission.rgb * fresnel;
                col = MixFog(col, IN.fogFactor);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}

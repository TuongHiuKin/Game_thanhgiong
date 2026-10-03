Shader "ThanhGiong/Golden Noise Dissolve"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Dissolve("Dissolve", Range(0,1)) = 0
        _EdgeWidth("Golden Edge Width", Range(.01,.3)) = .1
        [HDR]_EdgeColor("Golden Edge", Color) = (1,.55,.05,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="AlphaTest" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float3 positionWS:TEXCOORD1; };
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor, _EdgeColor;
            float _Dissolve, _EdgeWidth;
            CBUFFER_END
            float hash31(float3 p) { p=frac(p*.1031); p+=dot(p,p.yzx+33.33); return frac((p.x+p.y)*p.z); }
            float noise3(float3 p)
            {
                float3 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash31(i),hash31(i+float3(1,0,0)),f.x),lerp(hash31(i+float3(0,1,0)),hash31(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash31(i+float3(0,0,1)),hash31(i+float3(1,0,1)),f.x),lerp(hash31(i+float3(0,1,1)),hash31(i+1),f.x),f.y),f.z);
            }
            Varyings vert(Attributes v){Varyings o;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);o.positionHCS=TransformWorldToHClip(o.positionWS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);return o;}
            half4 frag(Varyings i):SV_Target
            {
                float n=noise3(i.positionWS*3.4+float3(0,_Time.y*.18,0));
                float threshold=_Dissolve*1.18-.09;
                clip(n-threshold);
                half4 baseCol=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
                float edge=1-smoothstep(0,_EdgeWidth,n-threshold);
                return half4(baseCol.rgb+_EdgeColor.rgb*edge*2.4,1);
            }
            ENDHLSL
        }
    }
}

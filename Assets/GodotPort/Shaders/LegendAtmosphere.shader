Shader "ThanhGiong/LegendAtmosphere" {
 Properties { _BaseColor("Color",Color)=(1,.8,.4,.2) _Shape("0 glow 1 ripple 2 recognition disc 3 painted sun ray",Float)=0 _Phase("Phase",Float)=0 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Blend SrcAlpha One
  ZWrite Off
  Cull Off
  Pass {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor; float _Shape,_Phase;
   CBUFFER_END
   struct Input { float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
   struct Output { float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float fog:TEXCOORD1; };
   Output vert(Input v){Output o;o.position=TransformObjectToHClip(v.vertex.xyz);o.uv=v.uv;o.color=v.color;o.fog=ComputeFogFactor(o.position.z);return o;}
   half4 frag(Output i):SV_Target {
    float radius=length((i.uv-.5)*2);float alpha=pow(saturate(1-radius),2);
    if(_Shape>.5&&_Shape<1.5)alpha=(1-smoothstep(.025,.065,abs(radius-(.18+_Phase*.69))))*saturate(1-_Phase)*smoothstep(0,.15,_Phase);
    if(_Shape>1.5&&_Shape<2.5)alpha=pow(saturate(1-radius),2.4)*(.85+sin(_Phase)*.08);
    if(_Shape>2.5){
     float width=lerp(.96,.36,i.uv.y);
     float sides=pow(saturate(1-abs((i.uv.x-.5)*2)/width),1.7);
     float ends=pow(saturate(sin(i.uv.y*3.14159265)),.7);
     alpha=sides*ends*(.88+sin(_Phase)*.12);
    }
    half4 color=_BaseColor*i.color;return half4(MixFog(color.rgb,i.fog),color.a*alpha);
   }
   ENDHLSL
  }
 }
}

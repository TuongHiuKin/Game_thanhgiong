Shader "ThanhGiong/LegendSurface" {
 Properties {
  _BaseColor("Color", Color)=(1,1,1,1)
  _BaseMap("Texture",2D)="white"{}
  _Mode("0 plain 1 terrain 2 road 3 foliage 4 cloth 5 water",Float)=0
  _Amplitude("Motion amplitude",Float)=0
  _Height("Bend height",Float)=1
  _MotionTime("Pause aware clock",Float)=0
  _Clearing("Clearing",Float)=0
  _UseVertexColors("Vertex colors",Float)=0
  _Visibility("Foreground dither visibility",Range(0,1))=1
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Cull Off
  Pass {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor, _BaseMap_ST;
   float _Mode,_Amplitude,_Height,_MotionTime,_Clearing,_UseVertexColors,_Visibility;
   CBUFFER_END
   struct Input {float4 position:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};
   struct Output {float4 position:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float4 color:COLOR;float fog:TEXCOORD3;};
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   Output vert(Input v){
    Output o;float3 p=v.position.xyz;float3 w=TransformObjectToWorld(p);
    if((_Mode>2.5&&_Mode<4.5)||_Mode>5.5){float bend=saturate(p.y/max(_Height,.01));if(_Mode>3.5&&_Mode<4.5)bend=saturate(abs(p.y)/max(_Height,.01));p.x+=sin(_MotionTime*1.8+w.x*.31+w.z*.17)*_Amplitude*bend; p.z+=sin(_MotionTime*2.7+w.z*.22)*_Amplitude*bend*.35;}
    o.world=TransformObjectToWorld(p);o.position=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(v.normal);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.color=v.color;o.fog=ComputeFogFactor(o.position.z);return o;
   }
   half4 frag(Output i):SV_Target {
    clip(_Visibility-hash(floor(i.position.xy)));
    half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
    if(_Mode<.5||(_Mode>2.5&&_Mode<5.5))color*=lerp(half4(1,1,1,1),i.color,_UseVertexColors);
    if(_Mode>5.5&&_Mode<6.5)color.rgb=lerp(color.rgb,color.rgb*.68,saturate(i.color.r));
    if(_Mode>6.5)color.rgb=lerp(color.rgb,float3(.44,.56,.32),saturate(i.uv.x));
    if(_Mode>2.5&&_Mode<3.5){float green=saturate((color.g-max(color.r,color.b)) * 5);float l=dot(color.rgb,float3(.23,.57,.20));color.rgb=lerp(color.rgb,float3(.43,.55,.32)*(.65+l*.75),green*.7);}
    if(_Mode>.5&&_Mode<1.5){float n=noise(i.world.xz*.095)*.7+noise(i.world.xz*.24)*.3;color.rgb=lerp(color.rgb,float3(.43,.46,.31),smoothstep(.30,.83,n)*.52);color.rgb*=.94+noise(i.world.xz*2.6)*.10+hash(floor(i.world.xz*12))*.025;}
    if(_Mode>1.5&&_Mode<2.5){float edge=_Clearing>.5?length((i.uv-.5)*2):abs(i.uv.x*2-1);clip(1-smoothstep(.70,1,edge+noise(i.world.xz*1.3)*.12)-.16);color.rgb*=.86+noise(i.world.xz*.42)*.23;}
    if(_Mode>4.5&&_Mode<5.5){float wave=sin(i.world.x*1.7+_MotionTime*1.1)*sin(i.world.z*2.1-_MotionTime*.9);color.rgb=lerp(color.rgb,float3(.58,.68,.66),smoothstep(.65,.97,wave)*.25);}
    float paint=noise(i.world.xz*1.6+i.world.y*.43);color.rgb*=.975+paint*.045;
    Light light=GetMainLight(TransformWorldToShadowCoord(i.world));float diffuse=saturate(dot(normalize(i.normal),light.direction));
    diffuse=lerp(diffuse,floor(diffuse*5+.5)/5,.12);
    half3 lit=color.rgb*(SampleSH(normalize(i.normal))+.32+light.color*diffuse*lerp(.64,1,light.shadowAttenuation)*.55);
    return half4(MixFog(lit,i.fog),1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}

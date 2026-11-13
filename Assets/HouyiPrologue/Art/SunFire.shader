Shader "Houyi/SunFire"
{
    Properties { _Color ("Core", Color) = (1,0.8,0.3,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 position:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; };
            float4 _Color;
            v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float center=saturate(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world)));
                float wave=sin(i.world.x*3+i.world.y*4+_Time.y)*sin(i.world.y*5-_Time.y*0.4)*0.035;
                float3 color=lerp(float3(1,0.17,0.015),float3(1,0.94,0.55),saturate(pow(center,0.65)+wave));
                return float4(color*_Color.rgb,1);
            }
            ENDCG
        }
    }
}

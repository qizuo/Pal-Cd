Shader "Houyi/DustSky"
{
    Properties { _Horizon ("Horizon", Color) = (0.72,0.43,0.20,1) _Zenith ("Zenith", Color) = (0.16,0.20,0.22,1) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 position:SV_POSITION; float3 direction:TEXCOORD0; };
            float4 _Horizon, _Zenith;
            v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float h=saturate(normalize(i.direction).y);
                float3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,0.55));
                return float4(color,1);
            }
            ENDCG
        }
    }
}

Shader "Houyi/SunHalo"
{
    Properties { _Color ("Glow", Color) = (1,0.35,0.05,0.4) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 position:SV_POSITION; float2 uv:TEXCOORD0; };
            float4 _Color;
            v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o; }
            fixed4 frag(v2f i):SV_Target
            { float r=length(i.uv-0.5)*2;float alpha=pow(saturate(1-r),2)*_Color.a;return float4(_Color.rgb,alpha); }
            ENDCG
        }
    }
}

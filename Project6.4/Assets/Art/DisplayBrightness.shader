Shader "Hidden/Demo/DisplayBrightness"
{
    Properties { _MainTex("Source",2D)="white"{} }
    SubShader { Cull Off ZWrite Off ZTest Always Pass {
        CGPROGRAM
        #pragma vertex vert_img
        #pragma fragment frag
        #include "UnityCG.cginc"
        sampler2D _MainTex;float _Gain;
        fixed4 frag(v2f_img i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv);c.rgb*=_Gain;return c;}
        ENDCG
    } }
}

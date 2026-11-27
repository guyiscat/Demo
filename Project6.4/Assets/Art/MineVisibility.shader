Shader "Hidden/Demo/MineVisibility"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Player, _ViewSize, _Mouse;
            float _Radius, _Feather, _MouseRadius, _MouseFeather;
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col=tex2D(_MainTex,i.uv);
                float d=length((i.uv-_Player.xy)*_ViewSize.xy);
                float light=1-smoothstep(_Radius-_Feather,_Radius,d);
                float mouseDistance=length((i.uv-_Mouse.xy)*_ViewSize.xy);
                if(_MouseRadius>0)light=max(light,1-smoothstep(max(0,_MouseRadius-_MouseFeather),_MouseRadius,mouseDistance));
                col.rgb*=light;
                return col;
            }
            ENDCG
        }
    }
}

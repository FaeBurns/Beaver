Shader "Hidden/FakeWorkPP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            int _LoopCount;

            struct Appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct V2F
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            V2F vert (Appdata v)
            {
                V2F o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (V2F i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 col_bak = col;

                for(int n = 0; n < _LoopCount; n++)
                {
                    col = fixed4(sqrt(col_bak.r), sqrt(col_bak.g), sqrt(col_bak.b), sqrt(col_bak.a));
                }

                return col;
            }
            ENDCG
        }
    }
}

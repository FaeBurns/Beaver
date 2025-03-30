Shader "Hidden/BlitEyePortion"
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

            V2F vert(Appdata v)
            {
                V2F o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            int _eyeIndex;

            fixed4 frag(V2F i) : SV_Target
            {
                // texture output from ffmpeg is vertically inverted so un-invert here
                i.uv.y = 1 - i.uv.y;
                if (_eyeIndex == 0) // left eye
                {
                    i.uv.x /= 2; // only want to read the first half
                    fixed4 col = tex2D(_MainTex, i.uv);
                    return col;
                }
                else // right eye
                {
                    i.uv.x /= 2; // only want to half
                    i.uv.x += 0.5; // offset to second half
                    fixed4 col = tex2D(_MainTex, i.uv);
                    return col;
                }
            }
            ENDCG
        }
    }
}
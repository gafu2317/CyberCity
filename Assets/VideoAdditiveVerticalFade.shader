Shader "UI/VideoAdditiveVerticalFade"
{
     Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One // ���Z����
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata_t { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // �������S�ɏ���
                if(col.r < 0.02 && col.g < 0.02 && col.b < 0.02)
                    col.rgb = 0;

                // ��ɍs���قǐF������������i���Z�����ł��t�F�[�h�Ƃ��Č�����j
                float fade = 1.0 - i.uv.y; 
                col.rgb *= fade;

                
               

                return col;
            }
            ENDCG
        }
    }
}

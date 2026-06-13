Shader "Custom/WhiteFlash"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader 
    {
        // 1. Tags de Transparência (Essencial para Sprites)
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        
        // 2. Instruções de Mistura (Alpha Blending)
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass 
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            
            v2f vert(appdata_base v) 
            { 
                v2f o; 
                o.pos = UnityObjectToClipPos(v.vertex); 
                o.uv = v.texcoord; 
                return o; 
            }

            fixed4 frag(v2f i) : SV_Target 
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // Retorna branco (1,1,1) mas preserva a transparência original (col.a)
                return fixed4(1, 1, 1, col.a); 
            }
            ENDCG
        }
    }
}
Shader "Custom/StarOutlineShader"
{
    Properties
    {
        // Proprietà di default richieste da Unity per gli Sprite
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Le nostre variabili custom per il contorno
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0, 5)) = 1
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane" 
            "CanUseSpriteAtlas"="True" 
        }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Questa è la stringa magica per mantenere le massime performance in WebGL!
            #pragma multi_compile_instancing 
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR; // Questo è il colore che passi da C#
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity inietta in automatico la dimensione del pixel
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineThickness;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

           fixed4 frag(v2f IN) : SV_Target
            {
                // Campiona il pixel corrente
                fixed4 c = tex2D(_MainTex, IN.texcoord);
                //fixed4 finalColor = c * IN.color;
                fixed4 finalColor = fixed4(IN.color.rgb, c.a * IN.color.a);

                // Se siamo nella parte trasparente, calcoliamo l'outline
                if (c.a < 0.05)
                {
                    float t = _OutlineThickness;
                    float2 uv = IN.texcoord;
                    float2 texSize = _MainTex_TexelSize.xy;

                    float alpha = 0;
                    
                    // Controllo a croce (Su, Giù, Destra, Sinistra)
                    alpha = max(alpha, tex2D(_MainTex, uv + float2(0, texSize.y * t)).a);
                    alpha = max(alpha, tex2D(_MainTex, uv - float2(0, texSize.y * t)).a);
                    alpha = max(alpha, tex2D(_MainTex, uv + float2(texSize.x * t, 0)).a);
                    alpha = max(alpha, tex2D(_MainTex, uv - float2(texSize.x * t, 0)).a);
                    
                    // Controllo diagonali per perfezionare le curve circolari
                    alpha = max(alpha, tex2D(_MainTex, uv + float2(texSize.x * t, texSize.y * t)).a);
                    alpha = max(alpha, tex2D(_MainTex, uv + float2(-texSize.x * t, texSize.y * t)).a);
                    alpha = max(alpha, tex2D(_MainTex, uv + float2(texSize.x * t, -texSize.y * t)).a);
                    alpha = max(alpha, tex2D(_MainTex, uv + float2(-texSize.x * t, -texSize.y * t)).a);

                    // Se almeno un pixel vicino (anche in diagonale) fa parte della stella, disegna il bordo
                    if (alpha > 0.05)
                    {
                        return _OutlineColor;
                    }
                }

                return finalColor;
            }
            ENDCG
        }
    }
}
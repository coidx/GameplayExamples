Shader "Custom/WaterFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0


        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _BorderColor ("Border Color", Color) = (1, 1, 1, 1)
        _WaveColor ("Wave Color", Color) = (1, 1, 1, 1)
        _CoreThickness ("Core Thickness", Range(0, 1)) = 0.2
        _WaveThickness ("Wave Thickness", Range(0, 1)) = 0.024
        _Cutoff ("Cutoff", Range(0, 1)) = 0.2
        [Toggle]
        _IsFalling ("Is Falling", Range(0, 1)) = 1
        _MaskTex ("Mask Tex", 2D) = "white" {}
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #include "UnitySprites.cginc"

            struct input
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct output
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            float4 _CoreColor;
            float4 _BorderColor;
            float4 _WaveColor;
            float _CoreThickness;
            float _WaveThickness;
            sampler2D _MaskTex;
            float _Cutoff;
            float _IsFalling;

            output vert(input IN)
            {
                output OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
                OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(output IN) : SV_Target
            {
                fixed4 maskCol = tex2D(_MaskTex, IN.texcoord);
                const bool isFalling = bool(_IsFalling);
                float a = 0;
                if (isFalling)
                {
                    a = maskCol.r >= _Cutoff ? 0 : step(0.001, maskCol.r);
                }
                else
                {
                    a = maskCol.r <= _Cutoff ? 0 : 1;
                }

                const float4 mainColor = _CoreThickness < IN.color.g ? _CoreColor : _BorderColor;
                fixed4 c = mainColor;

                const float3 waveColor = _WaveColor.rgb;
                if (isFalling)
                {
                    c.rgb = bool((_Cutoff - maskCol.r) < _WaveThickness) ? waveColor : c.rgb;
                }
                else
                {
                    c.rgb = bool((maskCol.r - _Cutoff) < _WaveThickness) ? waveColor : c.rgb;
                }

                c.a = a;
                c.rgb *= c.a;
                // c.rgb = float3(_Cutoff - maskCol.r,0,0);
                return c;
            }
            ENDCG
        }
    }
}
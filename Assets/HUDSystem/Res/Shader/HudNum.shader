Shader "Unlit/HudNum"
{
        Properties
        {
            _AtlasTex("Texture", 2D) = "white" {}
             AtlasWidth("_AtlasWidth", Float) = 0
            _AtlasHeight("_AtlasHeight", Float) = 0
            _AtlasMappingTex("Texture", 2D) = "white" {}
            _AtlasMappingWidth("_AtlasMappingWidth", Float) = 0
            _AtlasMappingHeight("_AtlasMappingHeight", Float) = 0
        }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest Off
            Cull Off
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing
                #include "UnityCG.cginc"
                #include "Hud.cginc"

                sampler2D _AtlasTex;
                sampler2D _AtlasMappingTex;

                float _AtlasMappingWidth;
                float _AtlasMappingHeight;
                float _AtlasWidth;
                float _AtlasHeight;
                float _FloatRange; 

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uv1 : TEXCOORD1;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    float4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                UNITY_INSTANCING_BUFFER_START(Props)
                    UNITY_DEFINE_INSTANCED_PROP(float4x4, _Param1)
                    UNITY_DEFINE_INSTANCED_PROP(float4x4, _Param2)
                UNITY_INSTANCING_BUFFER_END(Props)

                float2 spriteAtlasMapping(int index)
                {
                    return getAtlasMapping(index, _AtlasMappingWidth, _AtlasMappingHeight, _AtlasMappingTex);
                }

                v2f vert(appdata v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_TRANSFER_INSTANCE_ID(v, o);
                    float4x4 param1 = UNITY_ACCESS_INSTANCED_PROP(Props, _Param1);
                    float4x4 param2 = UNITY_ACCESS_INSTANCED_PROP(Props, _Param2);
                    int quadIndex = int(v.uv1.x);
                    float2 comPos = float2(param1[2][3], param1[3][3]);
                    float alignment = param2[1][3];
                    float angle = param2[0][3];
                    float2 spriteSize = getSpriteSize(quadIndex, param2);
                    fixed4 color = float2ToColor(param2[2][3], param2[3][3]);
                    int spriteid = getSpriteId(quadIndex, param1);
                    float2 spritePos = getSpritePosition(quadIndex, param1);
                    float2 spriteUVPos = spriteAtlasMapping(spriteid * 2);
                    float2 spriteUVSize = spriteAtlasMapping(spriteid * 2 + 1);
                    v.vertex.xy = (spritePos + v.vertex.xy * spriteSize)/100;
                    rotate2D(v.vertex.xy, angle);
                    v.vertex.xy = v.vertex.xy + comPos;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = (spriteUVPos + v.uv * spriteUVSize) / float2(_AtlasWidth, _AtlasHeight);
                    o.color = color;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_AtlasTex, i.uv);
                    col = col * i.color;
                    return col;
                }
                ENDCG
            }
        }
}

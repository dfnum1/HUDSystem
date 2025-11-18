Shader "Unlit/Hud_Image_TMP"
{
	Properties{
		[HDR] _FaceColor("Face Color", Color) = (1,1,1,1)
		_FaceDilate("Face Dilate", Range(-1,1)) = 0

		[HDR]_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_OutlineWidth("Outline Thickness", Range(0,1)) = 0
		_OutlineSoftness("Outline Softness", Range(0,1)) = 0

		[HDR]_UnderlayColor("Border Color", Color) = (0,0,0,.5)
		_UnderlayOffsetX("Border OffsetX", Range(-1,1)) = 0
		_UnderlayOffsetY("Border OffsetY", Range(-1,1)) = 0
		_UnderlayDilate("Border Dilate", Range(-1,1)) = 0
		_UnderlaySoftness("Border Softness", Range(0,1)) = 0

		_WeightNormal("Weight Normal", float) = 0
		_WeightBold("Weight Bold", float) = .5

		_ShaderFlags("Flags", float) = 0
		_ScaleRatioA("Scale RatioA", float) = 1
		_ScaleRatioB("Scale RatioB", float) = 1
		_ScaleRatioC("Scale RatioC", float) = 1

		_MainTex("FontTex", 2D) = "white" {}
		_TextureWidth("FontWidth", Float) = 0
		_TextureHeight("FontHeight", Float) = 0

		_FontMappingTex("FontMappingTex", 2D) = "white" {}
		_FontMappingWidth("FontMappingWidth", Float) = 0
		_FontMappingHeight("FontMappingHeight", Float) = 0

		_GradientScale("Gradient Scale", float) = 5
		_ScaleX("Scale X", float) = 1
		_ScaleY("Scale Y", float) = 1
		_PerspectiveFilter("Perspective Correction", Range(0, 1)) = 0.875
		_Sharpness("Sharpness", Range(-1,1)) = 0

		_CullMode("Cull Mode", Float) = 0

		// Image 的属性
		_AtlasTex("Texture", 2D) = "white" {}
		_AtlasMappingTex("Texture", 2D) = "white" {}
		_AtlasMappingWidth("_AtlasMappingWidth", Float) = 0
		_AtlasMappingHeight("_AtlasMappingHeight", Float) = 0
		_AtlasWidth("_AtlasWidth", Float) = 0
		_AtlasHeight("_AtlasHeight", Float) = 0
	}

	SubShader{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
		}


		Cull[_CullMode]
		ZWrite Off
		Lighting Off
		Fog { Mode Off }
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex VertShader
			#pragma fragment PixShader
			#pragma multi_compile_instancing
			#pragma shader_feature __ OUTLINE_ON
			#pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"
			#include "TMPro_Properties.cginc"
			#include "Hud.cginc"


			sampler2D _AtlasTex;
			sampler2D _AtlasMappingTex;

			float _AtlasMappingWidth;
			float _AtlasMappingHeight;
			float _AtlasWidth;
			float _AtlasHeight;

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float4x4, _Param1)
				UNITY_DEFINE_INSTANCED_PROP(float4x4, _Param2)
			UNITY_INSTANCING_BUFFER_END(Props)

			struct vertex_t {

				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4	vertex			: POSITION;
				float3	normal			: NORMAL;
				float2	texcoord0		: TEXCOORD0;
				float2  texcoord1       : TEXCOORD1;
			};

			struct pixel_t {
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4	vertex			: SV_POSITION;
				fixed4	faceColor : COLOR;
				fixed4	outlineColor : COLOR1;
				float4	texcoord0		: TEXCOORD0;			// Texture UV, Mask UV
				half4	param			: TEXCOORD1;			// Scale(x), BiasIn(y), BiasOut(z), Bias(w)
				half4	mask			: TEXCOORD2;			// Position in clip space(xy), Softness(zw)
				float4	clipRect		: TEXCOORD3;			// xy,size
				float4	worldPos		: TEXCOORD4;			//
				#if (UNDERLAY_ON | UNDERLAY_INNER)
				float4	texcoord1		: TEXCOORD5;			// Texture UV, alpha, reserved
				half2	underlayParam	: TEXCOORD6;			// Scale(x), Bias(y)
				#endif
			};

			float2 fontAtlasMapping(int index)
			{
				return getAtlasMapping(index, _FontMappingWidth, _FontMappingHeight, _FontMappingTex);
			}

			float2 spriteAtlasMapping(int index)
			{
				return getAtlasMapping(index, _AtlasMappingWidth, _AtlasMappingHeight, _AtlasMappingTex);
			}

			float UnityGetCircleClipping(float2 worldPos, float4 circle)
			{
				float edge = max(circle.w, 0.0001);
				float dist = distance(worldPos, circle.xy);
				return saturate(1.0 - smoothstep(circle.z - edge, circle.z, dist));
			}

			float4x4 localToWorld;

			pixel_t VertShader(vertex_t input)
			{
				pixel_t output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				float4x4 param1 = UNITY_ACCESS_INSTANCED_PROP(Props, _Param1);
				float4x4 param2 = UNITY_ACCESS_INSTANCED_PROP(Props, _Param2);

				float2 tmpOrImgTagZ = toFloat2(param2[1][3]);
				float tmpOrimg = tmpOrImgTagZ.x;

				//img和tmp 公用数据
				fixed4 color = float2ToColor(param2[2][3], param2[3][3]);
				float3 comPos = float3(param1[2][3], param1[3][3],tmpOrImgTagZ.y);

				float2 angleMaskType = toFloat2(param2[0][3]);
				float angle = angleMaskType.x;
				float maskType = int(angleMaskType.y);

				//tmp 需要数据
				float4x4 localToWorld = unity_ObjectToWorld;
				float lossyScaleY = length(localToWorld[1].xyz);
				float padding = param2[3][2];
				float adjustedScale = param2[2][2];

				//img 需要数据
				float amount =0;//param2[3][2];
				float origin =0;//param2[2][2];
				float method = 0;//param2[1][2];
				unpackAmountOriginMethod(param2[1][2], amount, origin, method);
				
				float4 clipRect = float4(0,0,0,0);
				clipRect.xy = toFloat2(param2[2][2]);
				clipRect.zw = toFloat2(param2[3][2]);

				//img和tmp spritePos和spriteSize 通用计算
				int quadIndex = int(input.texcoord1.x);
				int spriteid = getSpriteId(quadIndex, param1);
				float2 spritePos = getSpritePosition(quadIndex, param1);
				float2 spriteSize = getSpriteSize(quadIndex, param2);

				//tmp uv计算
				float2 tmpUVPos = fontAtlasMapping(spriteid * 2) - float2(padding, padding);
				float2 tmpUVSize = fontAtlasMapping(spriteid * 2 + 1) + 2 * float2(padding, padding);
				float2 tmpuv = (tmpUVPos + input.texcoord0 * tmpUVSize) / float2(_TextureWidth, _TextureHeight);

				//img uv计算
				float2 spriteUVPos = spriteAtlasMapping(spriteid * 2);
				float2 spriteUVSize = spriteAtlasMapping(spriteid * 2 + 1);
				float2 fillSpriteUVPos = spriteUVPos + spriteUVSize * (1 - amount) * origin;
				float2 fillSpriteUVSize = spriteUVSize * amount;
				spriteUVPos.x = lerp(spriteUVPos.x, fillSpriteUVPos.x, 1 - method);
				spriteUVPos.y = lerp(spriteUVPos.y, fillSpriteUVPos.y, method);
				spriteUVSize.x = lerp(spriteUVSize.x, fillSpriteUVSize.x, 1 - method);
				spriteUVSize.y = lerp(spriteUVSize.y, fillSpriteUVSize.y, method);
				float2 spriteuv = (spriteUVPos + input.texcoord0 * spriteUVSize) / float2(_AtlasWidth, _AtlasHeight);

				input.texcoord0 = lerp(spriteuv, tmpuv, tmpOrimg);

				float2 vertex_xy = spritePos + input.vertex.xy * spriteSize;
				input.vertex.xy = lerp(vertex_xy/100, vertex_xy, tmpOrimg);
				clipRect =  lerp(clipRect/100, clipRect, tmpOrimg);

				rotate2D(input.vertex.xy, angle);
				input.vertex.xyz = input.vertex.xyz + comPos;


				float boldparam = lossyScaleY * adjustedScale;
				float bold = step(boldparam, 0);
				float4 vert = input.vertex;
				float4 vPosition = UnityObjectToClipPos(vert);

				float2 pixelSize = vPosition.w;
				pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

				float scale = rsqrt(dot(pixelSize, pixelSize));
				scale *= abs(boldparam) * _GradientScale * (_Sharpness + 1);

				if (UNITY_MATRIX_P[3][3] == 0) scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(input.normal.xyz), normalize(WorldSpaceViewDir(vert)))));

				float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
				weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

				float layerScale = scale;

				scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
				float bias = (0.5 - weight) * scale - 0.5;
				float outline = _OutlineWidth * _ScaleRatioA * 0.5 * scale;

				float opacity = color.a;
				#if (UNDERLAY_ON | UNDERLAY_INNER)
				opacity = 1.0;
				#endif

				fixed4 faceColor = fixed4(color.rgb, opacity) * _FaceColor;
				faceColor.rgb *= faceColor.a;

				fixed4 outlineColor = _OutlineColor;
				outlineColor.a *= opacity;
				outlineColor.rgb *= outlineColor.a;
				outlineColor = lerp(faceColor, outlineColor, sqrt(min(1.0, (outline * 2))));

				#if (UNDERLAY_ON | UNDERLAY_INNER)
				layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
				float layerBias = (.5 - weight) * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);

				float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
				float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
				float2 layerOffset = float2(x, y);
				#endif


				// Populate structure for pixel shader
				
				output.vertex = vPosition;
				output.faceColor = faceColor;
				output.outlineColor = outlineColor;
				output.texcoord0 = float4(input.texcoord0.x, input.texcoord0.y, tmpOrimg, tmpOrimg);
				output.param = half4(scale, bias - outline, bias + outline, bias);
				output.worldPos.xy =vert.xy;
				output.worldPos.zw = float2(maskType,maskType);
				output.clipRect = clipRect;

				#if (UNDERLAY_ON || UNDERLAY_INNER)
				output.texcoord1 = float4(input.texcoord0 + layerOffset, color.a, 0);
				output.underlayParam = half2(layerScale, layerBias);
				#endif

				return output;
			}


			// PIXEL SHADER
			fixed4 PixShader(pixel_t input) : SV_Target
			{
				/*UNITY_SETUP_INSTANCE_ID(input);*/
				half4 col;
				if (input.texcoord0.z == 0)
				{
					col = tex2D(_AtlasTex, input.texcoord0.xy);
					col = col * input.faceColor;
					if(abs(input.worldPos.z - 1.0) < 0.01)
					{
						fixed clipFade = saturate((input.clipRect.z-input.clipRect.x)*100);
						col.a *= lerp(1,UnityGet2DClipping(input.worldPos.xy, input.clipRect), clipFade);
						clip(col.a - 0.001);
					}
					else if(abs(input.worldPos.z - 2.0) < 0.01)
					{
						fixed clipFade = saturate((input.clipRect.zx)*100);
						col.a *= lerp(1,UnityGetCircleClipping(input.worldPos.xy, input.clipRect), clipFade);
						clip(col.a - 0.001);
					}
					return col;
				}

				half d = tex2D(_MainTex, input.texcoord0.xy).a * input.param.x;
				col = input.faceColor * saturate(d - input.param.w);

				#ifdef OUTLINE_ON
				col = lerp(input.outlineColor, input.faceColor, saturate(d - input.param.z));
				col *= saturate(d - input.param.y);
				#endif

				#if UNDERLAY_ON
				d = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
				col += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate(d - input.underlayParam.y) * (1 - c.a);
				#endif

				#if UNDERLAY_INNER
				half sd = saturate(d - input.param.z);
				d = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
				col += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * (1 - saturate(d - input.underlayParam.y)) * sd * (1 - c.a);
				#endif

				#if (UNDERLAY_ON | UNDERLAY_INNER)
				col *= input.texcoord1.z;
				#endif

				//fixed4 col = tex2D(_AtlasTex, input.texcoord0.xy);
				//col = col * input.faceColor;

				//c = lerp(col, c, input.texcoord0.z);
		
				if(abs(input.worldPos.z - 1.0) < 0.01)
				{
					fixed clipFade = saturate((input.clipRect.z-input.clipRect.x)*100);
					col.a *= lerp(1,UnityGet2DClipping(input.worldPos.xy, input.clipRect), clipFade);
					clip(col.a - 0.001);
				}
				else if(abs(input.worldPos.z - 2.0) < 0.01)
				{
					fixed clipFade = saturate((input.clipRect.z)*100);
					col.a *= lerp(1,UnityGetCircleClipping(input.worldPos.xy, input.clipRect), clipFade);
					clip(col.a - 0.001);
				}
				return col;
			}
			ENDCG
			}
		}

	CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}

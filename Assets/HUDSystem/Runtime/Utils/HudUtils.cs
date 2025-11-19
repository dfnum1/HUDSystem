/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDSystem
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    public class HUDUtils
    {
        public const int QUAD_COUNT = 9;
        public const float default_unit = 0.01f;
        public static float3 ONE3 = new float3(1, 1, 1);
        public static float2 ONE2 = new float2(1,1);
#if UNITY_2022_1_OR_NEWER
        public static int batchMaxCount = 8191;
#else
    public static int batchMaxCount = 1023;
#endif
        public static int capacity = 512;

        public static int _AtlasTex = Shader.PropertyToID("_AtlasTex");
        public static int _AtlasWidth = Shader.PropertyToID("_AtlasWidth");
        public static int _AtlasHeight = Shader.PropertyToID("_AtlasHeight");
        public static int _AtlasMappingTex = Shader.PropertyToID("_AtlasMappingTex");
        public static int _AtlasMappingWidth = Shader.PropertyToID("_AtlasMappingWidth");
        public static int _AtlasMappingHeight = Shader.PropertyToID("_AtlasMappingHeight");

        public static int _MainTex = Shader.PropertyToID("_MainTex");
        public static int _TextureWidth = Shader.PropertyToID("_TextureWidth");
        public static int _TextureHeight = Shader.PropertyToID("_TextureHeight");
        public static int _FontMappingTex = Shader.PropertyToID("_FontMappingTex");
        public static int _FontMappingWidth = Shader.PropertyToID("_FontMappingWidth");
        public static int _FontMappingHeight = Shader.PropertyToID("_FontMappingHeight");

        private static int ms_nUniqueID = 0;
        //--------------------------------------------------------
        internal static int GetUniqueID()
        {
            return ++ms_nUniqueID;
        }
        //--------------------------------------------------------
        public static NativeArray<T> ArrayExpansion<T>(NativeArray<T> array, int size, Allocator allocator) where T : struct
        {
            int len = array.Length;
            array.Dispose();
            NativeArray<T> newarray = CollectionHelper.CreateNativeArray<T>(size, allocator, NativeArrayOptions.UninitializedMemory);
            return newarray;
        }
        //--------------------------------------------------------
        public static float ToOneFloat(float v1, float v2)
        {
            uint uv1 = math.f32tof16(v1);
            uint uv2 = math.f32tof16(v2);
            uint v = uv1 << 16 | uv2;
            return math.asfloat(v);
        }
        //--------------------------------------------------------
        public static float2 ToTowFloat(float fv)
        {
            uint uv = math.asuint(fv);
            uint uv1 = uv >> 16;
            uint uv2 = uv & 0x0000ffff;
            return new float2(math.f16tof32(uv1), math.f16tof32(uv2));
        }
        //--------------------------------------------------------
        public static float2 ColorToFloat(Color color)
        {
            float2 f2 = new float2();
            f2.x = ToOneFloat(color.r, color.g);
            f2.y = ToOneFloat(color.b, color.a);
            return f2;
        }
    }
}

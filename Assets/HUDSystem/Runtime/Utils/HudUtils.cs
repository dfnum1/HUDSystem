/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDSystem
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    public class HUDUtils
    {
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
        internal static int GetUniqueID()
        {
            return ++ms_nUniqueID;
        }
    }
}

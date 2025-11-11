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
        public static float fDefaultUnit = 0.01f;
        public static float3 ONE3 = new float3(1, 1, 1);
        public static float2 ONE2 = new float2(1,1);

        public const EDirtyFlag ALLDirty = EDirtyFlag.EQuad | EDirtyFlag.ETransform;

        internal static BuildTransformJobData TempJobDatas = new BuildTransformJobData();
        internal static BuildPerQuadData TempQuadData = new BuildPerQuadData();

        private static int ms_nUniqueID = 0;
        internal static int GetUniqueID()
        {
            return ++ms_nUniqueID;
        }
    }
}

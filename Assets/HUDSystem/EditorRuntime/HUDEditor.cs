/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDEditor
作    者:	HappLI
描    述:	HUD 编辑器
*********************************************************************/
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GridBrushBase;

namespace Framework.HUD.Runtime
{
    public class HUDEditor : EditorWindow
    {
        static HUDEditor ms_pInstance = null;
        [MenuItem("Window/HUD Editor")]
        public static void ShowWindow()
        {
            if (ms_pInstance == null)
                ms_pInstance = GetWindow<HUDEditor>("HUD Editor");
            ms_pInstance.Focus();
        }
        //--------------------------------------------------------
        private void OnGUI()
        {
        }
    }
}
#endif
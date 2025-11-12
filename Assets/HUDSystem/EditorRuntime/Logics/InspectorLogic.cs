/********************************************************************
生成日期:	11:11:2025
类    名: 	InspectorLogic
作    者:	HappLI
描    述:	HUD图元数据编辑逻辑
*********************************************************************/
#if UNITY_EDITOR
using UnityEngine;

namespace Framework.HUD.Editor
{
    public class InspectorLogic : AEditorLogic
    {
        public InspectorLogic(HUDEditor editor, Rect viewRect) : base(editor, viewRect)
        {
        }
    }
}
#endif
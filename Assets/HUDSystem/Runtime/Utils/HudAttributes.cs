/********************************************************************
生成日期:	11:11:2025
类    名: 	HudIconAttributes
作    者:	HappLI
描    述:	HUD 属性标签
*********************************************************************/

using System;

namespace Framework.HUD.Runtime
{
    public class HudIconAttribute : System.Attribute
    {
#if UNITY_EDITOR
        public string icon;
#endif
        public HudIconAttribute(string iconPath = null)
        {
#if UNITY_EDITOR
            icon = iconPath;
#endif
        }
    }
    //--------------------------------------------------------
    public class HudDataAttribute : System.Attribute
    {
#if UNITY_EDITOR
        public System.Type dataType;
#endif
        public HudDataAttribute(System.Type dataType)
        {
#if UNITY_EDITOR
            this.dataType = dataType;
#endif
        }
    }
    //--------------------------------------------------------
    public class HudFieldAttribute : System.Attribute
    {
#if UNITY_EDITOR
        public string fieldName;
#endif
        public HudFieldAttribute(string fieldName)
        {
#if UNITY_EDITOR
            this.fieldName = fieldName;
#endif
        }
    }
}

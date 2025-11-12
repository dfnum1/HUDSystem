/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDEditor
作    者:	HappLI
描    述:	HUD 编辑器
*********************************************************************/
#if UNITY_EDITOR
using UnityEngine;

namespace Framework.HUD.Editor
{
    public class AEditorLogic
    {
        public Rect viewRect;
        protected HUDEditor m_pEditor;
        //--------------------------------------------------------
        public AEditorLogic(HUDEditor editor, Rect viewRect)
        {
            m_pEditor = editor;
            this.viewRect = viewRect;
        }
        //--------------------------------------------------------
        public virtual void OnEnable()
        {

        }
        //--------------------------------------------------------
        public virtual void OnDisable() 
        {
        }
        //--------------------------------------------------------
        public virtual void OnDestroy()
        {

        }
        //--------------------------------------------------------
        public virtual void OnEvent(Event evt)
        {

        }
        //--------------------------------------------------------
        public virtual void OnGUI()
        {
        }
    }
}
#endif
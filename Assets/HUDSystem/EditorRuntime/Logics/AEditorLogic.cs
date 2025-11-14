/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDEditor
作    者:	HappLI
描    述:	HUD 编辑器
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using UnityEngine;
using static Codice.Client.Common.Connection.AskCredentialsToUser;

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
        public HudSystem GetHudSystem()
        {
            return m_pEditor.GetHudSystem();
        }
        //--------------------------------------------------------
        public HudObject GetHudObject()
        {
            return m_pEditor.GetHUDObject();
        }
        //--------------------------------------------------------
        public HudController GetHud()
        {
            return m_pEditor.GetHud();
        }
        //--------------------------------------------------------
        public virtual void OnSetHudObject(HudObject hudObject)
        {
        }
        //--------------------------------------------------------
        internal virtual void OnSelectComponent(AComponent component)
        {
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
        public void DrawGUI()
        {
            GUILayout.BeginArea(viewRect);
            OnGUI();
            GUILayout.EndArea();
        }
        //--------------------------------------------------------
        protected virtual void OnGUI()
        {
        }
        //--------------------------------------------------------
        public virtual void OnSave() 
        {
        }
    }
}
#endif
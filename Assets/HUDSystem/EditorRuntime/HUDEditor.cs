/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDEditor
作    者:	HappLI
描    述:	HUD 编辑器
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

namespace Framework.HUD.Editor
{
    public class HUDEditor : EditorWindow
    {
        //--------------------------------------------------------
        public static void EditorHud(HudObject pHudObject)
        {
            HUDEditor hudEditor = GetWindow<HUDEditor>("HUD Editor");
            hudEditor.SetHudObject(pHudObject);
            hudEditor.Focus();
        }
        HudSystem m_pHudSystem = null;
        HudObject m_pHudObject = null;
        HudController m_pHudController = null;
        List<AEditorLogic> m_vLogics = new List<AEditorLogic>(3);
        //--------------------------------------------------------
        public HudSystem GetHudSystem()
        {
            if (m_pHudSystem == null) m_pHudSystem = new HudSystem();
            return m_pHudSystem;
        }
        //--------------------------------------------------------
        public HudController GetHud()
        {
            if (m_pHudController == null) m_pHudController = new HudController(GetHudSystem());
            m_pHudController.SetEditorMode(true);
            return m_pHudController;
        }
        //--------------------------------------------------------
        void OnDestroy()
        {
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnDestroy();
            }
            m_vLogics.Clear();
        }
        //--------------------------------------------------------
        void OnEnable()
        {
            EditorApplication.update += OnUpdate;
            this.minSize = new Vector2(1280,600);
            m_pHudSystem = new HudSystem();
            if (m_vLogics.Count<=0)
            {
                m_vLogics.Add(new HierarchyLogic(this,new Rect(0,0,300,position.height)));
                m_vLogics.Add(new PreviewLogic(this, new Rect(300, 0, position.width-600, position.height)));
                m_vLogics.Add(new InspectorLogic(this, new Rect(position.width-300, 0, 300, position.height)));
            }
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnEnable();
            }
            SetHudObject(m_pHudObject);
        }
        //--------------------------------------------------------
        void SetHudObject(HudObject hudObject)
        {
            if (m_pHudObject == hudObject)
                return;
            m_pHudObject = hudObject;
            hudObject.EditInit();
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnSetHudObject(m_pHudObject);
            }
        }
        //--------------------------------------------------------
        private void OnDisable()
        {
            m_pHudSystem.Destroy();
            m_pHudSystem = null;
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnDisable();
            }
            if (m_pHudController != null) m_pHudController.Destroy();
            m_pHudController = null;
        }
        //--------------------------------------------------------
        void OnEvent(Event evt)
        {
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnEvent(evt);
            }
        }
        //--------------------------------------------------------
        public HudObject GetHUDObject()
        {
            return m_pHudObject;
        }
        //--------------------------------------------------------
        private void OnUpdate()
        {
            if(!Application.isPlaying)
                JobHandle.ScheduleBatchedJobs();

            this.Repaint();
        }
        //--------------------------------------------------------
        private void OnGUI()
        {
            m_vLogics[0].viewRect = new Rect(0, 25, 300, position.height-25);
            m_vLogics[1].viewRect = new Rect(300, 25, position.width - 600, position.height - 25);
            m_vLogics[2].viewRect = new Rect(position.width - 300, 25, 300, position.height - 25);
            for (int i =0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].DrawGUI();
            }
            DrawToolBar();
            OnEvent(Event.current);
        }
        //--------------------------------------------------------
        void DrawToolBar()
        {
            GUILayout.BeginArea(new Rect(0, 0, position.width, 25));
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("保存", GUILayout.Width(50)))
            {
                for (int i = 0; i < m_vLogics.Count; ++i)
                {
                    m_vLogics[i].OnSave();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        //--------------------------------------------------------
        internal void OnSelectComponent(AWidget component)
        {
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnSelectComponent(component);
            }
        }
        //--------------------------------------------------------
        public T GetLogic<T>() where T : AEditorLogic
        {
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                if (m_vLogics[i] is T)
                    return m_vLogics[i] as T;
            }
            return null;
        }
    }
}
#endif
/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDEditor
作    者:	HappLI
描    述:	HUD 编辑器
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using static UnityEngine.GridBrushBase;

namespace Framework.HUD.Editor
{
    public class HUDEditor : EditorWindow
    {
        static HUDEditor ms_pInstance = null;
        [MenuItem("Tools/HUD Editor")]
        public static void ShowWindow()
        {
            if (ms_pInstance == null)
                ms_pInstance = GetWindow<HUDEditor>("HUD Editor");
            ms_pInstance.Focus();
        }
        HudSystem m_pHudSystem = null;
        HudObject m_pHudObject = null;
        List<AEditorLogic> m_vLogics = new List<AEditorLogic>(3);
        //--------------------------------------------------------
        public HudSystem GetHudSystem()
        {
            return m_pHudSystem;
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
            if (m_pHudSystem != null)
                m_pHudSystem.Update();
        }
        //--------------------------------------------------------
        private void OnGUI()
        {
            m_vLogics[0].viewRect = new Rect(0, 0, 300, position.height);
            m_vLogics[1].viewRect = new Rect(300, 0, position.width - 600, position.height);
            m_vLogics[2].viewRect = new Rect(position.width - 300, 0, 300, position.height);
            for (int i =0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnGUI();
            }

            OnEvent(Event.current);
        }
    }
}
#endif
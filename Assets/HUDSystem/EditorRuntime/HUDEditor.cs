/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDEditor
作    者:	HappLI
描    述:	HUD 编辑器
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Application;

namespace Framework.HUD.Editor
{
    public class EditorTimer
    {
        public float m_PreviousTime;
        public float deltaTime = 0.02f;
        public float fixedDeltaTime = 0.02f;
        public float m_fDeltaTime = 0f;
        public float m_currentSnap = 1f;

        //-----------------------------------------------------
        public void Update()
        {
            if (Application.isPlaying)
            {
                // Application.targetFrameRate = 30;
                deltaTime = Time.deltaTime;
                m_fDeltaTime = (float)(deltaTime * m_currentSnap);
            }
            else
            {
                float curTime = Time.realtimeSinceStartup;
                m_PreviousTime = Mathf.Min(m_PreviousTime, curTime);//very important!!!

                deltaTime = curTime - m_PreviousTime;
                m_fDeltaTime = (float)(deltaTime * m_currentSnap);
            }

            m_PreviousTime = Time.realtimeSinceStartup;
        }
    }
    public class HUDEditor : EditorWindow, IHudSystemCallback
    {
        EditorTimer m_pTimer = new EditorTimer();
        //--------------------------------------------------------
        public static void EditorHud(HudObject pHudObject)
        {
            if (pHudObject == null)
                return;
            var windows = Resources.FindObjectsOfTypeAll<HUDEditor>();
            HUDEditor targetEditor = null;
            foreach (var win in windows)
            {
                if (win.m_pHudObject == pHudObject)
                {
                    win.m_pHudObject = null;
                    targetEditor = win;
                    targetEditor.SetHudObject(pHudObject);
                    targetEditor.Focus();
                    return;
                }
            }

            foreach (var win in windows)
            {
                if (win.m_pHudObject == null)
                {
                    targetEditor = win;
                    break;
                }
            }
            if(targetEditor == null)
                targetEditor = GetWindow<HUDEditor>("HUD Editor["+ pHudObject.name + "]");
            targetEditor.SetHudObject(pHudObject);
            targetEditor.Focus();
        }
        //--------------------------------------------------------
        HudSystem m_pHudSystem = null;
        HudObject m_pHudObject = null;
        HudController m_pHudController = null;
        List<AEditorLogic> m_vLogics = new List<AEditorLogic>(3);
        //--------------------------------------------------------
        public HudSystem GetHudSystem()
        {
            if (m_pHudSystem == null) m_pHudSystem = new HudSystem();
            m_pHudSystem.RegisterCallback(this);
            return m_pHudSystem;
        }
        //--------------------------------------------------------
        public HudController GetHud()
        {
            if (m_pHudController == null) m_pHudController = new HudController();
            m_pHudController.SetHudSystem(GetHudSystem());
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
                m_vLogics.Add(new HierarchyLogic(this, new Rect(0, 0, 300, position.height)));
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
            GetHud().Destroy();
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
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnDisable();
            }
            if (m_pHudController != null) m_pHudController.Destroy();
            m_pHudSystem.Destroy();
            m_pHudSystem = null;
            m_pHudController = null;
            m_pHudObject = null;
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
            m_pTimer.Update();
            if (!Application.isPlaying)
                JobHandle.ScheduleBatchedJobs();
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnUpdate(m_pTimer.deltaTime);
            }
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
                EditorUtility.SetDirty(m_pHudObject);
                AssetDatabase.SaveAssetIfDirty(m_pHudObject);
            }
            if (GUILayout.Button("说明文档", GUILayout.Width(80)))
            {
                Application.OpenURL("https://docs.qq.com/doc/DTGJMdnZ4SVJhdk9R");
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
        //--------------------------------------------------------
        public bool OnSpawnInstance(AWidget pWidget, string strParticle, Action<GameObject> onCallback)
        {
            if (string.IsNullOrEmpty(strParticle))
            {
                if (onCallback != null) onCallback(null);
                return true;
            }
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(strParticle);
            if (prefab == null)
            {
                if (onCallback != null) onCallback(null);
                return true;
            }
            GameObject pInst = GameObject.Instantiate(prefab);
            if (onCallback != null) onCallback(pInst);

            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnSpawnInstance(pWidget, strParticle, pInst);
            }
            return true;
        }
        //--------------------------------------------------------
        public bool OnDestroyInstance(AWidget pWidget, GameObject pGameObject)
        {
            for (int i = 0; i < m_vLogics.Count; ++i)
            {
                m_vLogics[i].OnDestroyInstance(pWidget, pGameObject);
            }

            if (Application.isPlaying)
                GameObject.Destroy(pGameObject);
            else GameObject.DestroyImmediate(pGameObject);
            pGameObject = null;

            return true;
        }
    }
}
#endif
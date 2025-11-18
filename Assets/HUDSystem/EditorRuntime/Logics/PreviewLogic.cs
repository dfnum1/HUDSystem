/********************************************************************
生成日期:	11:11:2025
类    名: 	PreviewLogic
作    者:	HappLI
描    述:	HUD 预览逻辑
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using UnityEditor;
using UnityEngine;

namespace Framework.HUD.Editor
{
    public class PreviewLogic : AEditorLogic
    {
        TargetPreview m_pPreview = null;
        GUIStyle m_PreviewStyle; 
        AComponent m_pSelectComponent = null;
        //--------------------------------------------------------
        public PreviewLogic(HUDEditor editor, Rect viewRect) : base(editor, viewRect)
        {
        }
        //--------------------------------------------------------
        internal override void OnSelectComponent(AComponent component)
        {
            m_pSelectComponent = component;
        }
        //--------------------------------------------------------
        public override void OnEnable()
        {
            if (m_pPreview == null)
            {
                m_pPreview = new TargetPreview();
                if (m_pPreview == null) m_pPreview = new TargetPreview(m_pEditor);
                GameObject[] roots = new GameObject[1];
                roots[0] = new GameObject("HudEditorRoot");
                m_pPreview.AddPreview(roots[0]);
                m_pPreview.showFloor = 0.01f;

                m_pPreview.SetCamera(0.01f, 10000f, 60f);
                m_pPreview.Initialize(roots);
                m_pPreview.SetPreviewInstance(roots[0] as GameObject);
                m_pPreview.bLeftMouseForbidMove = true;
                m_pPreview.OnDrawAfterCB += OnPreviewDraw;
                m_pPreview.SetLookatAndEulerAngle(Vector3.zero, new Vector3(0, 0, 0), 200,120);

                m_pEditor.GetHudSystem().SetRenderCamera(m_pPreview.GetCamera());
            }
        }
        //--------------------------------------------------------
        public override void OnDisable()
        {
            if (m_pPreview != null) m_pPreview.Destroy();
            m_pPreview = null;
        }
        //--------------------------------------------------------
        protected override void OnGUI()
        {
            if (m_pPreview != null && viewRect.width>0 && viewRect.height>0)
            {
                if (m_PreviewStyle == null) m_PreviewStyle = new GUIStyle(EditorStyles.textField);
                m_pPreview.OnPreviewGUI(new Rect(0,0, viewRect.width,viewRect.height), m_PreviewStyle);
            }
        }
        //--------------------------------------------------------
        void OnPreviewDraw(int controllerId, Camera camera, Event evt)
        {
            var hudSystem = GetHudSystem();
            if (hudSystem == null)
                return;
            hudSystem.Update();
            hudSystem.BeginRender();
            hudSystem.Render();
            hudSystem.EndRender();
            hudSystem.LateUpdate();

            if(m_pSelectComponent!=null)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 pos = Handles.DoPositionHandle(m_pSelectComponent.GetData().position, Quaternion.identity);
                if(EditorGUI.EndChangeCheck())
                {
                    m_pSelectComponent.GetData().position = pos;
                    m_pSelectComponent.SetDirty();
                }
            }
        }
    }
}
#endif
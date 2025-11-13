/********************************************************************
生成日期:	11:11:2025
类    名: 	PreviewLogic
作    者:	HappLI
描    述:	HUD 预览逻辑
*********************************************************************/
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Framework.HUD.Editor
{
    public class PreviewLogic : AEditorLogic
    {
        TargetPreview m_pPreview = null;
        GUIStyle m_PreviewStyle;
        //--------------------------------------------------------
        public PreviewLogic(HUDEditor editor, Rect viewRect) : base(editor, viewRect)
        {
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

                m_pPreview.SetCamera(0.01f, 10000f, 60f);
                m_pPreview.Initialize(roots);
                m_pPreview.SetPreviewInstance(roots[0] as GameObject);
                m_pPreview.bLeftMouseForbidMove = true;
                m_pPreview.OnDrawAfterCB += OnPreviewDraw;

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
        public override void OnGUI()
        {
            if (m_pPreview != null && viewRect.width>0 && viewRect.height>0)
            {
                if (m_PreviewStyle == null) m_PreviewStyle = new GUIStyle(EditorStyles.textField);
                m_pPreview.OnPreviewGUI(viewRect, m_PreviewStyle);
            }
        }
        //--------------------------------------------------------
        void OnPreviewDraw(int controllerId, Camera camera, Event evt)
        {
            m_pEditor.GetHudSystem().EditorRender();
        }
    }
}
#endif
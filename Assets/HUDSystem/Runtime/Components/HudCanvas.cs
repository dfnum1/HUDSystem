/********************************************************************
生成日期:	11:11:2025
类    名: 	HudCanvas
作    者:	HappLI
描    述:	组
*********************************************************************/
using System.Collections.Generic;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    [HudData(typeof(HudCanvasData))]
    public class HudCanvas : AComponent
    {
        private Transform m_pFollowTarget;
        private Matrix4x4 m_pFollowTransform;

        private Vector3 m_Offset = Vector3.zero;
        private Vector3 m_OffsetRotation = Vector3.zero;
        public HudCanvas(HudSystem pSystem) : base(pSystem)
        {
            m_eHudType = EHudType.Canvas;
        }
        //--------------------------------------------------------
        public void SetFollowTarget(Transform target)
        {
            m_pFollowTarget = target;
        }
        //--------------------------------------------------------
        public Transform GetFollowTarget()
        {
            return m_pFollowTarget;
        }
        //--------------------------------------------------------
        public void SetFollowTransform(Matrix4x4 transform)
        {
            m_pFollowTransform = transform;
        }
        //--------------------------------------------------------
        public Matrix4x4 GetFollowTransform()
        {
            return m_pFollowTransform;
        }
    }
}

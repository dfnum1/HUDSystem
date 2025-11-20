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
    //--------------------------------------------------------
    [System.Serializable]
    public class HudCanvasData : HudBaseData
    {
        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return TypePool.MallocWidget<HudCanvas>(pSystem,this);
        }
    }
    //--------------------------------------------------------
    //! HudCanvas
    //--------------------------------------------------------
    [HudData(typeof(HudCanvasData))]
    public class HudCanvas : AWidget
    {
        public HudCanvas() : base()
        {
            m_eHudType = EHudType.Canvas;
        }
        //--------------------------------------------------------
        protected override void OnInit()
        {
            Refresh();
        }
        //--------------------------------------------------------
        protected override void OnSyncData()
        {
            
        }
        //--------------------------------------------------------
        protected override void OnDirty()
        {
            Refresh();
        }
        //--------------------------------------------------------
        void Refresh()
        {
            OnAlpha();
        }
    }
}

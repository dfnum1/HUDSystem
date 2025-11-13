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
        public HudCanvas(HudSystem pSystem, HudBaseData hudData) : base(pSystem, hudData)
        {
            m_eHudType = EHudType.Canvas;
        }
    }
}

/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDText
作    者:	HappLI
描    述:	HUD 文字图
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    public class HUDText : AGraphic
    {
        public override EGraphicType GetGraphicType()
        {
            return EGraphicType.Text;
        }
        //--------------------------------------------------------
        public override int GetQuadCount()
        {
            return 16;
        }
    }
}

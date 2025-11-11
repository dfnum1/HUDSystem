/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDSprite
作    者:	HappLI
描    述:	HUD 精灵图
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    public class HUDSprite : AGraphic
    {
        public override EGraphicType GetGraphicType()
        {
            return EGraphicType.Sprite;
        }
        //--------------------------------------------------------
        public override int GetQuadCount()
        {
            return 1;
        }
    }
}

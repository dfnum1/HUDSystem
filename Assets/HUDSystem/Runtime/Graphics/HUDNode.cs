/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDNode
作    者:	HappLI
描    述:	节点
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    public class HUDNode : AGraphic
    {
        public override EGraphicType GetGraphicType()
        {
            return EGraphicType.Node;
        }
        //--------------------------------------------------------
        public override int GetQuadCount()
        {
            return 0;
        }
    }
}

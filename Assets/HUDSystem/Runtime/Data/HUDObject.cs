/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDObject
作    者:	HappLI
描    述:	HUD 数据对象层
*********************************************************************/
using System.Collections.Generic;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    public class HudObject : ScriptableObject
    {
        [System.Serializable]
        public struct Hierarchy
        {
            public int parentId;
            public List<int> children;
        }
        public List<Hierarchy> vHierarchies;
    }
}

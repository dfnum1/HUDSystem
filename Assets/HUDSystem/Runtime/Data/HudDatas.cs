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
    //--------------------------------------------------------
    public enum HorizontalAlignment : byte
    {
        Left,
        Middle,
        Right,
    }
    //--------------------------------------------------------
    public enum VerticalAlignment : byte
    {
        Bottom,
        Middle,
        Top,
    }
    //--------------------------------------------------------
    public enum EMaskType : byte
    {
        None =0,
        Rect,
        Circle
    }
    //--------------------------------------------------------
    [System.Serializable]
    public abstract class HudBaseData
    {
        public int id;
        public string name;
        public bool rayTest = true;
        public bool visible = true;

        public Vector3 position;
        public Vector2 sizeDelta = new Vector2(100,100);
        public float angle = 0;
        public Color   color =Color.white;
        public EMaskType mask;
        public Rect maskRegion;

        public abstract AWidget CreateWidget(HudSystem pSystem);
    }
}

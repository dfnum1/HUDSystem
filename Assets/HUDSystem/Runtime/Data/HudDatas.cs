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
    [System.Serializable]
    internal class HudBaseData
    {
        public int id;
        public string name;

        public Vector2 position;
        public Vector2 sizeDelta = new Vector2(100,100);
        public Vector3 eulerAngle;
        public Color   color;

        public List<int> childs;
    }
    //--------------------------------------------------------
    [System.Serializable]
    internal class HudCanvasData : HudBaseData
    {
    }
    //--------------------------------------------------------
    [System.Serializable]
    internal class HudTextData : HudBaseData
    {
        public string text;
        public int fontSize;
        public float lineSpacing;
        public HorizontalAlignment horizontalAlignment;
        public TMPro.TMP_FontAsset fontAsset;
    }
    //--------------------------------------------------------
    [System.Serializable]
    internal class HudImageData : HudBaseData
    {
        public enum FillMethod : byte
        {
            Horizontal,
            Vertical,
        }

        public enum ImageType : byte
        {
            Simple,
            Sliced,
            Filled,
        }

        public enum OriginHorizontal : byte
        {
            Left,
            Right,
        }

        public enum OriginVertical : byte
        {
            Bottom,
            Top,
        }
        public Sprite sprite;
        public ImageType imageType;
        public FillMethod fillMethod;
        public OriginHorizontal fillOrigin = OriginHorizontal.Left;
        public float fillAmount = 1.0f;
    }
}

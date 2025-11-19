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
    //--------------------------------------------------------
    [System.Serializable]
    public class HudCanvasData : HudBaseData
    {
        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return new HudCanvas(pSystem, this);
        }
    }
    //--------------------------------------------------------
    [System.Serializable]
    public class HudTextData : HudBaseData
    {
        public string text;
        public float fontSize = 20;
        public float lineSpacing =0;
        public HorizontalAlignment alignment = HorizontalAlignment.Middle;
        //    public TMPro.TMP_FontAsset fontAsset;
        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return new HudText(pSystem, this);
        }
    }
    //--------------------------------------------------------
    [System.Serializable]
    public class HudNumberData : HudBaseData
    {
        public string strNumber;
        public float fontSize = 20;
        public HorizontalAlignment alignment = HorizontalAlignment.Middle;
        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return new HudNumber(pSystem, this);
        }
    }
    //--------------------------------------------------------
    [System.Serializable]
    public class HudImageData : HudBaseData
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
        public int fillOrigin = 0;
        public float fillAmount = 1.0f;
        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return new HudImage(pSystem, this);
        }
    }
    //--------------------------------------------------------
    [System.Serializable]
    public class HudParticleData : HudBaseData
    {
        public string strParticle;
        public Vector3 scale = Vector3.one;
        public int renderOrder = 0;
        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return new HudParticle(pSystem, this);
        }
    }
}

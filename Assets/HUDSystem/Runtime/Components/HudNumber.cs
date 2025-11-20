/********************************************************************
生成日期:	11:11:2025
类    名: 	HudNumber
作    者:	HappLI
描    述:	数字
*********************************************************************/
using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    //--------------------------------------------------------
    [System.Serializable]
    public class HudNumberData : HudBaseData
    {
        public string strNumber;
        public float fontSize = 1;
        public HorizontalAlignment alignment = HorizontalAlignment.Middle;
        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return TypePool.MallocWidget<HudNumber>(pSystem, this);
        }
    }
    //--------------------------------------------------------
    //! HudNumber
    //--------------------------------------------------------
    [HudData(typeof(HudNumberData))]
    public class HudNumber : AWidget
    {
        enum EOverrideType : byte
        {
            FontSize = EParamOverrideType.Count,
            Alignment,
            Count,
        }
        Vector2 m_Size = Vector2.zero;
        string m_strNumber = "";
        public HudNumber() : base()
        {
            m_eHudType = EHudType.Number;
        }
        //--------------------------------------------------------
        protected override void OnInit()
        {
            Refresh();
        }
        //--------------------------------------------------------
        protected override void OnDirty()
        {
            Refresh();
        }
        //--------------------------------------------------------
        protected override void OnSyncData()
        {
            HudNumberData data = m_pHudData as HudNumberData;
            m_strNumber = data.strNumber;
        }
        //--------------------------------------------------------
        public void SetNumber(string text)
        {
            if (m_strNumber == text)
                return;
            m_strNumber = text;
            if (IsEditor())
            {
                HudNumberData hudImageData = m_pHudData as HudNumberData;
                hudImageData.strNumber = text;
            }
            SetDirty();
        }
        //--------------------------------------------------------
        public string GetNumber()
        {
            return m_strNumber;
        }
        //--------------------------------------------------------
        public void SetFontSize(float fontSize)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.FontSize, fontSize);
            if (bDirty) SetDirty();
        }
        //--------------------------------------------------------
        public float GetFontSize()
        {
            if (GetOverrideParam((byte)EOverrideType.FontSize, out var temp))
                return temp.floatVal0;
            HudNumberData data = m_pHudData as HudNumberData;
            return data.fontSize;
        }
        //--------------------------------------------------------
        public void SetAlignment(HorizontalAlignment align)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.Alignment, (int)align);
            if (bDirty) SetDirty();
        }
        //--------------------------------------------------------
        public HorizontalAlignment GetAlignment()
        {
            if (GetOverrideParam((byte)EOverrideType.Alignment, out var temp))
                return (HorizontalAlignment)temp.intVal0;
            HudNumberData data = m_pHudData as HudNumberData;
            return data.alignment;
        }
        //--------------------------------------------------------
        void Refresh()
        {
            m_Size = Vector2.zero;
            HudAtlas hudAtlas = GetHudAtlas();
            if (hudAtlas == null)
                return;
            if(string.IsNullOrEmpty(m_strNumber))
            {
                ResizeDataSnippet(0);
                return;
            }
            char[] chars = m_strNumber.ToCharArray();
            ResizeDataSnippet(1);
            HudDataSnippet snippet = GetDataSnippet(0);
            if (snippet == null) return;
            snippet.ResetNineParam();
            int count = chars.Length;
            int quadindex = 0;
            float curlen = 0.001f;

            float fontSize = GetFontSize()/10.0f;
            for (int i = 0; i < count; i++)
            {
                HudAtlas.SpriteInfo spriteInfo = hudAtlas.GetSpriteInfo(chars[i].ToString());
                if (spriteInfo != null)
                {
                    float2 size = new float2(spriteInfo.size.x, spriteInfo.size.y);
                    size = size * fontSize;

                    m_Size.x += size.x;
                    if (size.y > m_Size.y)
                        m_Size.y = size.y;

                    snippet.SetSpriteId(quadindex, spriteInfo.index);
                    snippet.SetSpritePositon(quadindex, new float2(curlen, -size.y / 2));
                    snippet.SetSpriteSize(quadindex, size);
                    snippet.SetAmount(1, 0, 0);
                    curlen += size.x;
                    quadindex++;
                    if (quadindex >= HUDUtils.QUAD_COUNT) break;
                }
            }
            for (int i = 0; i < quadindex; i++)
            {
                SetAlignment(curlen, snippet, i);
            }
            snippet.WriteParamData();
        }
        //--------------------------------------------------------
        new public Vector2 GetSize()
        {
            return m_Size;
        }
        //--------------------------------------------------------
        public void SetAlignment(float lenght, HudDataSnippet snippet, int index)
        {
            switch (GetAlignment())
            {
                case HorizontalAlignment.Left:

                    break;
                case HorizontalAlignment.Middle:
                    {
                        float2 pos = snippet.GetSpritePosition(index);
                        pos += new float2(-lenght / 2, 0);
                        snippet.SetSpritePositon(index, pos);
                    }
                    break;
                case HorizontalAlignment.Right:
                    {
                        float2 pos = snippet.GetSpritePosition(index);
                        pos += new float2(-lenght, 0);
                        snippet.SetSpritePositon(index, pos);
                    }
                    break;
            }
        }
        //--------------------------------------------------------
        protected override void OnDestroy()
        {
            m_strNumber = null;
            m_Size = Vector2.zero;
        }
    }
}

/********************************************************************
生成日期:	11:11:2025
类    名: 	HudText
作    者:	HappLI
描    述:	文字
*********************************************************************/
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore;
using static PlasticGui.PlasticTableColumn;

namespace Framework.HUD.Runtime
{
    //--------------------------------------------------------
    [System.Serializable]
    public class HudTextData : HudBaseData
    {
        public string text;
        public float fontSize = 20;
        public float lineSpacing = 0;
        public HorizontalAlignment alignment = HorizontalAlignment.Middle;
        //    public TMPro.TMP_FontAsset fontAsset;
        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return TypePool.MallocWidget<HudText>(pSystem, this);
        }
    }
    //--------------------------------------------------------
    // ! HudText
    //--------------------------------------------------------
    [HudData(typeof(HudTextData))]
    public class HudText : AWidget
    {
        enum EOverrideType : byte
        {
            FontSize = EParamOverrideType.Count,
            LineSpacing,
            Alignment,
            Count,
        }

        Vector2 m_Size = Vector2.zero;
        string m_strText = null;
        public HudText() : base()
        {
            m_eHudType = EHudType.Text;
        }
        //--------------------------------------------------------
        protected override void OnInit()
        {
            Refresh();
        }
        //--------------------------------------------------------
        protected override void OnSyncData()
        {
            HudTextData hudTextData = m_pHudData as HudTextData;
            m_strText = hudTextData.text;
        }
        //--------------------------------------------------------
        public void SetText(string text)
        {
            if (m_strText == text)
                return;
            m_strText = text;
            if (IsEditor())
            {
                HudTextData data = m_pHudData as HudTextData;
                data.text = text;
            }
            SetDirty();
        }
        //--------------------------------------------------------
        public string GetText()
        {
            return m_strText;
        }
        //--------------------------------------------------------
        public void SetFontSize(float fontSize)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.FontSize, fontSize);
            if(bDirty) SetDirty();
        }
        //--------------------------------------------------------
        public float GetFontSize()
        {
            if (GetOverrideParam((byte)EOverrideType.FontSize, out var temp))
                return temp.floatVal0;
            HudTextData data = m_pHudData as HudTextData;
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
            HudTextData data = m_pHudData as HudTextData;
            return data.alignment;
        }
        //--------------------------------------------------------
        public void SetLineSpacing(float space)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.LineSpacing, space);
            if (bDirty) SetDirty();
        }
        //--------------------------------------------------------
        public float GetLineSpacing()
        {
            if (GetOverrideParam((byte)EOverrideType.LineSpacing, out var temp))
                return temp.floatVal0;
            HudTextData data = m_pHudData as HudTextData;
            return data.lineSpacing;
        }
        //--------------------------------------------------------
        protected override void OnDirty()
        {
            Refresh();
        }
        //--------------------------------------------------------
        internal void Refresh()
        {
            if (string.IsNullOrEmpty(m_strText))
            {
                ResizeDataSnippet(0);
                return;
            }
            if (m_HudController.GetMaterial() == null)
                return;

            var expandFontAssets = m_HudController.GetFontAsset();
            if (expandFontAssets == null)
                return;
            char[] chars = m_strText.ToCharArray();
            if (chars.Length == 0)
            {
                ResizeDataSnippet(0);
                return;
            }

            float lineSpace = GetLineSpacing();
            HorizontalAlignment alignment = GetAlignment();

            int charCount = math.min(4 * HUDUtils.QUAD_COUNT, chars.Length);
            int snippetCount = (charCount - 1) / HUDUtils.QUAD_COUNT + 1;
            ResizeDataSnippet(snippetCount);
            bool isUsingAlternativeTypeface;
            float padding = ShaderUtilities.GetPadding(m_HudController.GetMaterial(), false, false);
            float fontsize = GetFontSize()/10.0f;
            float adjustedScale = fontsize / expandFontAssets.faceInfo.pointSize * expandFontAssets.faceInfo.scale * 0.1f;
            float curAdvance = 0;
            int curcharCount = 0;
            int snippetIndex = 0;
            int quadIndex = 0;
            HudDataSnippet snippet = GetDataSnippet(snippetIndex);
            snippet.ResetNineParam();
            snippet.SetTextOrImage(true);
            float2 bl = new float2(float.MaxValue, float.MaxValue);
            float2 tr = new float2(float.MinValue, float.MinValue);
            for (int i = 0; i < charCount; i++)
            {
                TMP_Character character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(chars[i], expandFontAssets, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                if (character == null)
                {
                    character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(chars[i], TMP_Settings.defaultFontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                }
                if (character == null)
                {
                    character = TMP_FontAssetUtilities.GetCharacterFormSysFont(chars[i], FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                }
                if (character != null)
                {
                    if (curcharCount >= HUDUtils.QUAD_COUNT && curcharCount % HUDUtils.QUAD_COUNT == 0)
                    {
                        snippet.SetTmpParam(padding, adjustedScale);
                        snippetIndex++;
                        snippet = GetDataSnippet(snippetIndex);
                        snippet.ResetNineParam();
                        snippet.SetTextOrImage(true);
                        quadIndex = 0;
                    }
                    GlyphMetrics currentGlyphMetrics = character.glyph.metrics;
                    snippet.SetSpriteId(quadIndex, character.index);
                    float currentElementScale = adjustedScale * character.glyph.scale;
                    float2 top_left;
                    top_left.x = (currentGlyphMetrics.horizontalBearingX - padding) * currentElementScale;
                    top_left.y = (currentGlyphMetrics.horizontalBearingY + padding) * currentElementScale;
                    float2 bottom_left;
                    bottom_left.x = top_left.x + curAdvance;
                    bottom_left.y = top_left.y - (currentGlyphMetrics.height + padding * 2) * currentElementScale;
                    float2 top_right;
                    top_right.x = bottom_left.x + (currentGlyphMetrics.width + padding * 2) * currentElementScale;
                    top_right.y = top_left.y;
                    float2 bottom_right;
                    bottom_right.x = top_right.x;
                    bottom_right.y = bottom_left.y;
                    bl = math.min(bottom_left, bl);
                    tr = math.max(top_right, tr);
                    snippet.SetSpritePositon(quadIndex, bottom_left);
                    snippet.SetSpriteSize(quadIndex, (top_right - bottom_left));
                    float advance = (character.glyph.metrics.horizontalAdvance + lineSpace) * currentElementScale + lineSpace;
                    curAdvance += advance;
                    quadIndex++;
                    curcharCount++;
                }
            }
            bl = math.max(bl, new float2(0, 0));
            snippet.SetTmpParam(padding, adjustedScale);
            float2 movePos = SetAlignment(bl, tr);
            for (int i = 0; i <= snippetIndex; i++)
            {
                HudDataSnippet datasnippet = GetDataSnippet(i);
                for (int spriteIndex = 0; spriteIndex < 9; spriteIndex++)
                {
                    float2 pos = datasnippet.GetSpritePosition(spriteIndex);
                    pos += movePos;
                    datasnippet.SetSpritePositon(spriteIndex, pos);
                }
                datasnippet.WriteParamData();
            }
            m_Size = new Vector2(tr.x - bl.x, tr.y - bl.y);

            if (IsEditor())
            {
                HudTextData data = m_pHudData as HudTextData;
                data.sizeDelta = m_Size;
            }
        }
        //--------------------------------------------------------
        new public Vector2 GetSize()
        {
            return m_Size;
        }
        //--------------------------------------------------------
        public float2 SetAlignment(float2 bl, float2 tr)
        {
            switch (GetAlignment())
            {
                case HorizontalAlignment.Left:
                    {
                        float2 movePos = -(tr + bl) / 2;
                        return new float2(-bl.x, movePos.y);
                    }
                case HorizontalAlignment.Middle:
                    {
                        float2 movePos = -(tr + bl) / 2;
                        return movePos;
                    }
                case HorizontalAlignment.Right:
                    {
                        float2 movePos = -(tr + bl) / 2;
                        return new float2(-tr.x, movePos.y);
                    }
            }
            return bl;
        }
        //--------------------------------------------------------
        protected override void OnDestroy()
        {
            m_strText = null;
            m_Size = Vector2.zero;
        }
    }
}

/********************************************************************
生成日期:	11:11:2025
类    名: 	HudRich
作    者:	HappLI
描    述:	图文混排
*********************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore;

namespace Framework.HUD.Runtime
{
    [System.Serializable]
    public class HudRichData : HudBaseData
    {
        /// <summary>
        /// 富文本内容，支持 [img=content,w=宽,h=高,ox=偏移x,oy=偏移y] 标签
        /// </summary>
        public string richText;
        public float fontSize = 20;
        public HorizontalAlignment alignment = HorizontalAlignment.Middle;
        public float lineSpacing = 0;
        public float lineHeight = 20;

        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return TypePool.MallocWidget<HudRich>(pSystem, this);
        }
    }
    //--------------------------------------------------------
    //! HudRich
    //--------------------------------------------------------
    [HudData(typeof(HudRichData))]
    public class HudRich : AWidget
    {
        //--------------------------------------------------------
        public struct RichSegment
        {
            public bool isText;
            public string content;
            public Vector2 size;
            public Vector2 offset;
            public RichSegment(string text)
            {
                isText = true;
                content = text;
                size = Vector2.zero;
                offset = Vector2.zero;
            }
            public RichSegment(string sprite,Vector2 size, Vector2 offset)
            {
                isText = false;
                content = sprite;
                this.size = size;
                this.offset = offset;
            }
        }
        //--------------------------------------------------------
        enum EOverrideType : byte
        {
            FontSize = EParamOverrideType.Count,
            Alignment,
            LineSpacing,
            LineHeight,
            Count,
        }
        //--------------------------------------------------------
        string m_richText = "";
        Vector2 m_Size = Vector2.zero;

        //--------------------------------------------------------
        public HudRich() : base()
        {
            m_eHudType = EHudType.Rich;
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
            HudRichData data = m_pHudData as HudRichData;
            m_richText = data.richText;
        }
        //--------------------------------------------------------
        public void SetRichText(string text)
        {
            if (m_richText == text)
                return;
            m_richText = text;
            if (IsEditor())
            {
                HudRichData data = m_pHudData as HudRichData;
                data.richText = text;
            }
            SetDirty();
        }
        //--------------------------------------------------------
        public string GetRichText()
        {
            return m_richText;
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
            HudRichData data = m_pHudData as HudRichData;
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
            HudRichData data = m_pHudData as HudRichData;
            return data.alignment;
        }
        //--------------------------------------------------------
        public void SetLineSpacing(float spacing)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.LineSpacing, spacing);
            if (bDirty) SetDirty();
        }
        //--------------------------------------------------------
        public float GetLineSpacing()
        {
            if (GetOverrideParam((byte)EOverrideType.LineSpacing, out var temp))
                return temp.floatVal0;
            HudRichData data = m_pHudData as HudRichData;
            return data.lineSpacing;
        }
        //--------------------------------------------------------
        public void SetLineHeight(float spacing)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.LineHeight, spacing);
            if (bDirty) SetDirty();
        }
        //--------------------------------------------------------
        public float GetLineHeight()
        {
            if (GetOverrideParam((byte)EOverrideType.LineHeight, out var temp))
                return temp.floatVal0;
            HudRichData data = m_pHudData as HudRichData;
            return data.lineHeight;
        }
        //--------------------------------------------------------
        void Refresh()
        {
            m_Size = Vector2.zero;
            HudAtlas hudAtlas = GetHudAtlas();
            if (hudAtlas == null) return;
            if (string.IsNullOrEmpty(m_richText))
            {
                ResizeDataSnippet(0);
                return;
            }

            List<RichSegment> segments = new List<RichSegment>();
            string text = m_richText;
            var regex = new Regex(@"\[img=([^\]]+)\]");
            var matches = regex.Matches(text);

            int lastPos = 0;
            foreach (Match match in matches)
            {
                if (match.Index > lastPos)
                    segments.Add(new RichSegment(text.Substring(lastPos, match.Index - lastPos)));
                var tag = ParseImageTag(match.Groups[1].Value);
                segments.Add(tag);
                lastPos = match.Index + match.Length;
            }
            if (lastPos < text.Length)
                segments.Add(new RichSegment(text.Substring(lastPos)));

            // 计算需要多少个 HudDataSnippet
            int snippetCount =0;
            foreach(var seg in segments)
            {
                if (seg.isText)
                {
                    int charCount = math.min(4 * HUDUtils.QUAD_COUNT, seg.content.Length);
                    snippetCount += (charCount - 1) / HUDUtils.QUAD_COUNT+1;
                }
                else snippetCount++;
            }
            ResizeDataSnippet(snippetCount);

            var fontAsset = m_HudController?.GetFontAsset();
            var material = m_HudController?.GetMaterial();
            if (fontAsset == null || material == null)
                return;

            float fontSize = GetFontSize();
            float lineSpacing = GetLineSpacing();
            float padding = TMPro.ShaderUtilities.GetPadding(material, false, false);
            float fontsizeScale = fontSize / 10.0f;
            float adjustedScale = fontsizeScale / fontAsset.faceInfo.pointSize * fontAsset.faceInfo.scale * 0.1f;

            int snippetIndex = 0;
            float curAdvance = 0;
            float fontCurAdvance = 0;
            float curY = 0;
            float currentElementScale = 0;
            float2 bl = new float2(float.MaxValue, float.MaxValue);
            float2 tr = new float2(float.MinValue, float.MinValue);
            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];

                if (seg.isText)
                {
                    HudDataSnippet snippet = null;
                    int quadindex = 0;
                    int curcharCount = 0;
                    for (int j =0; j < seg.content.Length; ++j)
                    {
                        char c = seg.content[j];
                        //! 判断是否是换行符
                        if(j+1 < seg.content.Length && seg.content[j] == '\\' && seg.content[j+1] == 'n')
                        {
                            j++;
                            curAdvance = 0;
                            fontCurAdvance = 0;
                            bl.x = float.MaxValue;
                            curY += GetLineHeight()/HUDUtils.PIXEL_SIZE;
                            continue;
                        }
                        if (j + 3 < seg.content.Length && seg.content[j] == '\\' && seg.content[j+1] == 'r' && seg.content[j+2] == '\\' && seg.content[j+3] == 'n')
                        {
                            j+=3;
                            curAdvance = 0;
                            bl.x = float.MaxValue;
                            fontCurAdvance = 0;
                            curY += GetLineHeight() / HUDUtils.PIXEL_SIZE;
                            continue;
                        }
                        bool isUsingAlternativeTypeface;
                        var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(c, fontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface)
                            ?? TMP_FontAssetUtilities.GetCharacterFromFontAsset(c, TMP_Settings.defaultFontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);

                        if (character == null) continue;
                        if (snippet == null)
                        {
                            snippet = GetDataSnippet(snippetIndex);
                            snippet.ResetNineParam();
                            snippet.SetTextOrImage(true);
                            snippetIndex++;
                            quadindex = 0;
                        }
                        if (curcharCount >= HUDUtils.QUAD_COUNT && curcharCount % HUDUtils.QUAD_COUNT == 0)
                        {
                            snippet.WriteParamData();
                            snippet.SetTmpParam(padding, adjustedScale);

                            snippet = GetDataSnippet(snippetIndex);
                            snippet.ResetNineParam();
                            snippet.SetTextOrImage(true);
                            snippetIndex++;
                            quadindex = 0;
                        }
                        GlyphMetrics currentGlyphMetrics = character.glyph.metrics;
                        snippet.SetSpriteId(quadindex, character.index);
                        currentElementScale = adjustedScale * character.glyph.scale;
                        float2 top_left;
                        top_left.x = (currentGlyphMetrics.horizontalBearingX - padding) * currentElementScale;
                        top_left.y = (currentGlyphMetrics.horizontalBearingY + padding) * currentElementScale - curY;
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
                        snippet.SetSpritePositon(quadindex, bottom_left);
                        snippet.SetSpriteSize(quadindex, (top_right - bottom_left));
                        float advance = (character.glyph.metrics.horizontalAdvance + lineSpacing) * currentElementScale + lineSpacing;
                        curAdvance += advance;
                        fontCurAdvance += character.glyph.metrics.horizontalAdvance + lineSpacing;
                        quadindex++;
                        curcharCount++;
                    }
                    snippet.SetTmpParam(padding, adjustedScale);
                    snippet.WriteParamData();
                }
                else
                {
                    HudAtlas.SpriteInfo spriteInfo = hudAtlas.GetSpriteInfo(seg.content);
                    if (spriteInfo != null)
                    {
                        HudDataSnippet snippet = GetDataSnippet(snippetIndex);
                        snippetIndex++;
                        int quadindex = 0;
                        snippet.ResetNineParam();
                        snippet.SetTextOrImage(false);

                        Vector2 size = seg.size == Vector2.zero ? new Vector2(spriteInfo.size.x, spriteInfo.size.y) : seg.size;
                        Vector2 pos = new Vector2(fontCurAdvance + seg.offset.x - seg.size.x/2, seg.offset.y - curY);
                        snippet.SetSpriteId(quadindex, spriteInfo.index);
                        snippet.SetSpritePositon(quadindex, pos);
                        snippet.SetSpriteSize(quadindex, size);
                        snippet.SetAmount(1, 0, 0);
                        bl = math.min(pos, bl);
                        tr = math.max(pos + size * 0.5f /HUDUtils.PIXEL_SIZE, tr);
                        curAdvance += size.x* 0.5f / HUDUtils.PIXEL_SIZE;
                        fontCurAdvance += size.x*0.5f;
                        snippet.WriteParamData();
                    }
                }
            }
            bl = math.max(bl, new float2(0, 0));
            float2 movePos = SetAlignment(bl, tr);

            m_Size = new Vector2(tr.x - bl.x, tr.y - bl.y);
            if (IsEditor())
            {
                HudRichData data = m_pHudData as HudRichData;
                data.sizeDelta = m_Size;
            }
        }
        //--------------------------------------------------------
        new public Vector2 GetSize()
        {
            return m_Size;
        }
        //--------------------------------------------------------
        Vector2 SetAlignment(Vector2 bl, Vector2 tr)
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
        private RichSegment ParseImageTag(string tag)
        {
            var result = new RichSegment();
            result.isText = false;
            //[img=content,w=宽,h=高,ox=偏移x,oy=偏移y] 标签
            var regex = new Regex(@"([^\s,]+)(?:,w=(\d+))?(?:,h=(\d+))?(?:,ox=(-?\d+))?(?:,oy=(-?\d+))?");
            var match = regex.Match(tag);
            if (match.Success)
            {
                result.content = match.Groups[1].Value;
                if (match.Groups[2].Success && !string.IsNullOrEmpty(match.Groups[2].Value)) result.size.x = float.Parse(match.Groups[2].Value);
                if (match.Groups[3].Success && !string.IsNullOrEmpty(match.Groups[3].Value)) result.size.y = float.Parse(match.Groups[3].Value);
                if (match.Groups[4].Success && !string.IsNullOrEmpty(match.Groups[4].Value)) result.offset.x = float.Parse(match.Groups[4].Value);
                if (match.Groups[5].Success && !string.IsNullOrEmpty(match.Groups[5].Value)) result.offset.y = float.Parse(match.Groups[5].Value);
            }
            return result;
        }
        //--------------------------------------------------------
        protected override void OnDestroy()
        {
            m_richText = null;
            m_Size = Vector2.zero;
        }
    }
}
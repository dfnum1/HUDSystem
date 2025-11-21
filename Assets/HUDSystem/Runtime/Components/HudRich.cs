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
            public Color color;
            public RichSegment(string text, Color color)
            {
                isText = true;
                content = text;
                size = Vector2.zero;
                offset = Vector2.zero;
                this.color = color;
            }
            public RichSegment(string sprite,Vector2 size, Vector2 offset, Color color)
            {
                isText = false;
                content = sprite;
                this.size = size;
                this.offset = offset;
                this.color = color;
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
        static List<RichSegment> ms_vSegments = null;
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

            string text = m_richText;
            if (ms_vSegments == null) ms_vSegments = new List<RichSegment>(2);
            ms_vSegments.Clear();
            ParseSimpleHtmlRichText(text,ref ms_vSegments);
            // 计算需要多少个 HudDataSnippet
            int snippetCount =0;
            foreach(var seg in ms_vSegments)
            {
                if (seg.isText)
                {
                    snippetCount += (seg.content.Length - 1) / HUDUtils.QUAD_COUNT+1;
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
            float maxWidth = 0;
            float curY = 0;
            float currentElementScale = 0;
            float2 bl = new float2(float.MaxValue, float.MaxValue);
            float2 tr = new float2(float.MinValue, float.MinValue);
            for (int i = 0; i < ms_vSegments.Count; i++)
            {
                var seg = ms_vSegments[i];

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
                        TMP_Character character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(seg.content[j], fontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                        if (character == null)
                        {
                            character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(seg.content[j], TMP_Settings.defaultFontAsset, true, FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                        }
                        if (character == null)
                        {
                            character = TMP_FontAssetUtilities.GetCharacterFormSysFont(seg.content[j], FontStyles.Normal, FontWeight.Regular, out isUsingAlternativeTypeface);
                        }
                        if (character == null) continue;
                        if (snippet == null)
                        {
                            snippet = GetDataSnippet(snippetIndex);
                            snippet.ResetNineParam();
                            snippet.SetTextOrImage(true);
                            snippet.SetMutiColor(seg.color);
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
                            snippet.SetMutiColor(seg.color);
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
                        fontCurAdvance += advance* HUDUtils.PIXEL_SIZE;
                        maxWidth = Mathf.Max(maxWidth, fontCurAdvance);
                        quadindex++;
                        curcharCount++;
                    }
                    if(snippet!=null)
                    {
                        snippet.SetTmpParam(padding, adjustedScale);
                        snippet.WriteParamData();
                    }
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
                        snippet.SetMutiColor(seg.color);

                        Vector2 size = seg.size == Vector2.zero ? new Vector2(spriteInfo.size.x, spriteInfo.size.y) : seg.size;
                        Vector2 pos = new Vector2(fontCurAdvance + seg.offset.x - seg.size.x/2, seg.offset.y - curY);
                        snippet.SetSpriteId(quadindex, spriteInfo.index);
                        snippet.SetSpritePositon(quadindex, pos);
                        snippet.SetSpriteSize(quadindex, size);
                        snippet.SetAmount(1, 0, 0);
                        bl = math.min(pos, bl);
                        tr = math.max(pos + size, tr);
                        curAdvance += size.x* 0.5f / HUDUtils.PIXEL_SIZE;
                        fontCurAdvance += size.x;
                        maxWidth = Mathf.Max(maxWidth, fontCurAdvance);
                        snippet.WriteParamData();
                    }
                }
            }
            bl = math.max(bl, new float2(0, 0));
            tr.x = bl.x + maxWidth;
            tr.y = bl.y + Mathf.Max(curY,GetLineHeight());
            float2 movePos = SetAlignment(bl, tr);
            for (int i = 0; i < snippetIndex; i++)
            {
                HudDataSnippet datasnippet = GetDataSnippet(i);
                for (int spriteIndex = 0; spriteIndex < 9; spriteIndex++)
                {
                    float2 pos = datasnippet.GetSpritePosition(spriteIndex);
                    if (datasnippet.isText)
                    {
                        pos += movePos / HUDUtils.PIXEL_SIZE;
                    }
                    else
                        pos += movePos;
                    datasnippet.SetSpritePositon(spriteIndex, pos);
                }
                datasnippet.WriteParamData();
            }
            m_Size = new Vector2(tr.x - bl.x, tr.y - bl.y);
            if (IsEditor())
            {
                HudRichData data = m_pHudData as HudRichData;
                data.sizeDelta = m_Size;
            }
        }
        //--------------------------------------------------------
        private void ParseSimpleHtmlRichText(string text, ref List<RichSegment> segments)
        {
            Color curColor = Color.white;
            int pos = 0;
            while (pos < text.Length)
            {
                // <img=xxx,w=xx,h=xx,x=11,y=222/>
                var imgMatch = Regex.Match(
                    text,
                    @"<img=([^\s,/>]+)(?:,w=(\d+))?(?:,h=(\d+))?(?:,x=(-?\d+))?(?:,y=(-?\d+))?/>",
                    RegexOptions.None,
                    TimeSpan.FromMilliseconds(100)
                );
                if (imgMatch.Success && imgMatch.Index == pos)
                {
                    string imgName = imgMatch.Groups[1].Value;
                    float w = imgMatch.Groups[2].Success ? float.Parse(imgMatch.Groups[2].Value) : 0;
                    float h = imgMatch.Groups[3].Success ? float.Parse(imgMatch.Groups[3].Value) : 0;
                    float x = imgMatch.Groups[4].Success ? float.Parse(imgMatch.Groups[4].Value) : 0;
                    float y = imgMatch.Groups[5].Success ? float.Parse(imgMatch.Groups[5].Value) : 0;
                    segments.Add(new RichSegment(imgName, new Vector2(w, h), new Vector2(x, y), curColor));
                    pos += imgMatch.Length;
                    continue;
                }

                // <color=0xff0000ff>内容</color>
                var colorMatch = Regex.Match(text, @"<color=0x([0-9a-fA-F]{8})>(.*?)</color>", RegexOptions.Singleline, TimeSpan.FromMilliseconds(100));
                if (colorMatch.Success && colorMatch.Index == pos)
                {
                    string hex = colorMatch.Groups[1].Value;
                    string innerText = colorMatch.Groups[2].Value;
                    uint val = Convert.ToUInt32(hex, 16);
                    Color color = new Color32(
                        (byte)((val >> 24) & 0xFF),
                        (byte)((val >> 16) & 0xFF),
                        (byte)((val >> 8) & 0xFF),
                        (byte)(val & 0xFF)
                    );
                    segments.Add(new RichSegment(innerText, color));
                    pos += colorMatch.Length;
                    continue;
                }

                // 普通文本，直到下一个标签
                int nextTag = text.IndexOf('<', pos);
                if (nextTag == -1) nextTag = text.Length;
                string content = text.Substring(pos, nextTag - pos);
                if (!string.IsNullOrEmpty(content))
                    segments.Add(new RichSegment(content, curColor));
                pos = nextTag;
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
                        return new float2(-tr.x, movePos.y);
                    }
                case HorizontalAlignment.Middle:
                    {
                        float2 movePos = -(tr + bl) / 2;
                        return movePos;
                    }
                case HorizontalAlignment.Right:
                    {
                        float2 movePos = -(tr + bl) / 2;
                        return new float2(-bl.x, movePos.y);
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
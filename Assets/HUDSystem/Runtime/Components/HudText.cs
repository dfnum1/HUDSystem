/********************************************************************
生成日期:	11:11:2025
类    名: 	HudText
作    者:	HappLI
描    述:	文字
*********************************************************************/
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore;
using static PlasticGui.PlasticTableColumn;

namespace Framework.HUD.Runtime
{
    [HudData(typeof(HudTextData))]
    public class HudText : AComponent
    {
        protected const int QUAD_COUNT = 9;
        public HudText(HudSystem pSystem, HudBaseData hudData) : base(pSystem, hudData)
        {
            m_eHudType = EHudType.Text;
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
        internal void Refresh()
        {
            HudTextData hudTextData = m_pHudData as HudTextData;
            if (hudTextData == null || string.IsNullOrEmpty(hudTextData.text))
            {
                ResizeDataSnippet(0);
                return;
            }
            if (m_HudController.GetMaterial() == null)
                return;

            var expandFontAssets = m_HudController.GetFontAsset();
            if (expandFontAssets == null)
                return;
            char[] chars = hudTextData.text.ToCharArray();
            if (chars.Length == 0)
            {
                ResizeDataSnippet(0);
                return;
            }
            int charCount = math.min(4 * QUAD_COUNT, chars.Length);
            int snippetCount = (charCount - 1) / QUAD_COUNT + 1;
            ResizeDataSnippet(snippetCount);
            bool isUsingAlternativeTypeface;
            float padding = ShaderUtilities.GetPadding(m_HudController.GetMaterial(), false, false);
            float fontsize = hudTextData.fontSize / 10f;
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
                    if (curcharCount >= QUAD_COUNT && curcharCount % QUAD_COUNT == 0)
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
                    float advance = (character.glyph.metrics.horizontalAdvance + hudTextData.lineSpacing) * currentElementScale + hudTextData.lineSpacing;
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
        }
        //--------------------------------------------------------
        public float2 SetAlignment(float2 bl, float2 tr)
        {
            HudTextData hudTextData = m_pHudData as HudTextData;
            if (hudTextData == null)
                return bl;
            switch (hudTextData.alignment)
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

    }
}

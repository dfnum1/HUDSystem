/********************************************************************
生成日期:	11:11:2025
类    名: 	HudImage
作    者:	HappLI
描    述:	图片
*********************************************************************/

using Unity.Mathematics;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    [HudData(typeof(HudImageData))]
    public class HudImage : AComponent
    {
        public HudImage(HudSystem pSystem, HudBaseData hudData) : base(pSystem, hudData)
        {
            m_eHudType = EHudType.Image;
        }
        //--------------------------------------------------------
        public void SetSprite(Sprite sprite)
        {
            HudImageData hudImageData = m_pHudData as HudImageData;
            if(hudImageData == null)
            {
                UnityEngine.Debug.LogWarning(GetId() + " SetSprite Error: HudImageData is null!");
                return;
            }
            if (hudImageData.sprite == sprite)
                return;

            RefreshSpirte();
        }
        //--------------------------------------------------------
        public Sprite GetSprite()
        {
            HudImageData hudImageData = m_pHudData as HudImageData;
            if (hudImageData == null)
            {
                UnityEngine.Debug.LogWarning(GetId() + " GetSprite Error: HudImageData is null!");
                return null;
            }
            return hudImageData.sprite;
        }
        //--------------------------------------------------------
        void RefreshSpirte()
        {
            HudImageData hudImageData = m_pHudData as HudImageData;
            if (hudImageData == null)
                return;

            HudAtlas hudAtlas = GetHudAtlas();
            if (hudAtlas == null)
                return;

            switch (hudImageData.imageType)
            {
                case HudImageData.ImageType.Simple:
                    {
                        Simple(hudImageData.sprite, hudImageData);
                    }
                    break;
                case HudImageData.ImageType.Sliced:
                    {
                        Sliced(hudImageData.sprite, hudImageData);
                    }
                    break;
                case HudImageData.ImageType.Filled:
                    {
                        Filled(hudImageData.sprite, hudImageData);
                    }
                    break;
            }
        }
        //--------------------------------------------------------
        private void Simple(Sprite sprite, HudImageData hudData)
        {
            var spriteInfo = GetHudAtlas().GetSpriteInfo(sprite.name);
            if (spriteInfo == null) return;
            ResizeDataSnippet(1);
            HudDataSnippet snippet = GetDataSnippet(0);
            snippet.ResetNineParam();

            int quadindex = 0;
            snippet.SetSpriteId(quadindex, spriteInfo.index);
            snippet.SetSpritePositon(quadindex, new float2(-hudData.sizeDelta.x / 2, -hudData.sizeDelta.y / 2));
            snippet.SetSpriteSize(quadindex, hudData.sizeDelta);
            snippet.SetAmount(1, 0, 0);
            snippet.WriteParamData();
        }
        //--------------------------------------------------------
        private void Filled(Sprite sprite, HudImageData hudData)
        {
            var spriteInfo = GetHudAtlas().GetSpriteInfo(sprite.name);
            if (spriteInfo == null) return;
            ResizeDataSnippet(1);
            HudDataSnippet snippet = GetDataSnippet(0);
            snippet.ResetNineParam();
            int quadindex = 0;
            snippet.SetSpriteId(quadindex, spriteInfo.index);
            float2 spritePos = new float2(-hudData.sizeDelta.x / 2, -hudData.sizeDelta.y / 2);
            float2 spriteSize = hudData.sizeDelta;
            int method = (int)(hudData.fillMethod);
            spritePos[method] = spritePos[method] + spriteSize[method] * (1 - hudData.fillAmount) * hudData.fillOrigin;
            spriteSize[method] = spriteSize[method] * hudData.fillAmount;
            snippet.SetSpritePositon(quadindex, spritePos);
            snippet.SetSpriteSize(quadindex, spriteSize);
            snippet.SetAmount(hudData.fillAmount, hudData.fillOrigin, (int)hudData.fillMethod);
            snippet.WriteParamData();
        }
        //--------------------------------------------------------
        private Vector4 GetAdjustedBorders(Vector4 border, Rect adjustedRect)
        {
            Rect originalRect = adjustedRect;

            for (int axis = 0; axis <= 1; axis++)
            {
                float borderScaleRatio;
                if (originalRect.size[axis] != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
                float combinedBorders = border[axis] + border[axis + 2];
                if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0)
                {
                    borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }
            return border;
        }
        //--------------------------------------------------------
        static readonly Vector2[] s_VertScratch = new Vector2[4];
        private void Sliced(Sprite sprite, HudImageData hudData)
        {
            if (sprite == null) return;
            if (sprite.border.SqrMagnitude() > 0)
            {
                ResizeDataSnippet(1);
                HudDataSnippet snippet = GetDataSnippet(0);
                snippet.ResetNineParam();
                Vector4 border, padding;
                padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
                border = sprite.border;
                Rect rect = new Rect(-hudData.sizeDelta.x / 2, -hudData.sizeDelta.y / 2, hudData.sizeDelta.x, hudData.sizeDelta.y);
                Vector4 adjustedBorders = GetAdjustedBorders(border, rect);
                s_VertScratch[0] = new Vector2(padding.x, padding.y);
                s_VertScratch[3] = new Vector2(rect.width - padding.z, rect.height - padding.w);
                s_VertScratch[1].x = adjustedBorders.x;
                s_VertScratch[1].y = adjustedBorders.y;
                s_VertScratch[2].x = rect.width - adjustedBorders.z;
                s_VertScratch[2].y = rect.height - adjustedBorders.w;
                for (int i = 0; i < 4; ++i)
                {
                    s_VertScratch[i].x += rect.x;
                    s_VertScratch[i].y += rect.y;
                }
                int slicedIndex = 0;
                for (int x = 0; x < 3; ++x)
                {
                    int x2 = x + 1;
                    for (int y = 0; y < 3; ++y)
                    {
                        int y2 = y + 1;
                        string spriteName = sprite.name + "_" + slicedIndex;
                        var spriteInfo = GetHudAtlas().GetSpriteInfo(spriteName);
                        if (spriteInfo != null)
                        {
                            snippet.SetSpriteId(slicedIndex, spriteInfo.index);
                            float2 pos = new float2(s_VertScratch[x].x, s_VertScratch[y].y);
                            float2 maxpos = new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y);
                            snippet.SetSpritePositon(slicedIndex, pos);
                            snippet.SetSpriteSize(slicedIndex, maxpos - pos);
                            snippet.SetAmount(1, 0, 0);
                        }
                        slicedIndex++;
                    }
                }
                snippet.WriteParamData();
            }
            else
            {
                Simple(sprite, hudData);
            }
        }
    }
}

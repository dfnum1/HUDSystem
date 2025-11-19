/********************************************************************
生成日期:	11:11:2025
类    名: 	HudImage
作    者:	HappLI
描    述:	图片
*********************************************************************/

using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    [HudData(typeof(HudImageData))]
    public class HudImage : AWidget
    {
        enum EOverrideType : byte
        {
            ImageType = EParamOverrideType.Count,
            FillMethod,
            FillOrigin,
            FillAmount,
            Count,
        }
        Sprite m_Sprite;
        public HudImage(HudSystem pSystem, HudBaseData hudData) : base(pSystem, hudData)
        {
            m_eHudType = EHudType.Image;
        }
        //--------------------------------------------------------
        protected override void OnInit()
        {
            RefreshSpirte();
        }
        //--------------------------------------------------------
        public void SetSprite(Sprite sprite)
        {
            if (m_Sprite == sprite)
                return;
            m_Sprite = sprite;
            if (IsEditor())
            {
                HudImageData hudImageData = m_pHudData as HudImageData;
                hudImageData.sprite = sprite;
            }
            RefreshSpirte();
        }
        //--------------------------------------------------------
        public Sprite GetSprite()
        {
            return m_Sprite;
        }
        //--------------------------------------------------------
        protected override void OnSyncData()
        {
            HudImageData hudImageData = m_pHudData as HudImageData;
            m_Sprite = hudImageData.sprite;
        }
        //--------------------------------------------------------
        public void SetImageType(HudImageData.ImageType type)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.ImageType, (int)type);
            if(bDirty) RefreshSpirte();
        }
        //--------------------------------------------------------
        public HudImageData.ImageType GetImageType()
        {
            if (GetOverrideParam((byte)EOverrideType.ImageType, out var temp))
                return (HudImageData.ImageType)temp.intVal0;
            HudImageData hudImageData = m_pHudData as HudImageData;
            return hudImageData.imageType;
        }
        //--------------------------------------------------------
        public void SetFillMethod(HudImageData.FillMethod type)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.FillMethod, (int)type);
            if (bDirty) RefreshSpirte();
        }
        //--------------------------------------------------------
        public HudImageData.FillMethod GetFillMethod()
        {
            if (GetOverrideParam((byte)EOverrideType.FillMethod, out var temp))
                return (HudImageData.FillMethod)temp.intVal0;
            HudImageData hudImageData = m_pHudData as HudImageData;
            return hudImageData.fillMethod;
        }
        //--------------------------------------------------------
        public void SetFillOrigin(int type)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.FillOrigin, type);
            if (bDirty) RefreshSpirte();
        }
        //--------------------------------------------------------
        public int GetFillOrigin()
        {
            if (GetOverrideParam((byte)EOverrideType.FillOrigin, out var temp))
                return temp.intVal0;
            HudImageData hudImageData = m_pHudData as HudImageData;
            return hudImageData.fillOrigin;
        }
        //--------------------------------------------------------
        public void SetFillAmount(float amount)
        {
            amount = Mathf.Clamp01(amount);
            bool bDirty = SetOverrideParam((byte)EOverrideType.FillAmount, amount);
            if (bDirty) RefreshSpirte();
        }
        //--------------------------------------------------------
        public float GetFillAmount()
        {
            if (GetOverrideParam((byte)EOverrideType.FillAmount, out var temp))
                return temp.floatVal0;
            HudImageData hudImageData = m_pHudData as HudImageData;
            return hudImageData.fillOrigin;
        }
        //--------------------------------------------------------
        protected override void OnDirty()
        {
            RefreshSpirte();
        }
        //--------------------------------------------------------
        internal void RefreshSpirte()
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
                        Simple(GetSprite());
                    }
                    break;
                case HudImageData.ImageType.Sliced:
                    {
                        Sliced(GetSprite());
                    }
                    break;
                case HudImageData.ImageType.Filled:
                    {
                        Filled(GetSprite());
                    }
                    break;
            }
        }
        //--------------------------------------------------------
        private void Simple(Sprite sprite)
        {
            int spriteIndex = -1;
            HudAtlas.SpriteInfo spriteInfo = null;
            if(sprite !=null)
            {
                spriteInfo = GetHudAtlas().GetSpriteInfo(sprite.name);
                if (spriteInfo != null) spriteIndex = spriteInfo.index;
            }
            ResizeDataSnippet(1);
            HudDataSnippet snippet = GetDataSnippet(0);
            snippet.ResetNineParam();

            Vector2 size = GetSize();

            int quadindex = 0;
            snippet.SetSpriteId(quadindex, spriteIndex);
            snippet.SetSpritePositon(quadindex, new float2(-size.x / 2, -size.y / 2));
            snippet.SetSpriteSize(quadindex, size);
            snippet.SetAmount(1, 0, 0);
            snippet.WriteParamData();
        }
        //--------------------------------------------------------
        private void Filled(Sprite sprite)
        {
            int spriteIndex = -1;
            HudAtlas.SpriteInfo spriteInfo = null;
            if (sprite != null)
            {
                spriteInfo = GetHudAtlas().GetSpriteInfo(sprite.name);
                if (spriteInfo != null) spriteIndex = spriteInfo.index;
            }

            Vector2 size = GetSize();
            int fillMethod = (int)GetFillMethod();
            float fillAmount = GetFillAmount();
            int nFillOrigin = GetFillOrigin();

            ResizeDataSnippet(1);
            HudDataSnippet snippet = GetDataSnippet(0);
            snippet.ResetNineParam();
            int quadindex = 0;
            snippet.SetSpriteId(quadindex, spriteIndex);
            float2 spritePos = new float2(-size.x / 2, -size.y / 2);
            float2 spriteSize = size;
            int method = fillMethod;
            spritePos[method] = spritePos[method] + spriteSize[method] * (1 - fillAmount) * nFillOrigin;
            spriteSize[method] = spriteSize[method] * fillAmount;
            snippet.SetSpritePositon(quadindex, spritePos);
            snippet.SetSpriteSize(quadindex, spriteSize);
            snippet.SetAmount(fillAmount, nFillOrigin, fillMethod);
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
        private void Sliced(Sprite sprite)
        {
            if (sprite!=null && sprite.border.SqrMagnitude() > 0)
            {
                Vector2 size = GetSize();
                ResizeDataSnippet(1);
                HudDataSnippet snippet = GetDataSnippet(0);
                snippet.ResetNineParam();
                Vector4 border, padding;
                padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
                border = sprite.border;
                Rect rect = new Rect(-size.x / 2, -size.y / 2, size.x, size.y);
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
                Simple(sprite);
            }
        }
    }
}

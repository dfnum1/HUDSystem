using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using Unity.Profiling;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR && UNITY_2018_4_OR_NEWER && !UNITY_2018_4_0 && !UNITY_2018_4_1 && !UNITY_2018_4_2 && !UNITY_2018_4_3 && !UNITY_2018_4_4
using UnityEditor.TextCore.LowLevel;
#endif

namespace TMPro
{
    [Serializable]
    [ExcludeFromPresetAttribute]
    public class TMP_ExpandFontAssets : TMP_FontAsset
    {
        private TMP_FontAsset parentFont;
        private TMP_FontAsset orginFont;

        public override Texture2D[] atlasTextures
        {
            get 
            {
                if (parentFont == null) return base.atlasTextures;
                return parentFont.atlasTextures;
            }
            set 
            {
                if (parentFont == null) 
                {
                    base.atlasTextures = value;
                    return;
                }
                parentFont.atlasTextures = value;
            }
        }

        public override Texture2D fontMappingTexture
        {
            get 
            {
                if (parentFont == null) return base.fontMappingTexture;
                return parentFont.fontMappingTexture; 
            }
        }

        internal override List<GlyphRect> usedGlyphRects
        {
            get 
            {
                if (parentFont == null) return base.usedGlyphRects;
                return parentFont.usedGlyphRects;
            }
            set 
            {
                if (parentFont == null)
                {
                    base.usedGlyphRects = value;
                    return;
                }
                parentFont.usedGlyphRects = value;
            }
        }

        internal override List<GlyphRect> freeGlyphRects
        {
            get 
            {
                if (parentFont == null)
                    return base.freeGlyphRects;
                return parentFont.freeGlyphRects;
            }
            set
            {
                if (parentFont == null)
                {
                    base.freeGlyphRects = value;
                    return;
                }
                parentFont.freeGlyphRects = value;
            }
        }

        internal override int atlasTextureIndex
        {
            get
            {
                if (parentFont == null) return base.atlasTextureIndex;
                return parentFont.atlasTextureIndex; 
            }
            set
            {
                if(parentFont == null)
                {
                    base.atlasTextureIndex = value;
                    return;
                }
                parentFont.atlasTextureIndex = value;
            }
        }

        public override List<TMP_Character> fontCharacter
        {
            get
            {
                if (parentFont == null) return base.fontCharacter;
                return parentFont.fontCharacter; 
            }
        }

        protected override void OnEnable()
        {
        }

        public static TMP_ExpandFontAssets CreateExpandFontAssets(string fontpath, TMP_FontAsset parentFont)
        {
            FontEngine.InitializeFontEngine();

            if (FontEngine.LoadFontFace(fontpath, parentFont.faceInfo.pointSize) != FontEngineError.Success)
            {
                Debug.LogWarning("Unable to load font face for [" + fontpath + "]. Make sure \"Include Font Data\" is enabled in the Font Import Settings.");
                return null;
            }

            TMP_ExpandFontAssets fontAsset = CreateInstance<TMP_ExpandFontAssets>();
            fontAsset.parentFont = parentFont;
            fontAsset.version = parentFont.version;
            fontAsset.faceInfo = FontEngine.GetFaceInfo();

            if (parentFont.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                fontAsset.sourceFontPath = fontpath;

            fontAsset.atlasPopulationMode = parentFont.atlasPopulationMode;

            fontAsset.atlasWidth = parentFont.atlasWidth;
            fontAsset.atlasHeight = parentFont.atlasHeight;
            fontAsset.atlasPadding = parentFont.atlasPadding;
            fontAsset.atlasRenderMode = parentFont.atlasRenderMode;
            fontAsset.isMultiAtlasTexturesEnabled = parentFont.isMultiAtlasTexturesEnabled;
            fontAsset.material = parentFont.material;
            fontAsset.ReadFontAssetDefinition();

            return fontAsset;
        }

        public static TMP_ExpandFontAssets CreateExpandFontAssets(TMP_FontAsset orginFont, TMP_FontAsset parentFont)
        {
            FontEngine.InitializeFontEngine();
            if (FontEngine.LoadFontFace(orginFont.sourceFontFile, parentFont.faceInfo.pointSize) != FontEngineError.Success)
            {
                Debug.LogWarning("Unable to load font face for [" + orginFont.sourceFontFile.name + "]. Make sure \"Include Font Data\" is enabled in the Font Import Settings.");
                return null;
            }

            TMP_ExpandFontAssets fontAsset = CreateInstance<TMP_ExpandFontAssets>();
            fontAsset.parentFont = parentFont;
            fontAsset.orginFont = orginFont;
            fontAsset.version = parentFont.version;
            fontAsset.faceInfo = FontEngine.GetFaceInfo();

            fontAsset.atlasPopulationMode = parentFont.atlasPopulationMode;

            if (parentFont.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                fontAsset.sourceFontFile = orginFont.sourceFontFile;

            fontAsset.atlasWidth = parentFont.atlasWidth;
            fontAsset.atlasHeight = parentFont.atlasHeight;
            fontAsset.atlasPadding = orginFont.atlasPadding;
            fontAsset.atlasRenderMode = orginFont.atlasRenderMode;
            fontAsset.isMultiAtlasTexturesEnabled = parentFont.isMultiAtlasTexturesEnabled;
            fontAsset.material = parentFont.material;
            fontAsset.ReadFontAssetDefinition();

            return fontAsset;
        }

        private static Dictionary<TMP_FontAsset, TMP_ExpandFontAssets> toFontAssetCache;

        public static TMP_FontAsset ToExpandFontAssets(TMP_FontAsset orginFontAsset)
        {
            if (orginFontAsset == TMP_Settings.defaultFontAsset) return orginFontAsset;

            if (toFontAssetCache == null) toFontAssetCache = new Dictionary<TMP_FontAsset, TMP_ExpandFontAssets>();
            TMP_ExpandFontAssets sysfontAsset = null;
            if (toFontAssetCache.TryGetValue(orginFontAsset, out sysfontAsset))
            {
                return sysfontAsset;
            }
            sysfontAsset = TMP_ExpandFontAssets.CreateExpandFontAssets(orginFontAsset, TMP_Settings.defaultFontAsset);
            toFontAssetCache[orginFontAsset] = sysfontAsset;
            return sysfontAsset;
        }
    }
}
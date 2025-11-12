/********************************************************************
生成日期:	11:11:2025
类    名: 	HudRenderBatch
作    者:	HappLI
描    述:	
*********************************************************************/
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Framework.HUD.Runtime
{
    public class HudRenderBatch
    {
        HudSystem m_pSystem;
        private Material m_pMaterial;
        private Mesh m_pMesh;
        private HudAtlas m_pAtlasMapping;
        private TMP_FontAsset m_pFontAsset;

        int m_nInstanceCount = 0;
        private MaterialPropertyBlock m_MaterialPropertyBlock;

        private HashSet<HudCanvasRender> m_vHudCanvasRenderer;
        public HudRenderBatch(HudSystem pSystem, Material material, Mesh mesh, HudAtlas atlasMapping, TMP_FontAsset fontAsset)
        {
            m_pSystem = pSystem;
            m_nInstanceCount = 0;
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
            m_pMaterial = material;
            m_pMesh = mesh;
            m_pFontAsset = fontAsset;
            SetAltas(atlasMapping);
            SetFontAsset(fontAsset);
        }
        //-----------------------------------------------------
        internal void SetAltas(HudAtlas atlas)
        {
            if (m_pAtlasMapping == atlas)
                return;
            m_pAtlasMapping = atlas;
            if (atlas != null)
            {
                m_pAtlasMapping.GenAtlasMappingInfo();
                if (m_pMaterial&& m_pMaterial.HasProperty(HUDUtils._AtlasTex))
                {
                    m_MaterialPropertyBlock.SetTexture(HUDUtils._AtlasTex, m_pAtlasMapping.atlasTex);
                    m_MaterialPropertyBlock.SetInt(HUDUtils._AtlasWidth, m_pAtlasMapping.width);
                    m_MaterialPropertyBlock.SetInt(HUDUtils._AtlasHeight, m_pAtlasMapping.height);

                    m_MaterialPropertyBlock.SetTexture(HUDUtils._AtlasMappingTex, m_pAtlasMapping.atlasMappingTex);
                    m_MaterialPropertyBlock.SetInt(HUDUtils._AtlasMappingWidth, m_pAtlasMapping.atlasMappingWidth);
                    m_MaterialPropertyBlock.SetInt(HUDUtils._AtlasMappingHeight, m_pAtlasMapping.atlasMappingHeight);
                }
            }
        }
        //-----------------------------------------------------
        internal void SetFontAsset(TMP_FontAsset fontAsset)
        {
            if (m_pFontAsset == fontAsset)
                return;
            m_pFontAsset = fontAsset;
            if (fontAsset != null)
            {
                int fontTexId = Shader.PropertyToID("_MainTex");
                if (m_pMaterial && m_pMaterial.HasProperty(fontTexId))
                {
                  //      TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;
                    m_MaterialPropertyBlock.SetTexture(HUDUtils._MainTex, fontAsset.atlasTexture);
                    m_MaterialPropertyBlock.SetInt(HUDUtils._TextureWidth, fontAsset.atlasWidth);
                    m_MaterialPropertyBlock.SetInt(HUDUtils._TextureHeight, fontAsset.atlasHeight);

                    m_MaterialPropertyBlock.SetTexture(HUDUtils._FontMappingTex, fontAsset.fontMappingTexture);
                    m_MaterialPropertyBlock.SetInt(HUDUtils._FontMappingWidth, fontAsset.fontMappingWidth);
                    m_MaterialPropertyBlock.SetInt(HUDUtils._FontMappingHeight, fontAsset.fontMappingHeight);
                }
            }
        }
        //-----------------------------------------------------
        internal void Render()
        {
            m_nInstanceCount = m_vHudCanvasRenderer.Count;
            int renderCount = m_nInstanceCount / HUDUtils.batchMaxCount;
            int lasterRenderCount = m_nInstanceCount % HUDUtils.batchMaxCount;

            int index = 0;
            for(int i =0;i < renderCount; ++i)
            {
                Matrix4x4[] matrixs = ;
            }
        }
        //-----------------------------------------------------
        public void DrawMeshInstanced(Mesh mesh, Material material, Matrix4x4[] matrix4x4, int count, MaterialPropertyBlock properties)
        {
            m_pSystem.DrawMeshInstanced(mesh, material, matrix4x4, count, properties);
        }
    }
}

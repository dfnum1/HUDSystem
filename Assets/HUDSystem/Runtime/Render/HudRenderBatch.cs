/********************************************************************
生成日期:	11:11:2025
类    名: 	HudRenderBatch
作    者:	HappLI
描    述:	
*********************************************************************/
using System.Collections.Generic;
using TMPro;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Framework.HUD.Runtime
{
    public unsafe class HudRenderBatch
    {
        HudSystem m_pSystem;
        private Material m_pMaterial;
        private Mesh m_pMesh;
        private HudAtlas m_pAtlasMapping;
        private TMP_FontAsset m_pFontAsset;

        int m_nInstanceCount = 0;
        private MaterialPropertyBlock m_MaterialPropertyBlock;

        private HudRenderCulling m_Culling;
        private HudInstanceCollector m_pCollector;
        private HashSet<HudController> m_vHudControllers;
        JobHandle m_pJobHandle;
        CommandBuffer m_CommandBuffer = null;
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

            m_Culling = new HudRenderCulling(HUDUtils.capacity);
            m_pCollector = new HudInstanceCollector(HUDUtils.capacity);

            m_pJobHandle = new JobHandle();
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
        internal void Render(Camera camera)
        {
            if (!m_pJobHandle.IsCompleted)
            {
                m_pJobHandle.Complete();
            }
            m_nInstanceCount = m_pCollector.instanceCount;
            int renderCount = m_nInstanceCount / HUDUtils.batchMaxCount;
            int lasterRenderCount = m_nInstanceCount % HUDUtils.batchMaxCount;

            int index = 0;
            if (m_pCollector.expansion)
            {
                m_pCollector.expansion = false;
                m_pCollector.TriggerNotif();
            }

            m_CommandBuffer?.Clear();
            for (int i = 0; i < renderCount; i++)
            {
                Profiler.BeginSample("DrawMeshInstanced " + i);
                Matrix4x4[] matrix4x4 = m_pCollector.GetObjectToWorld(index);
                m_pCollector.FullPropertyBlack(index, m_MaterialPropertyBlock);
                DrawMeshInstanced(matrix4x4, HUDUtils.batchMaxCount, m_MaterialPropertyBlock, camera);
                Profiler.EndSample();
                index++;
            }
            {
                if (lasterRenderCount > 0)
                {
                    Matrix4x4[] matrix4x4 = m_pCollector.GetObjectToWorld(index);
                    Profiler.BeginSample("DrawMeshInstanced Last");
                    m_pCollector.FullPropertyBlack(index, m_MaterialPropertyBlock);
                    DrawMeshInstanced(matrix4x4, lasterRenderCount, m_MaterialPropertyBlock, camera);
                    Profiler.EndSample();
                }
            }
        }
        //-----------------------------------------------------
        internal void LateUpdate()
        {
            if (!m_pJobHandle.IsCompleted)
                return;
            
            if (m_vHudControllers != null && m_vHudControllers.Count > 0)
            {
                foreach (var item in m_vHudControllers)
                    item.OnReorder();
                m_vHudControllers.Clear();
            }

            Profiler.BeginSample("ToJob");
            JobHandle transformJobHandle = m_Culling.ToJob(m_pSystem.CameraVP, m_pSystem.CameraDirection);
            m_pJobHandle = m_pCollector.ToJob(m_Culling, transformJobHandle);
            Profiler.EndSample();
        }
        //-----------------------------------------------------
        public void AddExpansionNotif(System.Action notify)
        {
            m_pCollector.AddExpansionNotif(notify);
        }
        //-----------------------------------------------------
        public void RemoveExpansionNotif(System.Action notify)
        {
            m_pCollector.RemoveExpansionNotif(notify);
        }
        //-----------------------------------------------------
        public void TriggerReorder(HudController controller)
        {
            if (m_vHudControllers == null) m_vHudControllers = new HashSet<HudController>(128);
            if (m_vHudControllers.Contains(controller)) return;
            m_vHudControllers.Add(controller);
        }
        //-----------------------------------------------------
        public int AddHudController(HudController controller, bool bRoot)
        {
            return m_Culling.Add(controller, bRoot);
        }
        //-----------------------------------------------------
        public void RemoveHudController(int id)
        {
            m_Culling.Remove(id);
        }
        //-----------------------------------------------------
        public void RemoveHudData(int hash)
        {
            m_pCollector.Remove(hash);
        }
        //-----------------------------------------------------
        public void SetControllerEnable(int index)
        {
            m_Culling.SetEnable(index);
        }
        //-----------------------------------------------------
        public void SetControllerDisable(int index)
        {
            m_Culling.SetDisable(index);
        }
        //-----------------------------------------------------
        public void SetBounds(int index, float2 center, float2 size)
        {
            m_Culling.SetBounds(index, center, size);
        }
        //-----------------------------------------------------
        public int AddFloat4x4(string name, int hashcode, RenderDataState<float4x4> value)
        {
            return m_pCollector.AddFloat4x4(name, hashcode, value);
        }
        //-----------------------------------------------------
        public void SetFloat4x4(string name, int idx, RenderDataState<float4x4> value)
        {
            m_pCollector.SetFloat4x4(name, idx, value);
        }
        //-----------------------------------------------------
        public int AddTransformId(int hashcode, RenderDataState<int> transformId)
        {
            return m_pCollector.AddTransformId(hashcode, transformId);
        }
        //-----------------------------------------------------
        public void SetTransformId(int idx, RenderDataState<int> transformId)
        {
            m_pCollector.SetTransformId(idx, transformId);
        }
        //-----------------------------------------------------
        public void DrawMeshInstanced(Matrix4x4[] matrix4x4, int count, MaterialPropertyBlock properties, Camera camera)
        {
            if(m_CommandBuffer == null)
            {
                m_CommandBuffer = new CommandBuffer();
                m_CommandBuffer.name = "HudRenderBatch";
                m_pSystem.OnCreateComandBuffer(m_CommandBuffer);
            }
#if UNITY_EDITOR
            Graphics.DrawMeshInstanced(m_pMesh, 0, m_pMaterial, matrix4x4, count, properties, ShadowCastingMode.Off, false, 0, camera);
#else
            m_CommandBuffer.DrawMeshInstanced(m_pMesh, 0, m_pMaterial, 0, matrix4x4, count, properties, 0, camera);
#endif
        }
        //-----------------------------------------------------
        internal void OnChangeCamera(Camera last, Camera cur)
        {
            if (m_CommandBuffer == null)
                return;
            if (last) last.RemoveCommandBuffer( CameraEvent.AfterForwardAlpha, m_CommandBuffer);
            if(cur) cur.AddCommandBuffer(CameraEvent.AfterForwardAlpha, m_CommandBuffer);
        }
        //-----------------------------------------------------
        public void Destroy()
        {
            if (!m_pJobHandle.IsCompleted) m_pJobHandle.Complete();
            m_Culling.Dispose();
            m_pCollector.Dispose();
        }
    }
}

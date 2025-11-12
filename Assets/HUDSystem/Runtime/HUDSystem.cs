/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDSystem
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GridBrushBase;

namespace Framework.HUD.Runtime
{
    public class HudSystem
    {
        Camera m_pRenderCamera = null;
        CommandBuffer m_CommandBuffer = null;
        private Dictionary<int, HudRenderBatch> m_vRenders = new Dictionary<int, HudRenderBatch>(2);
        //--------------------------------------------------------
        public HudSystem()
        {
        }
        //--------------------------------------------------------
        public void SetRenderCamera(Camera camera)
        {
            if (m_pRenderCamera == camera)
                return;
            if( m_CommandBuffer!=null)
            {
                if(m_pRenderCamera!=null) m_pRenderCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, m_CommandBuffer);
                if(camera!=null) m_pRenderCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, m_CommandBuffer);
            }
            m_pRenderCamera = camera;
        }
        //--------------------------------------------------------
        public HudRenderBatch GetRenderBatch(Material material, Mesh mesh, HudAtlas atlas, TMP_FontAsset fontAsset)
        {
            int hashCode = GetHashCode(material, mesh, atlas);
            if (hashCode == 0) return null;
            HudRenderBatch renderBatcher;
            if (!m_vRenders.TryGetValue(hashCode, out renderBatcher))
            {
                renderBatcher = new HudRenderBatch(this,material, mesh, atlas, fontAsset);
                m_vRenders[hashCode] = renderBatcher;
            }
            return renderBatcher;
        }
        //--------------------------------------------------------
        public void EditorRender()
        {

        }
        //--------------------------------------------------------
        public void Update()
        {
        }
        //--------------------------------------------------------
        public void Render()
        {
            BeginRender();
            foreach(var db in m_vRenders)
            {
                db.Value.Render();
            }
            EndRender();
        }
        //--------------------------------------------------------
        public void Destroy()
        {
            if (m_pRenderCamera != null && m_CommandBuffer!=null)
                m_pRenderCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, m_CommandBuffer);
        }
        //--------------------------------------------------------
        public int GetHashCode(Material _material, Mesh _mesh, HudAtlas _atlasMapping)
        {
            if (_material == null || _mesh == null) return 0;
            int hashCode = _material.GetHashCode() ^ _mesh.GetHashCode();
            if (_atlasMapping != null)
            {
                hashCode = hashCode ^ _atlasMapping.GetHashCode();
            }
            return hashCode;
        }
        //--------------------------------------------------------
        internal void BeginRender()
        {
            if(m_pRenderCamera == null)
            {
                UnityEngine.Debug.LogError("HudSystem BeginRender Error: Render Camera is null!");
                return;
            }
            if (m_CommandBuffer != null) m_CommandBuffer.Clear();
            else
            {
                m_CommandBuffer = new CommandBuffer();
                m_CommandBuffer.name = "HudRenderBatch";
                m_pRenderCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, m_CommandBuffer);
            }
        }
        //--------------------------------------------------------
        internal void DrawMeshInstanced( Mesh mesh, Material material, Matrix4x4[] matrix4x4, int count, MaterialPropertyBlock properties)
        {
#if UNITY_EDITOR
            Graphics.DrawMeshInstanced(mesh, 0, material, matrix4x4, count, properties, ShadowCastingMode.Off, false);
#else
            m_CommandBuffer.DrawMeshInstanced(commandBuffer,mesh, 0, material, 0, matrix4x4, count, properties);
#endif
        }
        //--------------------------------------------------------
        internal void EndRender()
        {
            if (m_CommandBuffer != null) m_CommandBuffer.Clear();
        }
    }
}

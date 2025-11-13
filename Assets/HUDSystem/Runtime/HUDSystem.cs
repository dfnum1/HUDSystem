/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDSystem
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Framework.HUD.Runtime
{
    public class HudSystem
    {
        Camera                                  m_pRenderCamera = null;
        Transform                               m_pRenderCameraTransform = null;
        private List<HudController>             m_vHuds = new List<HudController>(64);
        private Dictionary<int, HudRenderBatch> m_vRenders = new Dictionary<int, HudRenderBatch>(2);
        //--------------------------------------------------------
        public HudSystem()
        {
        }
        //--------------------------------------------------------
        public float4x4 CameraVP
        {
            get
            {
                if (m_pRenderCamera == null) return Matrix4x4.identity;
                float4x4 vp = math.mul(m_pRenderCamera.projectionMatrix, m_pRenderCamera.worldToCameraMatrix);
                return vp;
            }
        }
        //--------------------------------------------------------
        public float3 CameraDirection
        {
            get
            {
                if (m_pRenderCameraTransform == null) return Vector3.forward;
                return m_pRenderCameraTransform.forward;
            }
        }
        //--------------------------------------------------------
        public void SetRenderCamera(Camera camera)
        {
            if (m_pRenderCamera == camera)
                return;
            foreach (var db in m_vRenders)
                db.Value.OnChangeCamera(m_pRenderCamera, camera);
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
        public HudController CreateHud(HudObject hudObj)
        {
            HudController hud = new HudController(this);
            hud.SetHudObject(hudObj);
            m_vHuds.Add(hud);
            return hud;
        }
        //--------------------------------------------------------
        public void DeleteHud(HudController hudController)
        {
            hudController.Destroy();
            m_vHuds.Remove(hudController);
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
        public void LateUpdate()
        {
            foreach (var db in m_vRenders)
            {
                db.Value.LateUpdate();
            }
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
            SetRenderCamera(null);
            foreach (var db in m_vRenders)
            {
                db.Value.Destroy();
            }
            m_vRenders.Clear();
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
        }
        //--------------------------------------------------------
        internal void OnCreateComandBuffer(CommandBuffer cmdBuffer)
        {
            if (m_pRenderCamera != null)
                m_pRenderCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, cmdBuffer);
        }
        //--------------------------------------------------------
        internal void EndRender()
        {
        }
    }
}

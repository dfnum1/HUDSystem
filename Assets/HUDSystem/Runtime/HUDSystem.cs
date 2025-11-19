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
    public interface IHudSystemCallback
    {
        bool OnSpawnInstance(AWidget pWidget, string strParticle, System.Action<GameObject> onCallback);
        bool OnDestroyInstance(AWidget pWidget, GameObject pGameObject);
    }
    public class HudSystem
    {
        Camera                                  m_pRenderCamera = null;
        Transform                               m_pRenderCameraTransform = null;
        private List<HudController>             m_vHuds = new List<HudController>(64);
        private Dictionary<int, HudRenderBatch> m_vRenders = new Dictionary<int, HudRenderBatch>(2);
        private List<AWidget>                   m_vRayTest = null;
        private List<IHudSystemCallback>        m_vCallbacks = null;
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
            if (camera) m_pRenderCameraTransform = camera.transform;
            else m_pRenderCameraTransform = null;
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
        public void RegisterCallback(IHudSystemCallback callback)
        {
            if (callback == null) return;
            if (m_vCallbacks == null) m_vCallbacks = new List<IHudSystemCallback>(2);
            if (m_vCallbacks.Contains(callback))
                return;
            m_vCallbacks.Add(callback);
        }
        //--------------------------------------------------------
        public void UnRegisterCallback(IHudSystemCallback callback)
        {
            if (m_vCallbacks == null) return;
            m_vCallbacks.Remove(callback);
        }
        //--------------------------------------------------------
        internal void SpawnInstance(AWidget pWidget, string file, System.Action<GameObject> callback)
        {
            if (m_vCallbacks == null)
                return;
            foreach(var db in m_vCallbacks)
            {
                if (db.OnSpawnInstance(pWidget, file, callback))
                    return;
            }
        }
        //--------------------------------------------------------
        internal void DestroyInstance(AWidget pWidget, GameObject pGo)
        {
            if (pGo == null)
                return;
            if (m_vCallbacks == null || m_vCallbacks.Count <= 0)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    GameObject.Destroy(pGo);
                }
                else GameObject.DestroyImmediate(pGo);
#else
                GameObject.Destroy(pGo);
#endif
                return;
            }
            foreach (var db in m_vCallbacks)
            {
                if (db.OnDestroyInstance(pWidget, pGo))
                    return;
            }
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
        internal List<AWidget> GetRayTestCache()
        {
            if (m_vRayTest == null) m_vRayTest = new List<AWidget>(2);
            return m_vRayTest;
        }
        //--------------------------------------------------------
        public AWidget RaycastHud(Vector2 screenPosition, bool bIngoreRayTest = false)
        {
            if (m_pRenderCamera == null) return null;

            if (m_vHuds == null) return null;
            if (m_vRayTest != null) m_vRayTest.Clear();
            foreach (var hud in m_vHuds)
            {
                hud.RaycastHud(screenPosition, m_pRenderCamera,null, bIngoreRayTest);
            }
            if (m_vRayTest == null || m_vRayTest.Count <= 0) return null;
            if (m_vRayTest.Count > 1)
            {
                m_vRayTest.Sort((w1, w2) =>
                {
                    return w1.GetTagZ().CompareTo(w2.GetTagZ());
                });
            }
            AWidget widget = m_vRayTest[0];
            m_vRayTest.Clear();
            return widget;
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
                db.Value.Render(m_pRenderCamera);
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

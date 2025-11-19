/********************************************************************
生成日期:	11:11:2025
类    名: 	HudController
作    者:	HappLI
描    述:	HUD 控制器
*********************************************************************/
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    public class HudController
    {
        private HudSystem m_pSystem;
        private HudObject m_pObject;
        private List<AWidget> m_vWidgets = null;
        private HudRenderBatch m_RenderBatch = null;

        private Transform m_pFollowTarget;
        private Matrix4x4 m_pFollowTransform = Matrix4x4.identity;

        private Vector3 m_Offset = Vector3.zero;
        private Vector3 m_OffsetRotation = Vector3.zero;
        private int m_nTransId = -1;
#if UNITY_EDITOR
        internal bool m_bEditorMode = false;
#endif
        //--------------------------------------------------------
        public HudController(HudSystem pSystem)
        {
            m_pSystem = pSystem;
        }
        //--------------------------------------------------------
        public void SetEditorMode(bool bEditor)
        {
#if UNITY_EDITOR
            m_bEditorMode = bEditor;
#endif
        }
        //--------------------------------------------------------
        public bool IsEditorMode()
        {
#if UNITY_EDITOR
            return m_bEditorMode;
#else
            return false;
#endif
        }
        //--------------------------------------------------------
        public void SetFollowTarget(Transform target)
        {
            m_pFollowTarget = target;
        }
        //--------------------------------------------------------
        public Transform GetFollowTarget()
        {
            return m_pFollowTarget;
        }
        //--------------------------------------------------------
        public void SetFollowTransform(Matrix4x4 transform)
        {
            m_pFollowTransform = transform;
            m_pFollowTarget = null;
        }
        //--------------------------------------------------------
        public Matrix4x4 GetWorldMatrix()
        {
            if(m_pFollowTarget)
            {
                m_pFollowTransform = m_pFollowTarget.localToWorldMatrix;
            }
            return m_pFollowTransform*Matrix4x4.TRS(m_Offset, Quaternion.Euler(m_OffsetRotation), Vector3.one);
        }
        //--------------------------------------------------------
        public void UpdateTransform()
        {
            if (m_RenderBatch != null && m_nTransId >= 0)
            {
                m_RenderBatch.UpdateHudController(m_nTransId, this);
                m_RenderBatch.UpdateFontMaterial();
            }
        }
        //--------------------------------------------------------
        public HudRenderBatch renderBatch
        {
            get { return m_RenderBatch; }
        }
        //--------------------------------------------------------
        public HudAtlas GetAtlas()
        {
            if (m_pObject == null) return null;
            return m_pObject.atlasAset;
        }
        //--------------------------------------------------------
        public TMP_FontAsset GetFontAsset()
        {
            if (m_pObject == null) return null;
            return m_pObject.fontAsset;
        }
        //--------------------------------------------------------
        public Material GetMaterial()
        {
            if (m_pObject == null) return null;
            return m_pObject.material;
        }
        //--------------------------------------------------------
        public int GetTransId()
        {
            return m_nTransId;
        }
        //--------------------------------------------------------
        public void SetHudObject(HudObject hudObject)
        {
            if (m_pObject == hudObject)
                return;
            m_pObject = hudObject;

            if(m_vWidgets != null) m_vWidgets.Clear();
            if (m_pObject == null || m_pObject.vHierarchies == null)
                return;

            if(m_RenderBatch == null)
                m_RenderBatch = m_pSystem.GetRenderBatch(hudObject.material, hudObject.mesh, hudObject.atlasAset, hudObject.fontAsset);

            if (m_RenderBatch != null)
            {
                m_RenderBatch.AddExpansionNotif(TriggerReorder);
                if (m_nTransId != 0)
                {
                    m_RenderBatch.RemoveHudData(m_nTransId);
                    m_RenderBatch.RemoveHudController(m_nTransId);
                }
                m_nTransId = m_RenderBatch.AddHudController(this, true);
            }

            foreach (var db in m_pObject.vHierarchies)
            {
                CreateHierarchy(null, db);
            }

            foreach (var db in m_vWidgets)
            {
                db.Init();
            }

            if (m_RenderBatch != null)
            {
                m_RenderBatch.SetBounds(m_nTransId, hudObject.center, hudObject.size);
            }
            TriggerReorder();
        }
        //--------------------------------------------------------
        public List<AWidget> GetWidgets()
        {
            return m_vWidgets;
        }
        //--------------------------------------------------------
        internal void OnReorder()
        {
            if (m_vWidgets == null)
                return;

            if(m_nTransId>=0 && m_RenderBatch !=null)
            {
                m_RenderBatch.RemoveHudData(m_nTransId);
            }

            m_vWidgets.Sort((w1, w2) =>
            {
                return w1.GetTagZ().CompareTo(w2.GetTagZ());
            });
            foreach (var db in m_vWidgets)
            {
                db.OnReorder();
            }
        }
        //--------------------------------------------------------
        public void RemoveComponent(AWidget pComp)
        {
            if (pComp == null) return;
            if(pComp is HudCanvas)
                m_vWidgets.Remove((HudCanvas)pComp);
        }
        //--------------------------------------------------------
        internal void Destroy()
        {
            if(m_RenderBatch!=null) m_RenderBatch.RemoveExpansionNotif(TriggerReorder);
            m_RenderBatch = null;
            if (m_nTransId >= 0 && m_RenderBatch != null)
            {
                m_RenderBatch.RemoveHudData(m_nTransId);
            }
        }
        //--------------------------------------------------------
        internal void TriggerReorder()
        {
            if (m_RenderBatch == null) return;
            m_RenderBatch.TriggerReorder(this);
        }
        //--------------------------------------------------------
        public void SetDirty()
        {
            if (m_vWidgets == null)
                return;
            foreach (var db in m_vWidgets)
            {
                db.SetDirty();
            }
        }
        //--------------------------------------------------------
        public AWidget RaycastHud(Vector2 screenPosition, Camera  camera, List<AWidget> vResults = null)
        {
            if (camera == null || m_vWidgets==null) return null;

            var rayTest = m_pSystem.GetRayTestCache();
            rayTest.Clear();

            foreach (var comp in m_vWidgets)
            {
                RaycastHudRecursive(rayTest,comp, screenPosition, camera);
            }
            if (rayTest.Count <= 0) return null;
            if (vResults != null)
            {
                vResults.AddRange(rayTest);
            }
            if (rayTest.Count > 1)
            {
                rayTest.Sort((w1, w2) =>
                {
                    return w1.GetTagZ().CompareTo(w2.GetTagZ());
                });
            }
            AWidget widget = rayTest[0];
            rayTest.Clear();
            return widget;
        }
        //--------------------------------------------------------
        void RaycastHudRecursive(List<AWidget> vRayTest, AWidget widget, Vector2 screenPosition, Camera camera)
        {
            var data = widget.GetData();
            if (data == null || !data.rayTest) return;
            Vector3 worldPos = camera.WorldToScreenPoint(GetWorldMatrix()*widget.GetPosition());
            Rect rect = new Rect(worldPos.x - data.sizeDelta.x * 0.5f, worldPos.y - data.sizeDelta.y * 0.5f, data.sizeDelta.x, data.sizeDelta.y);
            if (rect.Contains(screenPosition))
            {
                vRayTest.Add(widget);

                if(widget.GetChilds()!=null)
                {
                    foreach(var child in widget.GetChilds())
                    {
                        RaycastHudRecursive(vRayTest, child, screenPosition, camera);
                    }
                }
            }
        }
        //--------------------------------------------------------
        void CreateHierarchy(AWidget pParent, HudObject.Hierarchy hierarchy)
        {
            var hudData = m_pObject.GetData(hierarchy.id);
            if (hudData == null)
                return;

            AWidget pRoot = null;
            if (hudData is HudCanvasData)
            {
                pRoot = CreateCanvas(hudData, pParent);
            }
            else if(hudData is HudImageData)
            {
                pRoot = CreateImage(pParent, hudData);
            }
            else if(hudData is HudTextData)
            {
                pRoot = CreateText(pParent, hudData);
            }
            else if (hudData is HudNumberData)
            {
                pRoot = CreateNumber(pParent, hudData);
            }
            if (pRoot == null)
                return;

            if (hierarchy.children != null)
            {
                foreach (var child in hierarchy.children)
                {
                    CreateHierarchy(pRoot, child);
                }
            }
        }
        //--------------------------------------------------------
        HudCanvas CreateCanvas( HudBaseData hudData, AWidget pParent)
        {
            var canvas = new HudCanvas(m_pSystem, hudData);
            canvas.SetHudController(this);
            if (pParent != null)
                pParent.Attach(canvas);
            else
            {
                if (m_vWidgets == null)
                    m_vWidgets = new List<AWidget>(4);
                m_vWidgets.Add(canvas);
            }
            return canvas;
        }
        //--------------------------------------------------------
        HudImage CreateImage(AWidget pParent,HudBaseData hudData)
        {
            var widget = new HudImage(m_pSystem, hudData);
            widget.SetHudController(this);
            if (pParent != null) pParent.Attach(widget);
            else
            {
                if (m_vWidgets == null)
                    m_vWidgets = new List<AWidget>(4);
                m_vWidgets.Add(widget);
            }
            return widget;
        }
        //--------------------------------------------------------
        HudText CreateText(AWidget pParent, HudBaseData hudData)
        {
            var widget = new HudText(m_pSystem, hudData);
            widget.SetHudController(this);
            if (pParent != null) pParent.Attach(widget);
            else
            {
                if (m_vWidgets == null)
                    m_vWidgets = new List<AWidget>(4);
                m_vWidgets.Add(widget);
            }
            return widget;
        }
        //--------------------------------------------------------
        HudNumber CreateNumber(AWidget pParent, HudBaseData hudData)
        {
            var widget = new HudNumber(m_pSystem, hudData);
            widget.SetHudController(this);
            if(pParent!=null) pParent.Attach(widget);
            else
            {
                if (m_vWidgets == null)
                    m_vWidgets = new List<AWidget>(4);
                m_vWidgets.Add(widget);
            }
            return widget;
        }
#if UNITY_EDITOR
        //--------------------------------------------------------
        public void DrawDebug(Camera camera, System.Action<AWidget,Camera> onDraw)
        {
            if (onDraw == null || camera == null) return;
            if (m_vWidgets == null) return;
            foreach(var db in m_vWidgets)
            {
                DrawDebug(db,camera, onDraw);
            }
        }
        //--------------------------------------------------------
        public void DrawDebug(AWidget widget, Camera camera, System.Action<AWidget, Camera> onDraw)
        {
            if (widget == null) return;

            onDraw(widget, camera);
            if (widget.GetChilds() == null)
                return;
            foreach (var db in widget.GetChilds())
            {
                DrawDebug(db,camera, onDraw);
            }
        }
#endif
    }
}

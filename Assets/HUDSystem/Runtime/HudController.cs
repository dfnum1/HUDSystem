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
        private List<AComponent> m_vWidgets = null;
        private HudRenderBatch m_RenderBatch = null;

        private Transform m_pFollowTarget;
        private Matrix4x4 m_pFollowTransform = Matrix4x4.identity;

        private Vector3 m_Offset = Vector3.zero;
        private Vector3 m_OffsetRotation = Vector3.zero;
        private int m_nTransId = -1;
        //--------------------------------------------------------
        public HudController(HudSystem pSystem)
        {
            m_pSystem = pSystem;
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
            return m_pFollowTransform;
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
                var hudData = hudObject.GetData(db.id);
                if (hudData == null)
                    continue;

                if (hudData is HudCanvasData)
                {
                    var root = CreateCanvas(hudData, null);
                    if(db.children!=null)
                    {
                        foreach(var child in db.children)
                        {
                            CreateHierarchy(root, child);
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning(hudObject.name + " Hierarchy Root Node must be HudCanvas!");
                }
            }
            if (m_RenderBatch != null)
            {
                m_RenderBatch.SetBounds(m_nTransId, hudObject.center, hudObject.size);
            }
            TriggerReorder();
        }
        //--------------------------------------------------------
        public List<AComponent> GetWidgets()
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
        public void RemoveComponent(AComponent pComp)
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
        void CreateHierarchy(AComponent pParent, HudObject.Hierarchy hierarchy)
        {
            var hudData = m_pObject.GetData(hierarchy.id);
            if (hudData == null)
                return;

            AComponent pRoot = null;
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
        HudCanvas CreateCanvas( HudBaseData hudData, AComponent pParent)
        {
            var canvas = new HudCanvas(m_pSystem, hudData);
            canvas.SetHudController(this);
            canvas.Init();
            if (pParent != null)
                pParent.Attach(canvas);
            else
            {
                if (m_vWidgets == null)
                    m_vWidgets = new List<AComponent>(4);
                m_vWidgets.Add(canvas);
            }
            return canvas;
        }
        //--------------------------------------------------------
        HudImage CreateImage(AComponent pParent,HudBaseData hudData)
        {
            if (pParent == null)
            {
                UnityEngine.Debug.LogWarning("CreateImage Error: Parent is null!");
                return null;
            }
            var widget = new HudImage(m_pSystem, hudData);
            widget.SetHudController(this);
            widget.Init();
            pParent.Attach(widget);
            return widget;
        }
        //--------------------------------------------------------
        HudText CreateText(AComponent pParent, HudBaseData hudData)
        {
            if (pParent == null)
            {
                UnityEngine.Debug.LogWarning("CreateText Error: Parent is null!");
                return null;
            }
            var widget = new HudText(m_pSystem, hudData);
            widget.SetHudController(this);
            widget.Init();
            pParent.Attach(widget);
            return widget;
        }
    }
}

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
    public class HudController : TypeObject
    {
        private HudSystem m_pSystem;
        private HudObject m_pObject;
        private List<AWidget> m_vTopWidgets = null;
        private Dictionary<int, AWidget> m_vWidgetMaps = null;
        private HudRenderBatch m_RenderBatch = null;

        private Transform m_pFollowTarget;
        private Matrix4x4 m_pFollowTransform = Matrix4x4.identity;

        private Vector3 m_Offset = Vector3.zero;
        private Vector3 m_OffsetRotation = Vector3.zero;
        private int m_nTransId = -1;

        private bool m_bDestroyed = false;
#if UNITY_EDITOR
        internal bool m_bEditorMode = false;
#endif
        //--------------------------------------------------------
        public HudController()
        {
            m_bDestroyed = false;
        }
        //--------------------------------------------------------
        internal void SetHudSystem(HudSystem pSystem)
        {
            m_bDestroyed = false;
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
        public bool AllowRotation
        {
            get
            {
                if (m_pObject != null) return m_pObject.allowRotation;
                return true;
            }
        }
        //--------------------------------------------------------
        public bool AllowScale
        {
            get
            {
                if (m_pObject != null) return m_pObject.allowScale;
                return true;
            }
        }
        //--------------------------------------------------------
        public Vector3 OffsetPosition
        {
            get
            {
                return this.m_Offset;
            }
            set
            {
                if(this.m_Offset != value)
                {
                    this.m_Offset = value;
                    UpdateTransform();
                }
            }
        }
        //--------------------------------------------------------
        public Vector3 OffsetRotation
        {
            get
            {
                return this.m_OffsetRotation;
            }
            set
            {
                if (this.m_OffsetRotation != value)
                {
                    this.m_OffsetRotation = value;
                    UpdateTransform();
                }
            }
        }
        //--------------------------------------------------------
        internal void SpawnInstance(AWidget pWidget, string file, System.Action<GameObject> callback)
        {
            m_pSystem.SpawnInstance(pWidget, file, callback);
        }
        //--------------------------------------------------------
        internal void DestroyInstance(AWidget pWidget, GameObject pGo)
        {
            m_pSystem.DestroyInstance(pWidget, pGo);
        }
        //--------------------------------------------------------
        public bool HasFollowTransform()
        {
            return m_pFollowTarget != null;
        }
        //--------------------------------------------------------
        public Transform GetFollowTargetJob()
        {
            return m_pFollowTarget;
        }
        //--------------------------------------------------------
        public void SetFollowTarget(Transform target)
        {
            if (m_pFollowTarget == target)
                return;
            m_pFollowTarget = target;
            UpdateTransform();
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
            UpdateTransform();
        }
        //--------------------------------------------------------
        public Matrix4x4 GetWorldMatrixJob()
        {
            if (m_pFollowTarget)
            {
                return Matrix4x4.identity;
            }
            Vector3 position = m_pFollowTransform.GetPosition() + m_Offset;
            Quaternion rotation = Quaternion.identity;
            if(AllowRotation) rotation = m_pFollowTransform.rotation * Quaternion.Euler(m_OffsetRotation);
            Vector3 scale = Vector3.one;
            if (AllowScale) scale = m_pFollowTransform.lossyScale;

            return Matrix4x4.TRS(position,rotation, scale);
        }
        //--------------------------------------------------------
        public Matrix4x4 GetWorldMatrix()
        {
            Matrix4x4 worldMatrix = m_pFollowTransform;
            if (m_pFollowTarget)
            {
                worldMatrix = m_pFollowTarget.localToWorldMatrix;
            }
            Vector3 position = worldMatrix.GetPosition() + m_Offset;
            Quaternion rotation = Quaternion.identity;
            if (AllowRotation) rotation = worldMatrix.rotation * Quaternion.Euler(m_OffsetRotation);
            Vector3 scale = Vector3.one;
            if (AllowScale) scale = worldMatrix.lossyScale;

            return Matrix4x4.TRS(position, rotation, scale);
        }
        //--------------------------------------------------------
        public void UpdateTransform()
        {
            if (m_RenderBatch != null && m_nTransId >= 0)
            {
                m_RenderBatch.UpdateHudController(m_nTransId, this);
            }
            foreach(var db in m_vTopWidgets)
            {
                db.DoTransformChanged();
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
        internal Dictionary<int ,AWidget> GetWidgets()
        {
            return m_vWidgetMaps;
        }
        //--------------------------------------------------------
        public void SetHudObject(HudObject hudObject)
        {
            m_bDestroyed = false;
            if (m_pObject == hudObject)
                return;
            m_pObject = hudObject;

            if (m_vWidgetMaps != null) m_vWidgetMaps.Clear();
            if (m_vTopWidgets != null) m_vTopWidgets.Clear();
            if (m_pObject == null || m_pObject.vHierarchies == null)
                return;

            m_pObject.Init();

            if (m_RenderBatch == null)
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

            var hudDatas = m_pObject.GetDatas();
            if (m_vWidgetMaps == null)
                m_vWidgetMaps = new Dictionary<int, AWidget>(hudDatas.Count);
            foreach (var db in hudDatas)
            {
                var hudData = db.Value;
                AWidget widget = hudData.CreateWidget(m_pSystem);
                if (widget != null)
                {
                    widget.SetHudController(this);
                    m_vWidgetMaps[db.Key] = widget;
                }
            }

            foreach (var node in m_pObject.vHierarchies)
            {
                if (node.children != null)
                {
                    foreach (var childId in node.children)
                    {
                        if (m_vWidgetMaps.TryGetValue(node.id, out var parent) && m_vWidgetMaps.TryGetValue(childId, out var child))
                        {
                            parent.Attach(child);
                        }
                    }
                }
            }
            if (m_vTopWidgets == null)
                m_vTopWidgets = new List<AWidget>(m_vWidgetMaps.Count);
            foreach (var node in m_pObject.vHierarchies)
            {
                if (node.parentId == -1 && m_vWidgetMaps.TryGetValue(node.id, out var widget))
                {
                    m_vTopWidgets.Add(widget);
                }
            }

            foreach (var db in m_vTopWidgets)
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
        public List<AWidget> GetTopWidgets()
        {
            return m_vTopWidgets;
        }
        //--------------------------------------------------------
        public T GetWidgetById<T>(int id) where T : AWidget
        {
            if (m_vWidgetMaps != null && m_vWidgetMaps.TryGetValue(id, out var widget))
            {
                return widget as T;
            }
            return null;
        }
        //--------------------------------------------------------
        internal void AddTopWidget(AWidget pWidget)
        {
            if (m_vWidgetMaps == null) m_vWidgetMaps = new Dictionary<int, AWidget>(8);
            m_vWidgetMaps[pWidget.GetId()] = pWidget;
            if (pWidget.GetParent() == null && !m_vTopWidgets.Contains(pWidget))
            {
                if (m_vTopWidgets == null) m_vTopWidgets = new List<AWidget>(2);
                m_vTopWidgets.Add(pWidget);
            }
        }
        //--------------------------------------------------------
        internal void RemoveTopWidget(AWidget pWidget)
        {
            if(m_vTopWidgets!=null) m_vTopWidgets.Remove(pWidget);
        }
        //--------------------------------------------------------
        internal void OnWidgetIDChange(AWidget pWidget, int newId, int oldId)
        {
            if (pWidget == null) return;
            m_vWidgetMaps.Remove(oldId);
            m_vWidgetMaps[newId] = pWidget;
        }
        //--------------------------------------------------------
        internal void OnWidgetDestroy(AWidget pWidget)
        {
            if (m_bDestroyed) return;
            if (pWidget == null) return;
            m_vTopWidgets.Remove(pWidget);
            m_vWidgetMaps.Remove(pWidget.GetId());
        }
        //--------------------------------------------------------
        internal void OnRebuild()
        {
            if (m_vTopWidgets == null)
                return;

            if(m_nTransId>=0 && m_RenderBatch !=null)
            {
                m_RenderBatch.RemoveHudData(m_nTransId);
            }

            m_vTopWidgets.Sort((w1, w2) =>
            {
                return w1.GetTagZ().CompareTo(w2.GetTagZ());
            });
            foreach (var db in m_vTopWidgets)
            {
                db.OnRebuild();
            }
        }
        //--------------------------------------------------------
        internal override void Destroy()
        {
            m_bDestroyed = true;
            if (m_RenderBatch != null) m_RenderBatch.RemoveExpansionNotif(TriggerReorder);
            m_RenderBatch = null;
            if (m_nTransId >= 0 && m_RenderBatch != null)
            {
                m_RenderBatch.RemoveHudData(m_nTransId);
            }
            if (m_vWidgetMaps != null)
            {
                foreach (var db in m_vWidgetMaps)
                {
                    db.Value.Destroy();
                }
                m_vWidgetMaps.Clear();
            }
            if (m_vTopWidgets != null) m_vTopWidgets.Clear();
            m_pSystem = null;
            m_pObject = null;

            m_pFollowTarget = null;
            m_pFollowTransform = Matrix4x4.identity;

            m_Offset = Vector3.zero;
            m_OffsetRotation = Vector3.zero;
            m_nTransId = -1;

#if UNITY_EDITOR
            m_bEditorMode = false;
#endif
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
            if (m_vTopWidgets == null)
                return;
            foreach (var db in m_vTopWidgets)
            {
                db.SetDirty();
            }
        }
        //--------------------------------------------------------
        public AWidget RaycastHud(Vector2 screenPosition, Camera  camera, List<AWidget> vResults = null, bool bIngoreRayTest= false)
        {
            if (camera == null || m_vTopWidgets==null) return null;

            var rayTest = m_pSystem.GetRayTestCache();
            rayTest.Clear();

            foreach (var comp in m_vTopWidgets)
            {
                RaycastHudRecursive(rayTest,comp, screenPosition, camera, bIngoreRayTest);
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
        void RaycastHudRecursive(List<AWidget> vRayTest, AWidget widget, Vector2 screenPosition, Camera camera, bool bIngoreRayTest)
        {
            var data = widget.GetData();
            if (data == null ) return;
            if (!bIngoreRayTest && !data.rayTest)
                return;
            Vector3 worldPos = camera.WorldToScreenPoint(GetWorldMatrix()*widget.GetPosition());
            Rect rect = new Rect(worldPos.x - data.sizeDelta.x * 0.5f, worldPos.y - data.sizeDelta.y * 0.5f, data.sizeDelta.x, data.sizeDelta.y);
            if (rect.Contains(screenPosition))
            {
                vRayTest.Add(widget);
            }
            if (widget.GetChilds() != null)
            {
                foreach (var child in widget.GetChilds())
                {
                    RaycastHudRecursive(vRayTest, child, screenPosition, camera, bIngoreRayTest);
                }
            }
        }
#if UNITY_EDITOR
        //--------------------------------------------------------
        public void DrawDebug(Camera camera, System.Action<AWidget,Camera> onDraw)
        {
            if (onDraw == null || camera == null) return;
            if (m_vTopWidgets == null) return;
            foreach(var db in m_vTopWidgets)
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

/********************************************************************
生成日期:	11:11:2025
类    名: 	PreviewLogic
作    者:	HappLI
描    述:	HUD 预览逻辑
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Framework.HUD.Editor
{
    public class PreviewLogic : AEditorLogic
    {
        public class Instance
        {
            public GameObject pInstance;
            public List<ParticleSystem> vParticles = new List<ParticleSystem>();
            public float time;
        }
        TargetPreview m_pPreview = null;
        GUIStyle m_PreviewStyle; 
        AWidget m_pSelectComponent = null;
        GameObject m_pTestRole = null;
        GameObject m_pPreviewRoot = null;
        Dictionary<AWidget, Instance> m_vInstance = new Dictionary<AWidget, Instance>();
        HashSet<AWidget> m_vRayTestLocks = new HashSet<AWidget>();
        //--------------------------------------------------------
        public PreviewLogic(HUDEditor editor, Rect viewRect) : base(editor, viewRect)
        {
        }
        //--------------------------------------------------------
        internal override void OnSelectComponent(AWidget component)
        {
            m_pSelectComponent = component;
        }
        //--------------------------------------------------------
        public override void OnEnable()
        {
            if (m_pPreview == null)
            {
                m_pPreview = new TargetPreview();
                if (m_pPreview == null) m_pPreview = new TargetPreview(m_pEditor);
                GameObject[] roots = new GameObject[1];
                roots[0] = new GameObject("PreviewRoot");
                m_pPreview.AddPreview(roots[0]);
                m_pPreview.showFloor = 0.1f;

                m_pPreview.SetCamera(0.01f, 10000f, 60f);
                m_pPreview.Initialize(roots);
                m_pPreview.SetPreviewInstance(roots[0] as GameObject);
                m_pPreview.bLeftMouseForbidMove = true;
                m_pPreview.OnDrawAfterCB += OnPreviewDraw;
                m_pPreview.ResetCamaraOffset(Vector3.zero);
                m_pPreview.SetZoom(2);

                m_pEditor.GetHudSystem().SetRenderCamera(m_pPreview.GetCamera());

                m_pPreviewRoot =  GameObject.CreatePrimitive(PrimitiveType.Cube);
                m_pPreviewRoot.transform.localScale = Vector3.one * 0.2f;
                m_pTestRole = new GameObject("HudRoot");
                m_pPreview.AddPreview(m_pPreviewRoot);
                m_pPreview.AddPreview(m_pTestRole);
            }
        }
        //--------------------------------------------------------
        public override void OnDisable()
        {
            if (m_pPreview != null) m_pPreview.Destroy();
            m_pPreview = null;
            if(m_pPreviewRoot!=null)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(m_pPreviewRoot);
                else
                    GameObject.DestroyImmediate(m_pPreviewRoot);
                m_pPreviewRoot = null;
            }
            if (m_pTestRole != null)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(m_pTestRole);
                else
                    GameObject.DestroyImmediate(m_pTestRole);
                m_pTestRole = null;
            }
        }
        //--------------------------------------------------------
        public override void OnSpawnInstance(AWidget pWidget, string strParticle, GameObject pIns)
        {
            if(pIns && m_pPreview!=null)
                m_pPreview.AddPreview(pIns);

            m_vInstance.Remove(pWidget);
            Instance pInstacne = new Instance();
            pInstacne.pInstance = pIns;
            pInstacne.time = 0;
            var particle = pIns.GetComponent<ParticleSystem>();
            if (particle != null) pInstacne.vParticles.Add(particle);
            var particles = pIns.GetComponentsInChildren<ParticleSystem>();
            if (particles != null)
                pInstacne.vParticles.AddRange(particles);
            foreach (var db in pInstacne.vParticles)
                db.Play(true);
            m_vInstance[pWidget] =pInstacne;
        }
        //--------------------------------------------------------
        public override void OnDestroyInstance(AWidget pWidget, GameObject pGameObject)
        {
        }
        //--------------------------------------------------------
        protected override void OnGUI()
        {
            if (m_pPreview != null && viewRect.width>0 && viewRect.height>0)
            {
                if (m_PreviewStyle == null) m_PreviewStyle = new GUIStyle(EditorStyles.textField);
                m_pPreview.OnPreviewGUI(new Rect(0,0, viewRect.width,viewRect.height), m_PreviewStyle);
            }
        }
        //--------------------------------------------------------
        void DrawHudObjectBounds()
        {
            var hudObject = GetHudObject();
            if (hudObject == null)
                return;

            Vector2 center2D = hudObject.center;
            Vector2 size2D = hudObject.size/ HUDUtils.PIXEL_SIZE;
            float boxDepth = 1f;

            // 包围盒中心
            Vector3 center = new Vector3(center2D.x, center2D.y, 0) + (m_pTestRole ? m_pTestRole.transform.position : Vector3.zero);
            Vector3 halfSize = new Vector3(size2D.x * 0.5f, size2D.y * 0.5f, boxDepth * 0.5f);

            // 8个顶点
            Vector3[] verts = new Vector3[8];
            verts[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z); // 左下前
            verts[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);  // 右下前
            verts[2] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);   // 右上前
            verts[3] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);  // 左上前
            verts[4] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);  // 左下后
            verts[5] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);   // 右下后
            verts[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);    // 右上后
            verts[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);   // 左上后

            Color oldColor = Handles.color;
            Handles.color = new Color(0,1,0,0.5f);

            // 前面
            Handles.DrawAAPolyLine(3, verts[0], verts[1], verts[2], verts[3], verts[0]);
            // 后面
            Handles.DrawAAPolyLine(3, verts[4], verts[5], verts[6], verts[7], verts[4]);
            // 连接前后
            Handles.DrawAAPolyLine(3, verts[0], verts[4]);
            Handles.DrawAAPolyLine(3, verts[1], verts[5]);
            Handles.DrawAAPolyLine(3, verts[2], verts[6]);
            Handles.DrawAAPolyLine(3, verts[3], verts[7]);

            Handles.color = oldColor;
        }
        //--------------------------------------------------------
        public override void OnUpdate(float deltaTime)
        {
            foreach (var db in m_vInstance)
            {
                if (db.Value.vParticles != null)
                {
                    foreach (var par in db.Value.vParticles)
                    {
                        if (!par.isPlaying)
                            par.Play();
                        par.Simulate(deltaTime, false,false); 
                    }
                }
                db.Value.time += deltaTime;
                if (db.Value.time >= 10000) db.Value.time = 0;
            }
        }
        //--------------------------------------------------------
        void OnPreviewDraw(int controllerId, Camera camera, Event evt)
        {
            var hudSystem = GetHudSystem();
            if (hudSystem == null)
                return;


            DrawHudObjectBounds();
            hudSystem.Update();
            hudSystem.BeginRender();
            hudSystem.Render();
            hudSystem.EndRender();
            hudSystem.LateUpdate();

            if(m_pSelectComponent!=null)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 pos = m_pSelectComponent.GetPosition();
                if (m_pTestRole != null) pos += m_pTestRole.transform.position;
                pos = Handles.DoPositionHandle(pos, Quaternion.identity);
                if(EditorGUI.EndChangeCheck())
                {
                    if (m_pTestRole != null) pos -= m_pTestRole.transform.position;
                    if (m_pSelectComponent.GetParent() != null) pos -= m_pSelectComponent.GetParent().GetPosition();
                    m_pSelectComponent.GetData().position = pos;
                    m_pSelectComponent.SetDirty();
                }
            }

            if(m_pTestRole!=null)
            {
                if(m_pPreviewRoot)
                {
                    m_pPreviewRoot.transform.position = m_pTestRole.transform.position;
                    m_pPreviewRoot.transform.rotation = m_pTestRole.transform.rotation;
                }
                if (m_pSelectComponent==null)
                {
                    if (UnityEditor.Tools.current == Tool.Move || UnityEditor.Tools.current == Tool.Transform)
                        m_pTestRole.transform.position = Handles.DoPositionHandle(m_pTestRole.transform.position, Quaternion.identity);
                    else if (UnityEditor.Tools.current == Tool.Scale)
                        m_pTestRole.transform.localScale = Handles.DoScaleHandle(m_pTestRole.transform.localScale, m_pTestRole.transform.position, Quaternion.identity, HandleUtility.GetHandleSize(m_pTestRole.transform.position));
            //        else if (UnityEditor.Tools.current == Tool.Rotate)
            //            m_pTestRole.transform.rotation = Handles.DoRotationHandle(m_pTestRole.transform.rotation, m_pTestRole.transform.position);
                }
                GetHud().SetFollowTarget(m_pTestRole.transform);
                GetHud().UpdateTransform();
            }

            GetHud().DrawDebug(camera, DrawWidgetRect);

            if (evt.type == EventType.KeyDown)
            {
                if(evt.keyCode == KeyCode.Escape)
                {
                    m_vRayTestLocks.Clear();
                }
            }

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                Vector2 mousePos = evt.mousePosition; 
                Vector2 fixedScreenPos = mousePos;
                fixedScreenPos.y = camera.pixelHeight - fixedScreenPos.y;

                List<AWidget> vTest = new List<AWidget>();
                var hitComp = GetHud().RaycastHud(fixedScreenPos, camera, vTest, true);
                if(vTest.Count>0)
                {
                    for(int i =0; i < vTest.Count;)
                    {
                        if (m_vRayTestLocks.Contains(vTest[i]))
                        {
                            vTest.RemoveAt(i);
                        }
                        else
                            ++i;
                    }
                    if (m_vRayTestLocks.Count >= vTest.Count)
                        m_vRayTestLocks.Clear();
                }
                if(vTest.Count>0)
                {
                    hitComp = vTest[0];
                }

                if (hitComp != null)
                {
                    m_vRayTestLocks.Add(hitComp);
                    m_pEditor.OnSelectComponent(hitComp);
                    evt.Use();
                }
            }
        }
        //--------------------------------------------------------
        void DrawWidgetRect(AWidget widget, Camera camera)
        {
            var data = widget.GetData();
            if (data == null) return;

            float perUnit = HUDUtils.PIXEL_SIZE;
            if (widget.GetHudType() == EHudType.Text)
                perUnit = 1;

            // 获取父节点的世界矩阵
            Matrix4x4 parentMatrix = Matrix4x4.identity;
            if (widget.GetParent() != null)
            {
                var parentPos = widget.GetParent().GetPosition() + m_pTestRole.transform.position;
                parentMatrix = Matrix4x4.TRS(parentPos, Quaternion.identity, Vector3.one);
            }

            Vector3 center = widget.GetPosition() + m_pTestRole.transform.position;
            Vector2 size = data.sizeDelta / perUnit;

            // 计算四个顶点（世界坐标）
            Vector3 right = Vector3.right * size.x * 0.5f;
            Vector3 up = Vector3.up * size.y * 0.5f;
            Vector3[] corners = new Vector3[5];
            corners[0] = GetHud().GetWorldMatrix()*(center - right - up);
            corners[1] = GetHud().GetWorldMatrix()*(center + right - up);
            corners[2] = GetHud().GetWorldMatrix()*(center + right + up);
            corners[3] = GetHud().GetWorldMatrix() * (center - right + up);
            corners[4] = corners[0]; // 闭合

            Color color = Handles.color;
            Handles.color = (widget == m_pSelectComponent) ? Color.red : Color.grey;
            Handles.DrawAAPolyLine((widget == m_pSelectComponent) ? 5:2, corners);
            Handles.color = color;

            // 仅选中时显示角点
            if (widget == m_pSelectComponent && widget.GetHudType() != EHudType.Text)
            {
                EditorGUI.BeginChangeCheck();
                float handleSize = HandleUtility.GetHandleSize(center) * 0.08f;
                Vector3[] newCorners = new Vector3[4];
                for (int i = 0; i < 4; ++i)
                {
                    Handles.color = Color.yellow;
                    newCorners[i] = Handles.FreeMoveHandle(corners[i], handleSize, Vector3.zero, Handles.SphereHandleCap);
                }
                Handles.color = color;

                if (EditorGUI.EndChangeCheck())
                {
                    // 检查哪个角被拖动
                    int dragIndex = 0;
                    float maxDist = (newCorners[0] - corners[0]).sqrMagnitude;
                    for (int i = 1; i < 4; ++i)
                    {
                        float dist = (newCorners[i] - corners[i]).sqrMagnitude;
                        if (dist > maxDist)
                        {
                            maxDist = dist;
                            dragIndex = i;
                        }
                    }
                    int oppIndex = (dragIndex + 2) % 4;

                    // 将拖拽后的世界坐标转换为本地坐标（相对父节点）
                    Matrix4x4 invParent = parentMatrix.inverse;
                    Vector3 localDragCorner = invParent.MultiplyPoint3x4(newCorners[dragIndex]);
                    Vector3 localOppCorner = invParent.MultiplyPoint3x4(newCorners[oppIndex]);

                    // 新中心和新size
                    Vector3 newCenter = (localDragCorner + localOppCorner) * 0.5f;
                    Vector2 newSize = new Vector2(
                        Mathf.Abs(localDragCorner.x - localOppCorner.x),
                        Mathf.Abs(localDragCorner.y - localOppCorner.y)
                    );

                    // 保持长宽比
                    if (Event.current.shift)
                    {
                        float aspect = size.x / Mathf.Max(size.y, 0.0001f);
                        if (newSize.x / Mathf.Max(newSize.y, 0.0001f) > aspect)
                        {
                            newSize.x = newSize.y * aspect;
                        }
                        else
                        {
                            newSize.y = newSize.x / aspect;
                        }
                    }
                    float minSize = 5f;
                    newSize.x = Mathf.Max(newSize.x, minSize / perUnit);
                    newSize.y = Mathf.Max(newSize.y, minSize / perUnit);

                    data.sizeDelta = newSize * perUnit;
                    data.position = newCenter;
                    widget.SetDirty();
                }
            }
        }
    }
}
#endif
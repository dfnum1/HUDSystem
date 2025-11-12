/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDSystem
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GridBrushBase;

namespace Framework.HUD.Runtime
{
    public class HUDSystem
    {
        HashSet<HUDGroup> m_vRebuildGroupMesh = null;
        Dictionary<AGraphic, EOperationType> m_vNeedRebuilds = null;
        private RenderMeshJob m_RenderMeshJob;

        private GraphicBatch[] m_arrBatchs = null;
        private BatchData m_BatchData;
        private float m_fFontUVPadding = 0;
        private int m_nFontAltasWidth = 0;
        private int m_nFontAtlasHeight = 0;

        Stack<Mesh> m_vMeshPool = null;
        public float font_uv_padding { get { return m_fFontUVPadding; } }
        public int font_altas_width { get { return m_nFontAltasWidth; } }
        public int font_altas_height { get { return m_nFontAtlasHeight; } }
        //--------------------------------------------------------
        public HUDSystem()
        {
            m_BatchData = new BatchData();
            m_BatchData.BeginRequestSpace();
            var spriteBatchInfo = m_BatchData.RequestSpace(500, 1);
            var textBatchInfo = m_BatchData.RequestSpace(500, 16);
            m_BatchData.EndRequestSpace();

            m_arrBatchs = new GraphicBatch[(int)EGraphicType.Count];
            for (int i = 0; i < m_arrBatchs.Length; ++i)
            {
                m_arrBatchs[i] = null;
            }

            //text
            {
                var batch = new GraphicBatch(this);
                BufferSlice slice = new BufferSlice();
                slice.Init(m_BatchData, ref textBatchInfo);
                batch.InitNew(m_BatchData, slice);
                m_arrBatchs[(int)EGraphicType.Text] = batch;
            }

            //sprite
            {
                var batch = new GraphicBatch(this);
                BufferSlice slice = new BufferSlice();
                slice.Init(m_BatchData, ref spriteBatchInfo);
                batch.InitNew(m_BatchData, slice);
                m_arrBatchs[(int)EGraphicType.Sprite] = batch;
            }

        //    m_fFontUVPadding = TMPro.ShaderUtilities.GetPadding(setting.atlas_material, false, false);
        //    m_nFontAltasWidth = setting.font.atlasWidth;
       //     m_nFontAtlasHeight = setting.font.atlasHeight;

            m_RenderMeshJob = new RenderMeshJob();
            m_RenderMeshJob.Init(m_BatchData);
        }
        //--------------------------------------------------------
        public void EditorRender()
        {

        }
        //--------------------------------------------------------
        public void Update()
        {
            m_RenderMeshJob.FinishUpdateMesh();

            NativeBuffer<JobHandle> handles = new NativeBuffer<JobHandle>(m_arrBatchs.Length, Allocator.Temp);
            bool has_job = false;
            for (int i =0; i < m_arrBatchs.Length; ++i)
            {
                var b = m_arrBatchs[i];
                var handle = b.BuildJob(out has_job);
                if (has_job)
                {
                    handles.Add(handle);
                }
            }

            //if(handles.Length > 0)
            if(m_vRebuildGroupMesh!=null)
            {
                m_RenderMeshJob.BeginCollectionMeshInfo();
                foreach (var g in m_vRebuildGroupMesh)
                {
                    if (g.GetMesh() == null)
                        continue;

                    m_RenderMeshJob.CollectionMeshfInfo(g);
                }
                m_RenderMeshJob.EndCollectionMeshInfo(JobHandle.CombineDependencies(handles));
                m_vRebuildGroupMesh.Clear();
            }
            handles.Dispose();
        }
        //--------------------------------------------------------
        public void RebuildGraphic(AGraphic graphic, EDirtyFlag flag)
        {
            if (graphic.Batch == null)
                return;

            switch (flag)
            {
                case EDirtyFlag.ETransform:
                    graphic.Batch.PushOperation(graphic, EOperationType.TransformChange);
                    break;
                case EDirtyFlag.EQuad:
                    graphic.Batch.PushOperation(graphic, EOperationType.VertexProperty);
                    break;
            }
            RegisterHUDGroupRebuildMesh(graphic.Group);
        }
        //--------------------------------------------------------
        internal void OnAddGraphic(AGraphic graphic)
        {
            if (graphic.Batch != null)
                return;

            graphic.Batch = m_arrBatchs[(int)graphic.GetGraphicType()];
            graphic.Batch.PushOperation(graphic, EOperationType.Add);
            RegisterHUDGroupRebuildMesh(graphic.Group);
        }
        //--------------------------------------------------------
        internal void OnRemoveGraphic(AGraphic graphic)
        {
            if (graphic.Batch == null)
                return;

            graphic.Batch.PushOperation(graphic, EOperationType.Remove);
            RegisterHUDGroupRebuildMesh(graphic.Group);
        }
        //--------------------------------------------------------
        internal void ActiveGraphic(AGraphic graphic)
        {
            OnAddGraphic(graphic);
        }
        //--------------------------------------------------------
        internal void DeActiveGraphic(AGraphic graphic)
        {
            OnRemoveGraphic(graphic);
        }
        //--------------------------------------------------------
        void RegisterHUDGroupRebuildMesh(HUDGroup group)
        {
            if(m_vRebuildGroupMesh != null && m_vRebuildGroupMesh.Contains(group))
            {
                return;
            }
            if (m_vRebuildGroupMesh == null)
                m_vRebuildGroupMesh = new HashSet<HUDGroup>(32);
            m_vRebuildGroupMesh.Add(group);
        }
        //--------------------------------------------------------
        public void ActiveHUDGroup(HUDGroup group)
        {
            if (group.GetMesh() == null)
            {
                if (m_vMeshPool == null || m_vMeshPool.Count == 0)
                {
                    var mesh = new Mesh();
                    mesh.MarkDynamic();
                    group.SetMesh(mesh);
                }
                else
                {
                    group.SetMesh(m_vMeshPool.Pop());
                }
            }
        }
        //--------------------------------------------------------
        public void DeActiveHUDGroup(HUDGroup group)
        {
            if (group.GetMesh() != null)
            {
                if (m_vMeshPool == null) m_vMeshPool = new Stack<Mesh>(32);
                m_vMeshPool.Push(group.GetMesh());
                group.SetMesh(null);
            }
        }
        //--------------------------------------------------------
        public void Destroy()
        {
            for (int i = 0; i < m_arrBatchs.Length; ++i)
            {
                var b = m_arrBatchs[i];
                b.Dispose();
                m_arrBatchs[i] = null;
            }
            m_RenderMeshJob.Dispose();
            m_BatchData.Dispose();
            if (m_vRebuildGroupMesh != null) m_vRebuildGroupMesh.Clear();
            if(m_vMeshPool!=null)
            {
                foreach(var db in m_vMeshPool)
                {
                    db.Clear();
#if UNITY_EDITOR
                    if(Application.isPlaying)
                        UnityEngine.Object.Destroy(db);
                    else
                        UnityEngine.Object.DestroyImmediate(db);
#else
                     UnityEngine.Object.Destroy(db);
#endif

                }
                m_vMeshPool.Clear();
            }
        }
    }
}

/********************************************************************
生成日期:	11:11:2025
类    名: 	GraphicBatch
作    者:	HappLI
描    述:	HUD 图元批次
*********************************************************************/
using Codice.CM.Common.Checkin.Partial.DifferencesApplier;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.Port;

namespace Framework.HUD.Runtime
{
    //--------------------------------------------------------
    //! BuildTransformJobData
    //--------------------------------------------------------
    public struct BuildTransformJobData
    {
        public EGraphicType grapicType;
        public byte is_active;
        public EDirtyFlag flag;
        public int index;
        public int per_quad_index;
        public int valid_quad;
        public float spacing;
        public float gscale;
        public float3 local_position;
        public float3 local_scale;

        public half4 extend;
    }
    //--------------------------------------------------------
    //! BuildPerQuadData
    //--------------------------------------------------------
    public struct BuildPerQuadData
    {
        public float2 size;
        public float4 uv0;
        public float4 tparams;
        public Color32 color;
    }
    //--------------------------------------------------------
    //SubBufferData
    //--------------------------------------------------------
    internal struct SubBufferData
    {
        public NativeSlice<BuildTransformJobData> transform_job_datas;
        public NativeSlice<BuildPerQuadData> quad_job_datas;
        public NativeSlice<Vertex> vertex_datas;
    }
    //--------------------------------------------------------
    //! BatchData
    //--------------------------------------------------------
    internal class BatchData
    {
        private NativeBuffer<Vertex> m_vVertexDatas;
        public NativeBuffer<Vertex> vertex_datas { get { return m_vVertexDatas; } }
        private NativeBuffer<BuildTransformJobData> m_vBuildTransformJobDatas;
        public NativeBuffer<BuildTransformJobData> build_transform_job_datas { get { return m_vBuildTransformJobDatas; } }
        private NativeBuffer<BuildPerQuadData> m_vBuildQuadDatas;
        public NativeBuffer<BuildPerQuadData> build_quad_datas { get { return m_vBuildQuadDatas; } }

        private Dictionary<int, BufferInfo> m_vBufferInfos = null;

        private int m_nCapacity = 0;
        private int m_nQuadCount = 0;
        private List<BufferInfo> m_vPendingInfos = null;

        public void Reset()
        {
            if(m_vBufferInfos!=null)
                m_vBufferInfos.Clear();
            if (m_vBuildTransformJobDatas.IsCreated)
                m_vBuildTransformJobDatas.Clear();

            if (m_vBuildQuadDatas.IsCreated)
                m_vBuildQuadDatas.Clear();

            if (m_vVertexDatas.IsCreated)
                m_vVertexDatas.Clear();
        }
        //--------------------------------------------------------
        public void BeginRequestSpace()
        {
            if(m_vPendingInfos!=null) m_vPendingInfos.Clear();
            m_nCapacity = 0;
            m_nQuadCount = 0;
        }
        //--------------------------------------------------------
        public BufferInfo RequestSpace(int capacity, int quad_count)
        {
            int offset = m_vBuildTransformJobDatas.IsCreated ? m_vBuildTransformJobDatas.Length : 0;
            offset += m_nCapacity;

            int quad_offset = m_vBuildQuadDatas.IsCreated ? m_vBuildQuadDatas.Length : 0;
            quad_offset += m_nQuadCount;

            BufferInfo info = new BufferInfo();
            info.offset = offset;
            info.quad_offset = quad_offset;
            info.length = capacity;
            info.quad_count = quad_count;
            if (m_vPendingInfos == null)
                m_vPendingInfos = new List<BufferInfo>(32);
            m_vPendingInfos.Add(info);
            m_nCapacity += capacity;
            m_nQuadCount += capacity * quad_count;

            return info;
        }
        //--------------------------------------------------------
        public void EndRequestSpace()
        {
            if (m_vPendingInfos == null)
                return;

            int vertex_count = m_nQuadCount * 4;
            if (!m_vBuildTransformJobDatas.IsCreated)
            {
                m_vBuildTransformJobDatas = new NativeBuffer<BuildTransformJobData>(m_nCapacity, Allocator.Persistent);
                m_vBuildTransformJobDatas.AddLength(m_nCapacity);
                m_vBuildQuadDatas = new NativeBuffer<BuildPerQuadData>(m_nQuadCount, Allocator.Persistent);
                m_vBuildQuadDatas.AddLength(m_nQuadCount);
                m_vVertexDatas = new NativeBuffer<Vertex>(vertex_count, Allocator.Persistent);
                m_vVertexDatas.AddLength(vertex_count);
            }
            else
            {
                m_vBuildTransformJobDatas.AddLength(m_nCapacity);
                m_vBuildQuadDatas.AddLength(m_nQuadCount);
                m_vVertexDatas.AddLength(vertex_count);
            }

            if (m_vBufferInfos == null)
                m_vBufferInfos = new Dictionary<int, BufferInfo>(64);
            foreach (var info in m_vPendingInfos)
            {
                m_vBufferInfos.Add(info.offset, info);
            }

            m_vPendingInfos.Clear();
        }
        //--------------------------------------------------------
        public SubBufferData GetSubBuildTransJobData(BufferInfo info)
        {
            SubBufferData data = new SubBufferData()
            {
                transform_job_datas = new NativeSlice<BuildTransformJobData>(m_vBuildTransformJobDatas, info.offset, info.length),
                quad_job_datas = new NativeSlice<BuildPerQuadData>(m_vBuildQuadDatas, info.quad_offset, info.length * info.quad_count),
                vertex_datas = new NativeSlice<Vertex>(m_vVertexDatas, info.quad_offset * 4, info.length * info.quad_count * 4)
            };

            return data;
        }
        //--------------------------------------------------------
        public void Dispose()
        {
            m_vBuildTransformJobDatas.Dispose();
            m_vBuildQuadDatas.Dispose();
            m_vVertexDatas.Dispose();
        }
    }
    //--------------------------------------------------------
    //! GraphicBatch
    //--------------------------------------------------------
    internal class GraphicBatch
    {
        public struct Operation
        {
            public EOperationType opt;
            public AGraphic item;
            public float4x4 world;
        }
        HUDSystem m_pSystem;
        private BufferSlice m_BufferInfo;
        private NativeBuffer<int> m_vDirtyIndices;
        private HashSet<int> m_vDirtySets = null;

        private Queue<Operation> m_vOperationQueue = null;
        private Dictionary<AGraphic, EOperationType> m_vNeedRebuildGraphics;

        private Dictionary<int, AGraphic> m_vItems = null;

        JobHandle m_CurrentJob;
        bool m_bJobValid = false;

        //--------------------------------------------------------
        public GraphicBatch(HUDSystem system)
        {
            m_pSystem = system;
        }
        //--------------------------------------------------------
        public BufferSlice GetBufferInfo()
        {
            return m_BufferInfo;
        }
        //--------------------------------------------------------
        public void InitNew(BatchData bufferData, BufferSlice bufferInfo)
        {
            m_BufferInfo = bufferInfo;
            m_vDirtyIndices = new NativeBuffer<int>(32, Allocator.Persistent);
        }
        //--------------------------------------------------------
        public void Dispose()
        {
            m_vDirtyIndices.Dispose();
        }
        //--------------------------------------------------------
        public JobHandle BuildJob(out bool valid)
        {
            valid = false;
            QueryDirtyData();
            if (m_vDirtySets!=null && m_vDirtySets.Count > 0)
            {
                m_CurrentJob = BuildJob();
                valid = true;
            }

            return m_CurrentJob;
        }
        //--------------------------------------------------------
        public void CheckJob()
        {
            if (m_bJobValid)
            {
                if (!m_CurrentJob.IsCompleted)
                {
                    m_CurrentJob.Complete();
                }
            }
        }
        //--------------------------------------------------------
        public void Tick()
        {
            QueryDirtyData();
            if (m_vDirtySets!=null && m_vDirtySets.Count > 0)
            {
                m_CurrentJob = BuildJob();
                m_bJobValid = true;
                JobHandle.ScheduleBatchedJobs();
            }
        }
        //--------------------------------------------------------
        private void QueryDirtyData()
        {
            if (m_vNeedRebuildGraphics == null)
                return;

           m_vDirtyIndices.Clear();
            m_vDirtySets.Clear();
            foreach (var op in m_vNeedRebuildGraphics)
            {
                var g = op.Key;
                var opt = op.Value;
                if (CheckOpt(opt, EOperationType.Add))
                {
                    InnerAddItem(g);
                    continue;
                }

                if (CheckOpt(opt, EOperationType.Remove))
                {
                    InnerRemoveItem(g);
                    continue;
                }

                if (CheckOpt(opt, EOperationType.TransformChange))
                {
                    InnerOnItemTransformChange(g);
                }

                if (CheckOpt(opt, EOperationType.VertexProperty))
                {
                    InnerOnVertexPropertyChange(g);
                }

                if (CheckOpt(opt, EOperationType.Active))
                {
                    InnerActive(g);
                }

                if (CheckOpt(opt, EOperationType.DeActive))
                {
                    InnerDeActive(g);
                }

            }

            m_vNeedRebuildGraphics.Clear();
        }
        //--------------------------------------------------------
        private bool CheckOpt(EOperationType state, EOperationType opt)
        {
            return (state & opt) != EOperationType.None;
        }
        //--------------------------------------------------------
        private void InnerAddItem(AGraphic item)
        {
            int index = -1;
            int per_quad_index = -1;
            if (item.BuildIndex < 0)
            {
                if (!CheckBufferOverFlow())
                {
                    return;
                }
                var data_index = m_BufferInfo.Add();
                index = data_index.x;
                per_quad_index = data_index.y;

                if (m_vItems == null) m_vItems = new Dictionary<int, AGraphic>(128);
                m_vItems.Add(index, item);
            }
            else
            {
                index = item.BuildIndex;
                per_quad_index = m_BufferInfo.transform_job_datas[index].per_quad_index;
            }

            item.BuildIndex = index;
            item.Batch = this;

            HUDUtils.TempJobDatas.index = index;
            HUDUtils.TempJobDatas.is_active = (byte)1;
            HUDUtils.TempJobDatas.per_quad_index = per_quad_index;
            HUDUtils.TempJobDatas.grapicType = item.GetGraphicType();
            HUDUtils.TempJobDatas.gscale = item.gscale;
            HUDUtils.TempJobDatas.spacing = item.spacing;
            HUDUtils.TempJobDatas.local_position = item.localPosition;
            HUDUtils.TempJobDatas.local_scale = item.localScale;
            HUDUtils.TempJobDatas.valid_quad = item.quadCount;
            HUDUtils.TempJobDatas.extend.x = (half)item.extend.x;
            HUDUtils.TempJobDatas.extend.y = (half)item.extend.y;
            HUDUtils.TempJobDatas.extend.z = (half)item.extend.z;
            HUDUtils.TempJobDatas.extend.w = (half)item.extend.w;
            HUDUtils.TempJobDatas.flag = EDirtyFlag.ETransform | EDirtyFlag.EQuad;

            int count = math.min(item.quadUV0.Length, m_BufferInfo.info.quad_count);
            count = math.min(count, item.quadCount);
            for (int i = 0; i < count; ++i)
            {
                int offset = per_quad_index + i;
                HUDUtils.TempQuadData.uv0 = item.quadUV0[i];
                HUDUtils.TempQuadData.tparams = item.quadParams[i];
                HUDUtils.TempQuadData.color = item.color;
                HUDUtils.TempQuadData.size = item.quadSizes[i];
                m_BufferInfo.quad_job_datas[offset] = HUDUtils.TempQuadData;
            }

            m_BufferInfo.transform_job_datas[index] = HUDUtils.TempJobDatas;
            AddIndexToDirty(index);
        }
        //--------------------------------------------------------
        private void AddIndexToDirty(int index)
        {
            if (m_vDirtySets ==null || !m_vDirtySets.Contains(index))
            {
                if(m_vDirtySets == null)
                {
                    m_vDirtySets = new HashSet<int>(32);
                }
                m_vDirtySets.Add(index);
                m_vDirtyIndices.Add(index);
            }
        }
        //--------------------------------------------------------
        private void InnerRemoveItem(AGraphic item)
        {
            if (item.BuildIndex < 0 || m_vItems == null)
                return;

            int last = m_BufferInfo.Length - 1;
            AGraphic last_item = m_vItems[last];
            int index = item.BuildIndex;

            m_BufferInfo.RemoveSwapAtBack(index);

            m_vItems[index] = last_item;
            last_item.BuildIndex = index;
            m_vItems.Remove(last);
            item.BuildIndex = -1;
            item.Batch = null;
        }
        //--------------------------------------------------------
        private void InnerActive(AGraphic item)
        {
            int index = -1;
            if (item.BuildIndex < 0)
            {
                return;
            }

            index = item.BuildIndex;

            HUDUtils.TempJobDatas = m_BufferInfo.transform_job_datas[index];
            if (HUDUtils.TempJobDatas.is_active == 0)
            {
                HUDUtils.TempJobDatas.is_active = (byte)1;
            }
        }
        //--------------------------------------------------------
        private void InnerDeActive(AGraphic item)
        {
            int index = -1;
            if (item.BuildIndex < 0)
            {
                return;
            }

            index = item.BuildIndex;

            HUDUtils.TempJobDatas = m_BufferInfo.transform_job_datas[index];
            HUDUtils.TempJobDatas.is_active = (byte)0;
        }
        //--------------------------------------------------------
        public void OnItemTransformChange(AGraphic item)
        {
            if (m_vOperationQueue == null)
                m_vOperationQueue = new Queue<Operation>(32);
            var operation = new Operation() { opt = EOperationType.TransformChange, item = item };
            m_vOperationQueue.Enqueue(operation);
        }
        //--------------------------------------------------------
        private void InnerOnItemTransformChange(AGraphic item)
        {
            int index = -1;
            if (item.BuildIndex < 0)
            {
                return;
            }

            index = item.BuildIndex;

            HUDUtils.TempJobDatas = m_BufferInfo.transform_job_datas[index];
            HUDUtils.TempJobDatas.spacing = item.spacing;
            HUDUtils.TempJobDatas.gscale = item.gscale;
            HUDUtils.TempJobDatas.local_position = item.localPosition;
            HUDUtils.TempJobDatas.local_scale = item.localScale;
            HUDUtils.TempJobDatas.valid_quad = item.quadCount;
            HUDUtils.TempJobDatas.extend.x = (half)item.extend.x;
            HUDUtils.TempJobDatas.extend.y = (half)item.extend.y;
            HUDUtils.TempJobDatas.extend.z = (half)item.extend.z;
            HUDUtils.TempJobDatas.extend.w = (half)item.extend.w;

            m_BufferInfo.transform_job_datas[index] = HUDUtils.TempJobDatas;

            AddIndexToDirty(index);
        }
        //--------------------------------------------------------
        public void OnVertexPropertyChange(AGraphic item)
        {
            var operation = new Operation() { opt = EOperationType.VertexProperty, item = item };
            if (m_vOperationQueue == null) m_vOperationQueue = new Queue<Operation>(32);
            m_vOperationQueue.Enqueue(operation);
        }
        //--------------------------------------------------------
        private void InnerOnVertexPropertyChange(AGraphic item)
        {
            int index = -1;
            int per_quad_index = -1;
            if (item.BuildIndex < 0)
            {
                return;
            }

            index = item.BuildIndex;

            HUDUtils.TempJobDatas = m_BufferInfo.transform_job_datas[index];
            HUDUtils.TempJobDatas.flag |= EDirtyFlag.EQuad;
            per_quad_index = HUDUtils.TempJobDatas.per_quad_index;

            int count = math.min(item.quadUV0.Length, m_BufferInfo.info.quad_count);
            count = math.min(count, item.quadCount);
            for (int i = 0; i < count; ++i)
            {
                int offset = per_quad_index + i;
                HUDUtils.TempQuadData.uv0 = item.quadUV0[i];
                HUDUtils.TempQuadData.tparams = item.quadParams[i];
                HUDUtils.TempQuadData.color = item.color;
                HUDUtils.TempQuadData.size = item.quadSizes[i];
                m_BufferInfo.quad_job_datas[offset] = HUDUtils.TempQuadData;
            }

            m_BufferInfo.transform_job_datas[index] = HUDUtils.TempJobDatas;

            AddIndexToDirty(index);
        }
        //--------------------------------------------------------
        private JobHandle BuildJob()
        {
            BatchRebuildJob job = new BatchRebuildJob()
            {
                quad_count = m_BufferInfo.info.quad_count,
                font_uv_padding = m_pSystem.font_uv_padding,
                fontAltas_width = m_pSystem.font_altas_width,
                fontAltas_height = m_pSystem.font_altas_height,
                vertices = m_BufferInfo.vertex_datas.ToNativeSlice(),
                build_trans_data = m_BufferInfo.transform_job_datas.ToNativeSlice(),
                build_quad_data = m_BufferInfo.quad_job_datas.ToNativeSlice(),
                dirty_indices = m_vDirtyIndices
            };

            var job_handle = job.Schedule(m_vDirtyIndices.Length, 16);
            return job_handle;
        }
        //--------------------------------------------------------
        public void PushOperation(AGraphic graphic, EOperationType opt)
        {
            EOperationType current;
            if (m_vNeedRebuildGraphics!=null && m_vNeedRebuildGraphics.TryGetValue(graphic, out current))
            {
                if (CheckOpt(opt, EOperationType.Add))
                {
                    current &= ~EOperationType.Remove;
                }

                if (CheckOpt(current, EOperationType.Remove))
                {
                    current &= ~EOperationType.Add;
                }

                if (CheckOpt(opt, EOperationType.Active))
                {
                    current &= ~EOperationType.DeActive;
                }

                if (CheckOpt(current, EOperationType.DeActive))
                {
                    current &= ~EOperationType.Active;
                }


                current |= opt;
                m_vNeedRebuildGraphics[graphic] = current;
            }
            else
            {
                if (m_vNeedRebuildGraphics == null) m_vNeedRebuildGraphics = new Dictionary<AGraphic, EOperationType>(32);
                m_vNeedRebuildGraphics.Add(graphic, opt);
            }
        }
        //--------------------------------------------------------
        private bool CheckBufferOverFlow()
        {
            if (m_BufferInfo.IsUseOut())
            {
                Debug.LogError("buffer use out!");
                return false;
            }

            return true;
        }
    }
}

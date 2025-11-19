/********************************************************************
生成日期:	11:11:2025
类    名: 	HudRenderCulling
作    者:	HappLI
描    述:	渲染裁剪和排序
*********************************************************************/
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using static UnityEditor.Experimental.GraphView.Port;

namespace Framework.HUD.Runtime
{
    //--------------------------------------------------------
    //HudRenderCulling
    //--------------------------------------------------------
    internal unsafe class HudRenderCulling
    {
        private TransformAccessArray m_transformArray;
        private NativeList<TransformData> m_vTransformData;
        private NativeList<TransformSort> m_vTransformSort;
        private NativeQueue<ushort> m_vRemaining;
        private int m_nReadIndex;
        private bool m_bDispose;
        private int m_nCapacity;
        private HudSystem m_pSystem;
        public HudRenderCulling(HudSystem system, int _capacity)
        {
            m_pSystem = system;
            m_nCapacity = _capacity;
            m_bDispose = false;
            m_nReadIndex = 0;

            m_transformArray = new TransformAccessArray(_capacity);
            m_vRemaining = new NativeQueue<ushort>(Allocator.Persistent);
            m_vTransformData = new NativeList<TransformData>(_capacity, Allocator.Persistent);
            m_vTransformSort = new NativeList<TransformSort>(_capacity, Allocator.Persistent);
        }
        //--------------------------------------------------------
        public NativeList<TransformData> transformData
        {
            get { return m_vTransformData; }
        }
        //--------------------------------------------------------
        public NativeList<TransformSort> transformSort
        {
            get { return m_vTransformSort; }
        }
        //--------------------------------------------------------
        private void TryExpansion()
        {
            if (m_nReadIndex < m_vTransformData.Length) return;
            int len = m_vTransformData.Length + m_nCapacity;
            m_vTransformData.Capacity = len;
            m_transformArray.capacity = len;
        }
        //--------------------------------------------------------
        public int Add(HudController controller, bool root)
        {
            if (m_bDispose) return 0;
            if (m_vRemaining.Count > 0)
            {
                int remainingindex = m_vRemaining.Dequeue();
                m_transformArray[remainingindex] = controller.GetFollowTarget();
                TransformData data = new TransformData();
                data.root = root ? (byte)1 : (byte)0;
                data.localToWorld = controller.GetWorldMatrix();
                m_vTransformData[remainingindex] = data;
                return remainingindex;
            }
            else
            {
                TryExpansion();
                int index = m_nReadIndex;
                m_transformArray.Add(controller.GetFollowTarget());
                TransformData data = new TransformData();
                data.root = root ? (byte)1 : (byte)0;
                data.localToWorld = controller.GetWorldMatrix();
                m_vTransformData.Add(data);
                m_nReadIndex++;
                return index;
            }
        }
        //--------------------------------------------------------
        public void Remove(int index)
        {
            if (m_bDispose) return;
            if (index<0 || index >= m_vTransformData.Length) return;
            m_transformArray[index] = null;
            TransformData data = m_vTransformData[index];
            data.disable = 1;
            m_vTransformData[index] = data;
            m_vRemaining.Enqueue((ushort)index);
        }
        //--------------------------------------------------------
        public void SetEnable(int index)
        {
            if (m_bDispose) return;
            if (index < 0 || index >= m_vTransformData.Length) return;
            TransformData data = m_vTransformData[index];
            data.disable = 0;
            m_vTransformData[index] = data;
        }
        //--------------------------------------------------------
        public void SetDisable(int index)
        {
            if (m_bDispose) return;
            if (index < 0 || index >= m_vTransformData.Length) return;
            TransformData data = m_vTransformData[index];
            data.disable = 1;
            m_vTransformData[index] = data;
        }
        //--------------------------------------------------------
        public void UpdateTransform(int index, Matrix4x4 worldMatrix)
        {
            if (m_bDispose) return;
            if (index < 0 || index >= m_vTransformData.Length) return;
            TransformData data = m_vTransformData[index];
            data.localToWorld = worldMatrix;
            m_vTransformData[index] = data;
        }
        //--------------------------------------------------------
        public void SetBounds(int index, float2 center, float2 size)
        {
            if (m_bDispose) return;
            if (index < 0 || index >= m_vTransformData.Length) return;
            TransformData data = m_vTransformData[index];
            data.boundCenter = center;
            data.boundSize = size;
            m_vTransformData[index] = data;
        }
        //--------------------------------------------------------
        public JobHandle ToJob(float4x4 vpMatrix, float3 forward)
        {
            if (m_bDispose) return new JobHandle();
            Profiler.BeginSample("TransformCullingSort");

            LocalToWorldJob job = new LocalToWorldJob();
            job.transformData = transformData;
            job.vpMatrix = vpMatrix;
            job.forward = forward;
            JobHandle localToWorldJobHandle = job.ScheduleReadOnly(m_transformArray, 32);

            TransformCullingJob sortJob = new TransformCullingJob();
            sortJob.transformSort = m_vTransformSort;
            sortJob.transformdataList = m_vTransformData;
            sortJob.vpMatrix = vpMatrix;
            sortJob.forward = forward;
            JobHandle jobHandle = sortJob.Schedule(localToWorldJobHandle);
            Profiler.EndSample();
            return jobHandle;
        }
        //--------------------------------------------------------
        public void Dispose()
        {
            m_bDispose = true;
            m_vRemaining.Dispose();
            m_vTransformData.Dispose();
            m_vTransformSort.Dispose();
        }
    }
}

/********************************************************************
生成日期:	11:11:2025
类    名: 	HudJobData
作    者:	HappLI
描    述:	
*********************************************************************/
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace Framework.HUD.Runtime
{
    internal unsafe class CanvasCollector : IDisposable
    {
        private TransformAccessArray transformArray;
        internal NativeList<TransformData> transformData;
        internal NativeList<TransformSort> transformSort;
        private NativeQueue<ushort> m_vRemaining;
        private int m_nReadIndex;
        private bool m_bDispose;
        private int m_nCapacity;
        //--------------------------------------------------------
        public CanvasCollector(int capacity)
        {
            m_nCapacity = capacity;
            m_bDispose = false;
            transformArray = new TransformAccessArray(capacity);
            m_vRemaining = new NativeQueue<ushort>(Allocator.Persistent);
            transformData = new NativeList<TransformData>(capacity, Allocator.Persistent);
            transformSort = new NativeList<TransformSort>(capacity, Allocator.Persistent);
            m_nReadIndex = 0;
        }
        //--------------------------------------------------------
        public int count
        {
            get { return m_nReadIndex; }
        }
        //--------------------------------------------------------
        private void TryExpansion()
        {
            if (m_nReadIndex < transformData.Length) return;
            int len = transformData.Length + m_nCapacity;
            transformArray.capacity = len;
            transformData.Capacity = len;
        }
        //--------------------------------------------------------
        public int Add(Transform transform, bool root)
        {
            if (m_bDispose) return 0;
            if (m_vRemaining.Count > 0)
            {
                int remainingindex = m_vRemaining.Dequeue();
                transformArray[remainingindex] = transform;
                TransformData data = new TransformData();
                data.root = root ? (byte)1 : (byte)0;
                transformData[remainingindex] = data;
                return remainingindex;
            }
            else
            {
                TryExpansion();
                int index = m_nReadIndex;
                transformArray.Add(transform);
                TransformData data = new TransformData();
                data.root = root ? (byte)1 : (byte)0;
                transformData.Add(data);
                m_nReadIndex++;
                return index;
            }
        }
        //--------------------------------------------------------
        public void Remove(int index)
        {
            if (m_bDispose) return;
            if (index >= transformData.Length) return;
            transformArray[index] = null;
            TransformData data = transformData[index];
            data.disable = 1;
            transformData[index] = data;
            m_vRemaining.Enqueue((ushort)index);
        }
        //--------------------------------------------------------
        public void SetEnable(int index, Transform transform)
        {
            if (m_bDispose) return;
            if (index >= transformData.Length) return;
            transformArray[index] = transform;
            TransformData data = transformData[index];
            data.disable = 0;
            transformData[index] = data;
        }
        //--------------------------------------------------------
        public void SetDisable(int index)
        {
            if (m_bDispose) return;
            if (index >= transformData.Length) return;
            transformArray[index] = null;
            TransformData data = transformData[index];
            data.disable = 1;
            transformData[index] = data;
        }
        //--------------------------------------------------------
        public void SetBounds(int index, float2 center, float2 size)
        {
            if (m_bDispose) return;
            if (index >= transformData.Length) return;
            transformArray[index] = null;
            TransformData data = transformData[index];
            data.boundCenter = center;
            data.boundSize = size;
            transformData[index] = data;
        }
        //--------------------------------------------------------
        public JobHandle ToJob(float4x4 vpMatrix, float3 forward)
        {
            if (m_bDispose) return new JobHandle();
            Profiler.BeginSample("CanvasCollector");
            LocalToWorldJob job = new LocalToWorldJob();
            job.transformData = transformData;
            job.vpMatrix = vpMatrix;
            job.forward = forward;
            JobHandle localToWorldJobHandle = job.ScheduleReadOnly(transformArray, 32);
            TransformSortJob sortJob = new TransformSortJob();
            sortJob.transformSort = transformSort;
            sortJob.transformdataList = transformData;
            JobHandle jobHandle = sortJob.Schedule(localToWorldJobHandle);
            Profiler.EndSample();
            return jobHandle;
        }
        //--------------------------------------------------------
        public void Dispose()
        {
            m_bDispose = true;
            m_vRemaining.Dispose();
            transformData.Dispose();
            transformArray.Dispose();
            transformSort.Dispose();
        }
    }
    //--------------------------------------------------------
    [BurstCompile]
    public unsafe struct LocalToWorldJob : IJobParallelForTransform
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<TransformData> transformData;

        public float4x4 vpMatrix;

        public float3 forward;

        public unsafe void Execute(int index, TransformAccess transform)
        {
            TransformData data = transformData[index];
            if (data.disable == 0)
            {
                //Vector3 up = transform.rotation * Vector3.up;
                //quaternion rotation = quaternion.LookRotation(forward, Vector3.up);
                float angle = transform.rotation.eulerAngles.z;
                quaternion rotation = quaternion.AxisAngle(forward, angle * Mathf.Deg2Rad);
                Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;
                float4x4 localToWorld = float4x4.TRS(localToWorldMatrix.GetPosition(), rotation, localToWorldMatrix.lossyScale);

                if (data.root == 1)
                {
                    float4x4 mvp = math.mul(vpMatrix, localToWorld);
                    float zvalue;
                    bool culling = Culling(mvp, data, out zvalue);
                    data.culling = (byte)(culling ? 1 : 0);
                    data.zvalue = zvalue;
                }
                data.localToWorld = localToWorld;
                transformData[index] = data;
            }
        }

        public bool Culling(float4x4 mvp, TransformData data, out float zvalue)
        {
            if (InView(mvp, float2.zero, out zvalue)) return false;
            float2 center = data.boundCenter / 100f;
            float2 halfsize = data.boundSize / 200f;
            float2 leftDownPos = center - halfsize;
            float2 leftUpPos = center - new float2(halfsize.x, -halfsize.y);
            float2 rightUpPos = center + halfsize;
            float2 rightDownPos = center + new float2(halfsize.x, -halfsize.y);
            if (InView(mvp, leftDownPos, out zvalue)) return false;

            if (InView(mvp, leftUpPos, out zvalue)) return false;

            if (InView(mvp, rightUpPos, out zvalue)) return false;

            if (InView(mvp, rightDownPos, out zvalue)) return false;

            return true;
        }

        public unsafe bool InView(float4x4 mvp, float2 pos, out float zvalue)
        {
            float4 mvpPos = math.mul(mvp, new float4(pos.x, pos.y, 0, 1));
            float3 view = mvpPos.xyz / mvpPos.w;
            bool inview = (view.x >= -1f && view.x <= 1f)
                          && (view.y >= -1f && view.y <= 1f)
                          && (view.z >= -1 && view.z <= 1f);
            zvalue = view.z;
            return inview;
        }
    }

    public struct PointDataComparer : IComparer<TransformSort>
    {
        public int Compare(TransformSort a, TransformSort b)
        {
            return b.weights.CompareTo(a.weights);
        }
    }

    [BurstCompile]
    public unsafe struct TransformSortJob : IJob
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeList<TransformData> transformdataList;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<TransformSort> transformSort;

        public void Execute()
        {
            transformSort.Clear();
            for (int i = 0; i < transformdataList.Length; i++)
            {
                TransformData transformData = transformdataList[i];
                if (transformData.culling == 0
                    && transformData.disable == 0
                    && transformData.root == 1)
                {
                    TransformSort sortdata = new TransformSort();
                    sortdata.transId = i;
                    sortdata.weights = transformData.zvalue;
                    transformSort.Add(sortdata);
                }
            }
            transformSort.Sort(new PointDataComparer());
        }
    }

    [BurstCompile]
    public unsafe struct FillGPUInstanceFloat4Job : IJob
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<IntPtr> renderDataPtr;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeMultiHashMap<int, RenderDataState<float4>> renderdataHashMap;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeList<TransformData> transformdata;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<TransformSort> transformSort;

        public int batchMaxCount;

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        void GetValueArraySort()
        {
            int count = 0;
            for (int i = 0; i < transformSort.Length; i++)
            {
                RenderDataState<float4> datestate;
                int index = transformSort[i].transId;
                if (renderdataHashMap.TryGetFirstValue(index, out datestate, out var iterator))
                {
                    do
                    {
                        if (datestate.show == 1)
                        {
                            SetData(count, datestate.data);
                            count++;
                        }
                    } while (renderdataHashMap.TryGetNextValue(out datestate, ref iterator));
                }
            }
        }

        public void SetData(int index, float4 value)
        {
            int arraypart = index / batchMaxCount;
            int arrayindex = index % batchMaxCount;
            float4* ptr = (float4*)renderDataPtr[arraypart];
            ptr[arrayindex] = value;
        }

        public void Execute()
        {
            GetValueArraySort();
        }
    }

    [BurstCompile]
    public unsafe struct FillGPUInstanceFloat4x4Job : IJob
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<IntPtr> renderDataPtr;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeMultiHashMap<int, RenderDataState<float4x4>> renderdataHashMap;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeList<TransformData> transformdata;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<TransformSort> transformSort;

        public int batchMaxCount;

        [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
        void GetValueArraySort()
        {
            int count = 0;
            for (int i = 0; i < transformSort.Length; i++)
            {
                RenderDataState<float4x4> datestate;
                int index = transformSort[i].transId;
                if (renderdataHashMap.TryGetFirstValue(index, out datestate, out var iterator))
                {
                    do
                    {
                        if (datestate.show == 1)
                        {
                            SetData(count, datestate.data);
                            count++;
                        }
                    } while (renderdataHashMap.TryGetNextValue(out datestate, ref iterator));
                }
            }
        }

        public void SetData(int index, float4x4 value)
        {
            int arraypart = index / batchMaxCount;
            int arrayindex = index % batchMaxCount;
            float4x4* ptr = (float4x4*)renderDataPtr[arraypart];
            ptr[arrayindex] = value;
        }

        public void Execute()
        {
            GetValueArraySort();
        }
    }

    [BurstCompile]
    public unsafe struct FillGPUInstanceTransformJob : IJob
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeList<TransformData> transformdata;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<IntPtr> renderDataPtr;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeMultiHashMap<int, RenderDataState<int>> renderdataHashMap;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<TransformSort> transformSort;

        [NativeDisableUnsafePtrRestriction]
        public int* len;

        public int batchMaxCount;

        void GetValueArraySort()
        {
            int count = 0;
            *len = 0;
            for (int i = 0; i < transformSort.Length; i++)
            {
                RenderDataState<int> datestate;
                int index = transformSort[i].transId;
                if (renderdataHashMap.TryGetFirstValue(index, out datestate, out var iterator))
                {
                    do
                    {
                        if (datestate.show == 1)
                        {
                            int transindex = datestate.data;
                            SetData(count, transformdata[transindex].localToWorld);
                            count++;
                            *len = count;
                        }
                    } while (renderdataHashMap.TryGetNextValue(out datestate, ref iterator));
                }
            }
        }

        public void SetData(int index, float4x4 value)
        {
            int arraypart = index / batchMaxCount;
            int arrayindex = index % batchMaxCount;
            float4x4* ptr = (float4x4*)renderDataPtr[arraypart];
            ptr[arrayindex] = value;
        }

        public void Execute()
        {
            GetValueArraySort();
        }
    }

}

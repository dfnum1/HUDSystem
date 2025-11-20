/********************************************************************
生成日期:	11:11:2025
类    名: 	HudJobData
作    者:	HappLI
描    述:	
*********************************************************************/
using log4net.Util;
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
    public struct PointDataComparer : IComparer<TransformSort>
    {
        public int Compare(TransformSort a, TransformSort b)
        {
            return b.weights.CompareTo(a.weights);
        }
    }
    [BurstCompile]
    public unsafe struct LocalToWorldJob : IJobParallelForTransform
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<TransformData> transformData;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeList<ushort> transformDataIndex;

        public float4x4 vpMatrix;

        public float3 forward;

        public unsafe void Execute(int index, TransformAccess transform)
        {
            int dataIndex = transformDataIndex[index];
            if (dataIndex < 0 || dataIndex >= transformData.Length)
                return;

            TransformData data = transformData[dataIndex];
            if (data.disable == 0)
            {
                if(data.transformJob!=0 && transform.isValid)
                {
                    Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;
                    Vector3 offsetPos = data.offsetPosition;
                    //Vector3 up = transform.rotation * Vector3.up;
                    //quaternion rotation = quaternion.LookRotation(forward, Vector3.up);
                    quaternion rotation = quaternion.identity;
                    if(data.allowRotation!=0)
                    {
                        float angle = transform.rotation.eulerAngles.z;
                        rotation = quaternion.AxisAngle(forward, angle * Mathf.Deg2Rad);
                        quaternion offsetQuat = quaternion.EulerXYZ(data.offsetRotation * Mathf.Deg2Rad);
                        rotation = math.mul(rotation, offsetQuat);
                    }
                    float3 scale = Vector3.one;
                    if (data.allowScale != 0)
                    {
                        scale = localToWorldMatrix.lossyScale;
                    }
                    data.localToWorld = float4x4.TRS(localToWorldMatrix.GetPosition() + offsetPos, rotation, scale);
                }

                if (data.root == 1)
                {
                    float4x4 mvp = math.mul(vpMatrix, data.localToWorld);
                    float zvalue;
                    bool culling = Culling(mvp, data, out zvalue);
                    data.culling = (byte)(culling ? 1 : 0);
                    data.zvalue = zvalue;
                }
                transformData[dataIndex] = data;
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

    [BurstCompile]
    public unsafe struct TransformCullingJob : IJob
    {
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeList<TransformData> transformdataList;

        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<TransformSort> transformSort;

        public float4x4 vpMatrix;

        public float3 forward;

        public void Execute()
        {
            transformSort.Clear();
            for (int i = 0; i < transformdataList.Length; i++)
            {
                TransformData transformData = transformdataList[i];
                if (transformData.disable != 0)
                    continue;
                if (transformData.root != 1)
                    continue;

                //! culling check
                float4x4 mvp = math.mul(vpMatrix, transformData.localToWorld);
                float zvalue;
                bool culling = Culling(mvp, transformData, out zvalue);
                transformData.culling = (byte)(culling ? 1 : 0);
                transformData.zvalue = zvalue;

                if (transformData.culling == 0)
                {
                    TransformSort sortdata = new TransformSort();
                    sortdata.transId = i;
                    sortdata.weights = transformData.zvalue;
                    transformSort.Add(sortdata);
                }
            }
            transformSort.Sort(new PointDataComparer());
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

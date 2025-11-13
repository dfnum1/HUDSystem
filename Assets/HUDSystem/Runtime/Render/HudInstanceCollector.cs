/********************************************************************
生成日期:	11:11:2025
类    名: 	HudInstanceData
作    者:	HappLI
描    述:	
*********************************************************************/
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Framework.HUD.Runtime
{
    internal unsafe class HudInstanceCollector : IDisposable
    {
        private HudGpuRenderData m_pGpuData;
        private Dictionary<string, GPUInstanceData<float4x4, Matrix4x4>> m_float4x4Values;
        private NativeArray<JobHandle> m_JobHandles;
        private int m_nCapacity;
        private int* m_nInstanceCount;
        private bool m_bDispose;
        //--------------------------------------------------------
        public HudInstanceCollector(int _capacity)
        {
            m_nInstanceCount = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), Allocator.Persistent);
            *m_nInstanceCount = 0;
            m_bDispose = false;
            m_pGpuData = new HudGpuRenderData(_capacity);
            m_float4x4Values = new Dictionary<string, GPUInstanceData<float4x4, Matrix4x4>>();
            m_JobHandles = new NativeArray<JobHandle>(8, Allocator.Persistent); ;
            m_nCapacity = _capacity;
        }
        //--------------------------------------------------------
        public int instanceCount
        {
            get { return *m_nInstanceCount; }
        }
        //--------------------------------------------------------
        public bool expansion
        {
            get { return m_pGpuData.expansion; }
            set { m_pGpuData.expansion = value; }
        }
        //--------------------------------------------------------
        public void FullPropertyBlack(int index, MaterialPropertyBlock propertyBlock)
        {
            foreach (var item in m_float4x4Values)
            {
                propertyBlock.SetMatrixArray(item.Key, item.Value.GetArray(index));
            }
        }
        //--------------------------------------------------------
        public Matrix4x4[] GetObjectToWorld(int index)
        {
            return m_pGpuData.renderData.arrays[index];
        }
        //--------------------------------------------------------
        internal GPUInstanceData<float4x4, Matrix4x4> TryGetFloat4x4(string name)
        {
            GPUInstanceData<float4x4, Matrix4x4> value;
            if (!m_float4x4Values.TryGetValue(name, out value))
            {
                value = new GPUInstanceData<float4x4, Matrix4x4>(m_nCapacity);
                m_float4x4Values[name] = value;
            }
            return value;
        }
        //--------------------------------------------------------
        public int AddFloat4x4(string name, int hashcode, RenderDataState<float4x4> value)
        {
            if (m_bDispose) return -1;
            var vectordata = TryGetFloat4x4(name);
            return vectordata.Add(hashcode, value);
        }
        //--------------------------------------------------------
        public void SetFloat4x4(string name, int idx, RenderDataState<float4x4> value)
        {
            if (m_bDispose) return;
            var vectordata = TryGetFloat4x4(name);
            vectordata.Set(idx, value);
        }
        //--------------------------------------------------------
        public int AddTransformId(int hashcode, RenderDataState<int> transformId)
        {
            if (m_bDispose) return -1;
            return m_pGpuData.Add(hashcode, transformId);
        }
        //--------------------------------------------------------
        public void SetTransformId(int idx, RenderDataState<int> transformId)
        {
            if (m_bDispose) return;
            m_pGpuData.Set(idx, transformId);
        }
        //--------------------------------------------------------
        public void AddExpansionNotif(Action notify)
        {
            m_pGpuData.AddExpansionNotif(notify);
        }
        //--------------------------------------------------------
        public void RemoveExpansionNotif(Action notify)
        {
            m_pGpuData.RemoveExpansionNotif(notify);
        }
        //--------------------------------------------------------
        public void TriggerNotif()
        {
            m_pGpuData.TriggerNotif();
        }
        //--------------------------------------------------------
        public void Remove(int hashcode)
        {
            if (m_bDispose) return;
            m_pGpuData.Remove(hashcode);
            foreach (var value in m_float4x4Values)
            {
                value.Value.Remove(hashcode);
            }
        }
        //--------------------------------------------------------
        public unsafe JobHandle ToJob(HudRenderCulling culling, JobHandle dependsOn)
        {
            int jobHandleCount = 0;
            Profiler.BeginSample("RenderDataCollectorJob");
            var float4x4enumerator = m_float4x4Values.GetEnumerator();
            while (float4x4enumerator.MoveNext())
            {
                var value = float4x4enumerator.Current.Value;
                FillGPUInstanceFloat4x4Job job = new FillGPUInstanceFloat4x4Job();
                job.renderDataPtr = value.renderData.arrayPtrs;
                job.renderdataHashMap = value.renderdataHashMap;
                job.transformdata = culling.transformData;
                job.transformSort = culling.transformSort;
                job.batchMaxCount = HUDUtils.batchMaxCount;
                JobHandle jobHandle = job.Schedule(dependsOn);
                m_JobHandles[jobHandleCount] = jobHandle;
                jobHandleCount++;
            }
            {
                FillGPUInstanceTransformJob job = new FillGPUInstanceTransformJob();
                job.renderDataPtr = m_pGpuData.renderData.arrayPtrs;
                job.renderdataHashMap = m_pGpuData.renderdataHashMap;
                job.len = m_nInstanceCount;
                job.transformdata = culling.transformData;
                job.transformSort = culling.transformSort;
                job.batchMaxCount = HUDUtils.batchMaxCount;
                JobHandle jobHandle = job.Schedule(dependsOn);
                m_JobHandles[jobHandleCount] = jobHandle;
                jobHandleCount++;
            }
            Profiler.EndSample();
            if (jobHandleCount == 0) return dependsOn;
            Profiler.BeginSample("CombineDependencies");
            Profiler.BeginSample("NativeSlice");
            var nativeSlice = new NativeSlice<JobHandle>(m_JobHandles, 0, jobHandleCount);
            Profiler.EndSample();
            JobHandle jobhadle = JobHandle.CombineDependencies(nativeSlice);
            Profiler.EndSample();
            return jobhadle;
        }
        //--------------------------------------------------------
        public void Dispose()
        {
            UnsafeUtility.Free(m_nInstanceCount, Allocator.Persistent);
            foreach (var value in m_float4x4Values)
            {
                value.Value.Dispose();
            }
            m_float4x4Values.Clear();
            m_JobHandles.Dispose();
            m_pGpuData.Dispose();
            m_bDispose = true;
        }
    }
    //--------------------------------------------------------
    //!GPUInstanceData
    //--------------------------------------------------------
    internal struct GPUInstanceData<T, U> : IDisposable where T : unmanaged where U : unmanaged
    {
        public ArrayPtr<T, U> renderData;
        public NativeMultiHashMap<int, RenderDataState<T>> renderdataHashMap;
        private bool dispose;
        //--------------------------------------------------------
        public GPUInstanceData(int _capacity)
        {
            dispose = false;
            renderData = new ArrayPtr<T, U>(_capacity);
            renderdataHashMap = new NativeMultiHashMap<int, RenderDataState<T>>(_capacity, Allocator.Persistent);
        }
        //--------------------------------------------------------
        public U[] GetArray(int index)
        {
            return renderData.arrays[index];
        }
        //--------------------------------------------------------
        public unsafe int Add(int hashCode, RenderDataState<T> value)
        {
            if (dispose) return -1;
            int idx = 0;
            int lastcapacity = renderdataHashMap.Capacity;
            renderdataHashMap.Add(hashCode, value, out idx);
            int curcapacity = renderdataHashMap.Capacity;
            if (curcapacity != lastcapacity)
            {
                renderData.Resize(curcapacity);
            }
            return idx;
        }
        //--------------------------------------------------------
        public void Remove(int hashCode)
        {
            if (dispose) return;

            renderdataHashMap.Remove(hashCode);
        }
        //--------------------------------------------------------
        public unsafe void Set(int idx, RenderDataState<T> value)
        {
            if (dispose) return;
            UnsafeHashMapBucketData bucketdata = renderdataHashMap.GetUnsafeBucketData();
            UnsafeUtility.WriteArrayElement(bucketdata.values, idx, value);
        }
        //--------------------------------------------------------
        public void Dispose()
        {
            renderData.Dispose();
            renderdataHashMap.Dispose();
            //idxtoindex.Dispose();
            dispose = true;
        }
    }
}

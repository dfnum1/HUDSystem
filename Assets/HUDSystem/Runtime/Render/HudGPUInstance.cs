/********************************************************************
生成日期:	11:11:2025
类    名: 	HudGPUInstance
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
    internal struct GPUInstanceData<T, U> : IDisposable where T : unmanaged where U : unmanaged
    {
        public ArrayPtr<T, U> renderData;
        public NativeMultiHashMap<int, RenderDataState<T>> renderdataHashMap;
        private bool dispose;

        public GPUInstanceData(int _capacity)
        {
            dispose = false;
            renderData = new ArrayPtr<T, U>(_capacity);
            renderdataHashMap = new NativeMultiHashMap<int, RenderDataState<T>>(_capacity, Allocator.Persistent);
        }

        public U[] GetArray(int index)
        {
            return renderData.arrays[index];
        }

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

        public void Remove(int hashCode)
        {
            if (dispose) return;

            renderdataHashMap.Remove(hashCode);
        }

        public unsafe void Set(int idx, RenderDataState<T> value)
        {
            if (dispose) return;
            UnsafeHashMapBucketData bucketdata = renderdataHashMap.GetUnsafeBucketData();
            UnsafeUtility.WriteArrayElement(bucketdata.values, idx, value);
        }

        public void Dispose()
        {
            renderData.Dispose();
            renderdataHashMap.Dispose();
            //idxtoindex.Dispose();
            dispose = true;
        }
    }
    //--------------------------------------------------------
    //HudGPUTransform
    //--------------------------------------------------------
    internal class HudGPUTransform
    {
        public ArrayPtr<float4x4, Matrix4x4> renderData;
        public NativeMultiHashMap<int, RenderDataState<int>> renderdataHashMap;
        private HashSet<Action> expansionNotif;
        private bool dispose;

        public HudGPUTransform(int _capacity)
        {
            dispose = false;
            expansion = false;
            expansionNotif = new HashSet<Action>();
            renderData = new ArrayPtr<float4x4, Matrix4x4>(_capacity);
            renderdataHashMap = new NativeMultiHashMap<int, RenderDataState<int>>(_capacity, Allocator.Persistent);
        }

        public int count
        {
            get { return renderdataHashMap.Count(); }
        }

        public bool expansion
        {
            get;
            set;
        }

        public unsafe int Add(int hashCode, RenderDataState<int> value)
        {
            if (dispose) return -1;
            int idx = 0;
            int lastcapacity = renderdataHashMap.Capacity;
            renderdataHashMap.Add(hashCode, value, out idx);
            int curcapacity = renderdataHashMap.Capacity;
            if (curcapacity != lastcapacity)
            {
                renderData.Resize(curcapacity);
                expansion = true;
            }
            return idx;
        }

        public void Remove(int hashCode)
        {
            if (dispose) return;
            renderdataHashMap.Remove(hashCode);
        }

        public unsafe void Set(int idx, RenderDataState<int> value)
        {
            if (dispose) return;
            UnsafeHashMapBucketData bucketdata = renderdataHashMap.GetUnsafeBucketData();
            UnsafeUtility.WriteArrayElement(bucketdata.values, idx, value);
        }

        public void TriggerNotif()
        {
            foreach (var notif in expansionNotif)
            {
                notif?.Invoke();
            }
        }

        public void AddExpansionNotif(Action notify)
        {
            expansionNotif.Add(notify);
        }

        public void RemoveExpansionNotif(Action notify)
        {
            expansionNotif.Remove(notify);
        }

        public void Dispose()
        {
            dispose = true;
            renderData.Dispose();
            renderdataHashMap.Dispose();
            expansionNotif.Clear();
        }
    }
    //--------------------------------------------------------
    //HudGPUInstance
    //--------------------------------------------------------
    internal unsafe class HudGPUInstance : IDisposable
    {
        private HudGPUTransform m_gpuInstanceTransform;
        private Dictionary<string, GPUInstanceData<float4x4, Matrix4x4>> m_float4x4Values;
        private NativeArray<JobHandle> m_jobhandles;
        private int m_nCapacity;
        private int* m_nInstanceCount;
        private bool m_bDispose;
        //--------------------------------------------------------
        public HudGPUInstance(int _capacity)
        {
            m_nInstanceCount = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), Allocator.Persistent);
            *m_nInstanceCount = 0;
            m_bDispose = false;
            m_gpuInstanceTransform = new HudGPUTransform(_capacity);
            m_float4x4Values = new Dictionary<string, GPUInstanceData<float4x4, Matrix4x4>>();
            m_jobhandles = new NativeArray<JobHandle>(8, Allocator.Persistent); ;
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
            get { return m_gpuInstanceTransform.expansion; }
            set { m_gpuInstanceTransform.expansion = value; }
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
            return m_gpuInstanceTransform.renderData.arrays[index];
        }
        //--------------------------------------------------------
        private GPUInstanceData<float4x4, Matrix4x4> TryGetFloat4x4(string name)
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
            return m_gpuInstanceTransform.Add(hashcode, transformId);
        }
        //--------------------------------------------------------
        public void SetTransformId(int idx, RenderDataState<int> transformId)
        {
            if (m_bDispose) return;
            m_gpuInstanceTransform.Set(idx, transformId);
        }
        //--------------------------------------------------------
        public void AddExpansionNotif(Action notify)
        {
            m_gpuInstanceTransform.AddExpansionNotif(notify);
        }
        //--------------------------------------------------------
        public void RemoveExpansionNotif(Action notify)
        {
            m_gpuInstanceTransform.RemoveExpansionNotif(notify);
        }
        //--------------------------------------------------------
        public void TriggerNotif()
        {
            m_gpuInstanceTransform.TriggerNotif();
        }
        //--------------------------------------------------------
        public void Remove(int hashcode)
        {
            if (m_bDispose) return;
            m_gpuInstanceTransform.Remove(hashcode);
            foreach (var value in m_float4x4Values)
            {
                value.Value.Remove(hashcode);
            }
        }
        //--------------------------------------------------------
        public unsafe JobHandle ToJob(CanvasCollector transformCollector, JobHandle dependsOn)
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
                job.transformdata = transformCollector.transformData;
                job.transformSort = transformCollector.transformSort;
                job.batchMaxCount = HudRendererBatch.batchMaxCount;
                JobHandle jobHandle = job.Schedule(dependsOn);
                m_jobhandles[jobHandleCount] = jobHandle;
                jobHandleCount++;
            }
            {
                FillGPUInstanceTransformJob job = new FillGPUInstanceTransformJob();
                job.renderDataPtr = m_gpuInstanceTransform.renderData.arrayPtrs;
                job.renderdataHashMap = m_gpuInstanceTransform.renderdataHashMap;
                job.len = m_nInstanceCount;
                job.transformdata = transformCollector.transformData;
                job.transformSort = transformCollector.transformSort;
                job.batchMaxCount = HudRendererBatch.batchMaxCount;
                JobHandle jobHandle = job.Schedule(dependsOn);
                m_jobhandles[jobHandleCount] = jobHandle;
                jobHandleCount++;
            }
            Profiler.EndSample();
            if (jobHandleCount == 0) return dependsOn;
            Profiler.BeginSample("CombineDependencies");
            Profiler.BeginSample("NativeSlice");
            var nativeSlice = new NativeSlice<JobHandle>(m_jobhandles, 0, jobHandleCount);
            Profiler.EndSample();
            JobHandle jobhadle = JobHandle.CombineDependencies(nativeSlice);
            Profiler.EndSample();
            return jobhadle;
        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_nInstanceCount, Allocator.Persistent);
            foreach (var value in m_float4x4Values)
            {
                value.Value.Dispose();
            }
            m_float4x4Values.Clear();
            m_jobhandles.Dispose();
            m_gpuInstanceTransform.Dispose();
            m_bDispose = true;
        }
    }
}

/********************************************************************
生成日期:	11:11:2025
类    名: 	HudGpuRenderData
作    者:	HappLI
描    述:	Gpu 渲染数据
*********************************************************************/
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    internal unsafe struct HudGpuRenderData : IDisposable
    {
        public ArrayPtr<float4x4, Matrix4x4> renderData;
        public NativeMultiHashMap<int, RenderDataState<int>> renderdataHashMap;
        private HashSet<Action> m_vExpansionNotif;
        private bool m_bDispose;
        //--------------------------------------------------------
        public HudGpuRenderData(int _capacity)
        {
            m_bDispose = false;
            expansion = false;
            m_vExpansionNotif = new HashSet<Action>();
            renderData = new ArrayPtr<float4x4, Matrix4x4>(_capacity);
            renderdataHashMap = new NativeMultiHashMap<int, RenderDataState<int>>(_capacity, Allocator.Persistent);
        }
        //--------------------------------------------------------
        public int count
        {
            get { return renderdataHashMap.Count(); }
        }
        //--------------------------------------------------------
        public bool expansion
        {
            get;
            set;
        }
        //--------------------------------------------------------
        public unsafe int Add(int hashCode, RenderDataState<int> value)
        {
            if (m_bDispose) return -1;
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
        //--------------------------------------------------------
        public void Remove(int hashCode)
        {
            if (m_bDispose) return;
            renderdataHashMap.Remove(hashCode);
        }
        //--------------------------------------------------------
        public unsafe void Set(int idx, RenderDataState<int> value)
        {
            if (m_bDispose) return;
            UnsafeHashMapBucketData bucketdata = renderdataHashMap.GetUnsafeBucketData();
            UnsafeUtility.WriteArrayElement(bucketdata.values, idx, value);
        }
        //--------------------------------------------------------
        public void TriggerNotif()
        {
            foreach (var notif in m_vExpansionNotif)
            {
                notif?.Invoke();
            }
        }
        //--------------------------------------------------------
        public void AddExpansionNotif(Action notify)
        {
            m_vExpansionNotif.Add(notify);
        }
        //--------------------------------------------------------
        public void RemoveExpansionNotif(Action notify)
        {
            m_vExpansionNotif.Remove(notify);
        }
        //--------------------------------------------------------
        public void Clear()
        {
            m_vExpansionNotif.Clear();
            renderdataHashMap.Clear();
            renderData.Resize(renderdataHashMap.Capacity);
        }
        //--------------------------------------------------------
        public void Dispose()
        {
            m_bDispose = true;
            renderData.Dispose();
            renderdataHashMap.Dispose();
            m_vExpansionNotif.Clear();
        }
    }
}

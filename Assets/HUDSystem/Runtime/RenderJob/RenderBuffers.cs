/********************************************************************
生成日期:	11:11:2025
类    名: 	RenderBuffers
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.HUD.Runtime
{
    public struct Vertex
    {
        public float3 position;
        public float2 uv0;
        public float2 uv1;
        public Color32 color;
    }
    //--------------------------------------------------------
    internal struct BufferInfo
    {
        public int offset;
        public int length;
        public int quad_offset;
        public int quad_count;
    }
    //--------------------------------------------------------
    //NativeBufferSlice
    //--------------------------------------------------------
    internal class NativeBufferSlice<T> where T : struct
    {
        private int m_nOffset;
        private int m_nLength;
        private int m_nStride;
        private NativeBuffer<T> m_vBuffer;

        private int m_nUsed;
        public int used
        {
            get { return m_nUsed; }
        }
        //--------------------------------------------------------
        public NativeBufferSlice(NativeBuffer<T> buffer, int offset, int length, int stride)
        {
            m_vBuffer = buffer;
            m_nOffset = offset;
            m_nLength = length;
            m_nStride = stride;
            m_nUsed = 0;
        }
        //--------------------------------------------------------
        public T this[int index]
        {
            get
            {
                return m_vBuffer[m_nOffset + index];
            }

            set
            {
                m_vBuffer[m_nOffset + index] = value;
            }
        }
        //--------------------------------------------------------
        public bool IsUseout()
        {
            return used >= m_nLength;
        }
        //--------------------------------------------------------
        public int Add()
        {
            int ret = used;
            m_nUsed += m_nStride;
            return ret;
        }
        //--------------------------------------------------------
        public void RemoveSwawAtBack(int index)
        {
            m_vBuffer.RangeSwapBackTargetIndex(m_nOffset + index, m_nStride, m_nOffset + m_nUsed);
            m_nUsed -= m_nStride;
        }
        //--------------------------------------------------------
        public NativeSlice<T> ToNativeSlice()
        {
            return new NativeSlice<T>(m_vBuffer, m_nOffset, m_nLength);
        }
    }
    //--------------------------------------------------------
    //BufferSlice
    //--------------------------------------------------------
    internal class BufferSlice
    {
        private BufferInfo m_vInfo;
        private BatchData m_Buffer;
        public BufferInfo info { get { return m_vInfo; } }
        public BatchData buffer { get { return m_Buffer; } }

        public NativeBufferSlice<BuildTransformJobData> transform_job_datas { get; private set; }
        public NativeBufferSlice<BuildPerQuadData> quad_job_datas { get; private set; }
        public NativeBufferSlice<Vertex> vertex_datas { get; private set; }

        //--------------------------------------------------------
        public void Init(BatchData data, ref BufferInfo info)
        {
            m_Buffer = data;
            this.m_vInfo = info;
            transform_job_datas = new NativeBufferSlice<BuildTransformJobData>(data.build_transform_job_datas, info.offset, info.length, 1);
            quad_job_datas = new NativeBufferSlice<BuildPerQuadData>(data.build_quad_datas, info.quad_offset, info.quad_count * info.length, info.quad_count);
            vertex_datas = new NativeBufferSlice<Vertex>(data.vertex_datas, info.quad_offset * 4, info.quad_count * info.length * 4, info.quad_count * 4);
        }

        static Vector2Int ms_nRect;
        //--------------------------------------------------------
        public Vector2Int Add()
        {
            ms_nRect.x = transform_job_datas.Add();
            ms_nRect.y = quad_job_datas.Add();
            vertex_datas.Add();
            return ms_nRect;
        }
        //--------------------------------------------------------
        public int Length { get { return transform_job_datas.used; } }
        //--------------------------------------------------------
        public void RemoveSwapAtBack(int index)
        {
            var tdata = transform_job_datas[index];

            transform_job_datas.RemoveSwawAtBack(index);
            quad_job_datas.RemoveSwawAtBack(tdata.per_quad_index);
            vertex_datas.RemoveSwawAtBack(tdata.per_quad_index * 4);

            var n_tdata = transform_job_datas[index];
            if (index != n_tdata.index)
            {
                n_tdata.per_quad_index = tdata.per_quad_index;
                n_tdata.index = index;
                transform_job_datas[index] = n_tdata;
            }
        }
        //--------------------------------------------------------
        public bool IsUseOut()
        {
            return transform_job_datas.IsUseout();
        }
    }
}

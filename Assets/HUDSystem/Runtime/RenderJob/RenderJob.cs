/********************************************************************
生成日期:	11:11:2025
类    名: 	BatchRebuildJob
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    //--------------------------------------------------------
    //! CollectionMeshInfoOffset
    //--------------------------------------------------------
    internal struct CollectionMeshInfoOffset
    {
        public int group;
        public int offset;
        public int length;
        public int out_index;

        // 输出
        public int v_index;
        public int i_index;
    }
    //--------------------------------------------------------
    //! TransformIndex
    //--------------------------------------------------------
    public struct TransformIndex
    {
        public int index;
        public int buffer_offset;
        public int quad_buffer_offset;
        public int quad_count;
    }
    //--------------------------------------------------------
    //! BuildMeshInfoJob
    //--------------------------------------------------------
    [BurstCompile]
    internal struct BuildMeshInfoJob : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeArray<Vertex> vertices;

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeArray<BuildTransformJobData> transform_datas;
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeArray<BuildPerQuadData> quad_datas;
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeArray<TransformIndex> transform_indices;

        public NativeArray<CollectionMeshInfoOffset> offset_info;
        public int mesh_quad_count;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector3> out_poices;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector2> out_uv0;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector2> out_uv1;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Color32> out_colors;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> out_indices;

        public void Execute(int index)
        {
            var index_offset = offset_info[index];
            int length = index_offset.length;
            int base_index = index_offset.offset;
            int base_out_index = index_offset.out_index * mesh_quad_count * 4;
            index_offset.v_index = base_out_index;
            int base_out_index_index = index_offset.out_index * mesh_quad_count * 6;
            index_offset.i_index = base_out_index_index;
            int fill_quad_count = 0;

            for (int i = 0; i < length; ++i)
            {
                var tr_index = transform_indices[base_index + i];
                int quad_count = transform_indices[base_index + i].quad_count;
                var data = transform_datas[tr_index.buffer_offset + tr_index.index];
                int valid_len = data.valid_quad;
                int base_vertex_index = tr_index.quad_buffer_offset * 4 + tr_index.index * quad_count * 4;
                for (int j = 0; j < valid_len; ++j)
                {
                    int quad_vertex_index = base_vertex_index + j * 4;
                    out_indices[base_out_index_index++] = base_out_index + 0;
                    out_indices[base_out_index_index++] = base_out_index + 1;
                    out_indices[base_out_index_index++] = base_out_index + 2;
                    out_indices[base_out_index_index++] = base_out_index + 2;
                    out_indices[base_out_index_index++] = base_out_index + 3;
                    out_indices[base_out_index_index++] = base_out_index + 0;

                    for (int t = 0; t < 4; ++t)
                    {
                        int v_index = quad_vertex_index + t;
                        var v = vertices[v_index];
                        out_poices[base_out_index] = v.position;
                        out_uv0[base_out_index] = v.uv0;
                        out_uv1[base_out_index] = v.uv1;
                        out_colors[base_out_index] = v.color;
                        base_out_index++;
                    }

                    fill_quad_count++;
                }
            }

            index_offset.length = fill_quad_count; // 作为输出，填充了多少个quad
            offset_info[index] = index_offset;
        }
    }
    //--------------------------------------------------------
    //! BatchRebuildJob
    //--------------------------------------------------------
    [Unity.Burst.BurstCompile]
    internal struct BatchRebuildJob : IJobParallelFor
    {
        public int quad_count;
        public float font_uv_padding;
        public int fontAltas_width;
        public int fontAltas_height;

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public NativeSlice<Vertex> vertices;

        [NativeDisableContainerSafetyRestriction]
        public NativeSlice<BuildTransformJobData> build_trans_data;
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeSlice<BuildPerQuadData> build_quad_data;
        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> dirty_indices;

        public void Execute(int index)
        {
            index = dirty_indices[index];

            var data = build_trans_data[index];
            // 顶点索引偏移
            int valid_quad_count = data.valid_quad;
            int base_index = data.index * quad_count * 4;
            int per_quad_index = data.per_quad_index;
            float3 position = data.local_position;
            bool is_text = data.grapicType == EGraphicType.Text;
            float is_text_flag = is_text ? 1 : 0;
            half progress = data.extend.x;
            var gscale = data.gscale;
            var scale = data.local_scale * HUDUtils.fDefaultUnit / 2 * gscale;
            float2 sdf_scale = new float2(is_text_flag, scale.y);

            float left_x = position.x;
            for (int i = 0; i < valid_quad_count; ++i)
            {
                int vertex_offset = base_index + i * 4;
                var quad_data = build_quad_data[per_quad_index + i];
                var half_size = quad_data.size / 2;

                Vertex v0 = vertices[vertex_offset + 0];
                Vertex v1 = vertices[vertex_offset + 1];
                Vertex v2 = vertices[vertex_offset + 2];
                Vertex v3 = vertices[vertex_offset + 3];

                if ((data.flag & EDirtyFlag.ETransform) != EDirtyFlag.ENone)
                {
                    var p = position;
                    p.z = 0;

                    var tparams = quad_data.tparams;
                    if (is_text)
                    {
                        // lt
                        p.x = position.x + (tparams.z - font_uv_padding) * scale.x;
                        p.y = position.y + (tparams.w + font_uv_padding) * scale.y;
                        v1.position = p;

                        // record top y
                        float ty = p.y;

                        // lb
                        //p.x = p.x;
                        p.y = p.y - ((tparams.y + font_uv_padding * 2) * scale.y);
                        v0.position = p;

                        // record bottom y
                        float by = p.y;

                        // rt
                        p.x = p.x + ((tparams.x + font_uv_padding * 2) * scale.x);
                        p.y = ty;
                        v2.position = p;

                        // rb
                        //p.x = p.x;
                        p.y = by;
                        v3.position = p;

                        position.x = p.x + data.spacing;

                    }
                    else
                    {
                        float lx = position.x;
                        float rx = position.x + scale.x * half_size.x * 2;

                        //lb
                        p.x = lx;
                        p.y = position.y - scale.y * half_size.y;
                        v0.position = p;

                        //lt
                        p.y = position.y + scale.y * half_size.y;
                        v1.position = p;

                        p.x = lx + (rx - lx) * progress;
                        //rt
                        p.y = position.y + scale.y * half_size.y;
                        v2.position = p;

                        //rb
                        p.y = position.y - scale.y * half_size.y;
                        v3.position = p;

                        position.x += rx + data.spacing;
                    }

                    v1.uv1 = v2.uv1 = v3.uv1 = v0.uv1 = sdf_scale;
                }

                if ((data.flag & EDirtyFlag.EQuad) != EDirtyFlag.ENone)
                {
                    v0.color = v1.color = v2.color = v3.color = quad_data.color;

                    // uv0 // uv1 pack 到uv0
                    float2 uv_0 = float2.zero;
                    float4 rect = quad_data.uv0;
                    float xmin, xmax, ymin, ymax;
                    if (is_text)
                    {
                        xmin = (rect.x - font_uv_padding) / fontAltas_width;
                        xmax = (rect.x + font_uv_padding + rect.z) / fontAltas_width;
                        ymin = (rect.y - font_uv_padding) / fontAltas_height;
                        ymax = (rect.y + font_uv_padding + rect.w) / fontAltas_height;
                    }
                    else
                    {
                        xmin = (rect.x);
                        xmax = (rect.x + rect.z);
                        ymin = (rect.y);
                        ymax = (rect.y + rect.w);
                    }

                    uv_0.x = xmin;
                    uv_0.y = ymin;
                    v0.uv0 = uv_0;

                    uv_0.y = ymax;
                    v1.uv0 = uv_0;

                    uv_0.x = xmin + (xmax - xmin) * progress;
                    v2.uv0 = uv_0;

                    uv_0.y = ymin;
                    v3.uv0 = uv_0;

                }

                vertices[vertex_offset + 0] = v0;
                vertices[vertex_offset + 1] = v1;
                vertices[vertex_offset + 2] = v2;
                vertices[vertex_offset + 3] = v3;
            }

            float h_size = (position.x - left_x - data.spacing) / 2;
            for (int i = 0; i < valid_quad_count; ++i)
            {
                int vertex_offset = base_index + i * 4;
                Vertex v0 = vertices[vertex_offset + 0];
                Vertex v1 = vertices[vertex_offset + 1];
                Vertex v2 = vertices[vertex_offset + 2];
                Vertex v3 = vertices[vertex_offset + 3];
                v0.position.x -= h_size;
                v1.position.x -= h_size;
                v2.position.x -= h_size;
                v3.position.x -= h_size;
                vertices[vertex_offset + 0] = v0;
                vertices[vertex_offset + 1] = v1;
                vertices[vertex_offset + 2] = v2;
                vertices[vertex_offset + 3] = v3;
            }

            data.flag = EDirtyFlag.ENone;
            build_trans_data[index] = data;
        }
    }
    //--------------------------------------------------------
    //! RenderMeshJob
    //--------------------------------------------------------
    internal class RenderMeshJob
    {
        private int m_nMaxMeshQuadCount = 0;
        private int m_nOutputIndex = 0;

        NativeBuffer<Vector3> m_OutPoices;
        NativeBuffer<Vector2> m_OutUV0;
        NativeBuffer<Vector2> m_OutUV1;
        NativeBuffer<Color32> m_OutColors;
        NativeBuffer<int> m_OutIndices;

        Dictionary<int, HUDGroup> m_BuildingGroups = null;

        private const int step_capacity = 200;
        NativeBuffer<TransformIndex> m_TransformIndices;
        NativeBuffer<CollectionMeshInfoOffset> m_indicesOffset;
        private BatchData m_BatchData;

        JobHandle m_JobHandle;
        bool m_IsJobVaild = false;
        //--------------------------------------------------------
        public void Init(BatchData data)
        {
            m_BatchData = data;
            m_TransformIndices = new NativeBuffer<TransformIndex>(step_capacity, Allocator.Persistent);
            m_indicesOffset = new NativeBuffer<CollectionMeshInfoOffset>(step_capacity / 10, Allocator.Persistent);
            m_OutPoices = new NativeBuffer<Vector3>(step_capacity * 60, Allocator.Persistent);
            m_OutUV0 = new NativeBuffer<Vector2>(step_capacity * 60, Allocator.Persistent);
            m_OutUV1 = new NativeBuffer<Vector2>(step_capacity * 60, Allocator.Persistent);
            m_OutColors = new NativeBuffer<Color32>(step_capacity * 60, Allocator.Persistent);
            m_OutIndices = new NativeBuffer<int>(step_capacity * 60, Allocator.Persistent);
        }
        //--------------------------------------------------------
        public void Dispose()
        {
            CompleteJob();
            m_TransformIndices.Dispose();
            m_indicesOffset.Dispose();
            m_OutPoices.Dispose();
            m_OutUV0.Dispose();
            m_OutUV1.Dispose();
            m_OutColors.Dispose();
            m_OutIndices.Dispose();
        }
        //--------------------------------------------------------
        private void CompleteJob()
        {
            if (m_IsJobVaild)
            {
                m_IsJobVaild = false;
                m_JobHandle.Complete();
            }
        }
        //--------------------------------------------------------
        public void FinishUpdateMesh()
        {
            if (!m_IsJobVaild)
                return;
            CompleteJob();

            if (m_BuildingGroups == null)
                return;

            var a_out_poices = m_OutPoices.AsArray();
            var a_out_uv0 = m_OutUV0.AsArray();
            var a_out_uv1 = m_OutUV1.AsArray();
            var a_out_colors = m_OutColors.AsArray();
            var a_out_indices = m_OutIndices.AsArray();

            int count = m_indicesOffset.Length;
            for (int i = 0; i < count; ++i)
            {
                var info = m_indicesOffset[i];
                HUDGroup group = null;
                if (!m_BuildingGroups.TryGetValue(info.group, out group))
                {
                    continue;
                }

                var mesh = group.GetMesh();
                if (mesh == null)
                {
                    // 说明group 被销毁或隐藏了
                    continue;
                }

                int out_quad_count = info.length;
                var mesh_info = group.GetRenderAble();
                // 检查下大小
                if (out_quad_count > mesh_info.quad_count)
                {
                    mesh_info.Resize(out_quad_count);
                }

                int v_count = out_quad_count * 4;
                int i_count = out_quad_count * 6;
                mesh.Clear();
                mesh.SetVertices(a_out_poices, info.v_index, v_count);
                mesh.SetUVs(0, a_out_uv0, info.v_index, v_count);
                mesh.SetUVs(1, a_out_uv1, info.v_index, v_count);
                mesh.SetColors(a_out_colors, info.v_index, v_count);
                mesh.SetIndices(a_out_indices, info.i_index, i_count, MeshTopology.Triangles, 0);
            }
        }
        //--------------------------------------------------------
        public void BeginCollectionMeshInfo()
        {
            m_TransformIndices.Clear();
            m_indicesOffset.Clear();
            m_BuildingGroups.Clear();
            m_nMaxMeshQuadCount = 0;
            m_nOutputIndex = 0;
        }
        //--------------------------------------------------------
        public void CollectionMeshfInfo(HUDGroup group)
        {
            if (group.GetGraphics() == null)
                return;

            CheckCapacity(group.GetGraphics().Count);
            CollectionMeshInfoOffset offset = new CollectionMeshInfoOffset() { group = group.GetID() };
            int count = 0;
            int mesh_quad_count = 0;
            offset.offset = m_TransformIndices.Length;
            foreach (var itm in group.GetGraphics())
            {
                if (itm.BuildIndex < 0 || itm.Batch == null)
                    continue;

                var bufferInfo = itm.Batch.GetBufferInfo();
                if (bufferInfo.transform_job_datas[itm.BuildIndex].is_active == 0)
                    continue;

                m_TransformIndices.Add(new TransformIndex()
                {
                    index = itm.BuildIndex,
                    quad_count = (int)itm.GetQuadCount(),
                    buffer_offset = bufferInfo.info.offset,
                    quad_buffer_offset = bufferInfo.info.quad_offset
                });
                count++;
                mesh_quad_count += itm.GetQuadCount();
            }

            offset.length = count;
            //if(offset.length > 0)
            {
                m_indicesOffset.Add(offset);
                offset.out_index = m_nOutputIndex;
                m_nOutputIndex++;
                m_BuildingGroups.Add(group.GetID(), group);
            }
            if (mesh_quad_count > m_nMaxMeshQuadCount)
            {
                m_nMaxMeshQuadCount = mesh_quad_count;
            }
        }
        //--------------------------------------------------------
        private void CheckCapacity(int add_count)
        {
            if (m_TransformIndices.Capacity - m_TransformIndices.Length < add_count)
            {
                m_TransformIndices.Capacity += step_capacity;
            }

            if (m_indicesOffset.Capacity == m_indicesOffset.Length)
            {
                m_indicesOffset.Capacity += step_capacity / 10;
            }
        }
        //--------------------------------------------------------
        public void EndCollectionMeshInfo(JobHandle batchs_handle)
        {
            int out_buffer_length = m_nMaxMeshQuadCount;
            int need_add = out_buffer_length - m_OutPoices.Length;
            if (need_add > 0)
            {
                int v_add = need_add * 4;
                m_OutPoices.AddLength(v_add);
                m_OutUV0.AddLength(v_add);
                m_OutUV1.AddLength(v_add);
                m_OutColors.AddLength(v_add);
                m_OutIndices.AddLength(need_add * 6);
            }

            var job = new BuildMeshInfoJob()
            {
                vertices = m_BatchData.vertex_datas,
                transform_datas = m_BatchData.build_transform_job_datas,
                quad_datas = m_BatchData.build_quad_datas,

                transform_indices = m_TransformIndices,
                offset_info = m_indicesOffset,
                mesh_quad_count = m_nMaxMeshQuadCount,

                out_poices = m_OutPoices,
                out_uv0 = m_OutUV0,
                out_uv1 = m_OutUV1,
                out_colors = m_OutColors,
                out_indices = m_OutIndices
            };

            //batchs_handle.Complete();
            m_JobHandle = job.Schedule(m_indicesOffset.Length, 1, batchs_handle);
            m_IsJobVaild = true;
            JobHandle.ScheduleBatchedJobs();
        }
    }
}

using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;
namespace Framework.HUD.Runtime
{

    public class Float4Value
    {
        public int idx = -1;
        public RenderDataState<float4> value;
    }

    public class Float4x4Value
    {
        public int idx = -1;
        public RenderDataState<float4x4> value;
    }

    public class TransformIdValue
    {
        public int idx = -1;
        public RenderDataState<int> value;
    }

    public class HudDataSnippet
    {
        private TransformIdValue m_TransValue = new TransformIdValue();
        private Float4x4Value m_Param1 = new Float4x4Value();
        private Float4x4Value m_Param2 = new Float4x4Value();

        private AComponent m_pComponet;
        public HudDataSnippet(AComponent _componet)
        {
            m_pComponet = _componet;
        }

        public HudRenderBatch rendererBatch
        {
            get
            {
                if (m_pComponet != null) return m_pComponet.GetRenderBatch();
                return null;
            }
        }

        public void Init(bool show, int transId)
        {
            m_Show = show;
            m_Enable = true;
            m_Param1.value = new RenderDataState<float4x4>(new float4x4(), CanShow());
            m_Param2.value = new RenderDataState<float4x4>(new float4x4(), CanShow());
            m_TransValue.value = new RenderDataState<int>(transId, CanShow());
        }

        private int rootId { get { return m_pComponet.GetRootId(); } }
        private float tagZ { get { return m_pComponet.GetTagZ(); } }

        private bool isText { get { return m_pComponet.GetHudType() == EHudType.Text; } }
        private bool m_Show = true;

        public void SetShow(bool show)
        {
            if (show == m_Show) return;
            m_Show = show;
            UpdateShowState();
        }

        private bool m_Enable = true;

        public void SetEnable(bool enable)
        {
            if (enable == m_Enable) return;
            m_Enable = enable;
            UpdateShowState();
        }

        private byte CanShow() { return (m_Show && m_Enable) ? (byte)1 : (byte)0; }

        private void UpdateShowState()
        {
            var f4x4dataState = m_Param1.value;
            f4x4dataState.show = CanShow();
            m_Param1.value = f4x4dataState;

            f4x4dataState = m_Param2.value;
            f4x4dataState.show = CanShow();
            m_Param2.value = f4x4dataState;

            var intdataState = m_TransValue.value;
            intdataState.show = CanShow();
            m_TransValue.value = intdataState;
            WriteData();
        }

        public void WriteData()
        {
            WriteParam1Data();
            WriteParam2Data();
            WriteTransformData();
        }

        public void WriteParamData()
        {
            WriteParam1Data();
            WriteParam2Data();
        }

        private void WriteParam1Data()
        {
            if (m_Param1.idx == -1) return;
            rendererBatch.SetFloat4x4("_Param1", m_Param1.idx, m_Param1.value);
        }

        private void WriteParam2Data()
        {
            if (m_Param2.idx == -1) return;
            rendererBatch.SetFloat4x4("_Param2", m_Param2.idx, m_Param2.value);
        }

        private void WriteTransformData()
        {
            if (m_TransValue.idx == -1) return;
            rendererBatch.SetTransformId(m_TransValue.idx, m_TransValue.value);
        }

        public void SetColor(Color32 color)
        {
            float4x4 f4x4 = m_Param2.value.data;
            float2 color2 = HUDUtils.ColorToFloat(color);
            float4 c3 = f4x4.c3;
            c3.zw = color2;
            f4x4.c3 = c3;
            m_Param2.value.data = f4x4;
        }

        public void SetAngle(float angle)
        {
            float4x4 f4x4 = m_Param2.value.data;
            float4 c3 = f4x4.c3;
            c3.x = HUDUtils.ToOneFloat(angle * Mathf.Deg2Rad, (float)m_pComponet.GetMaskType());
            f4x4.c3 = c3;
            m_Param2.value.data = f4x4;
        }

        public void SetMaskType(int maskType)
        {
            float4x4 f4x4 = m_Param2.value.data;
            float4 c3 = f4x4.c3;
            c3.x = HUDUtils.ToOneFloat(m_pComponet.GetAngle() * Mathf.Deg2Rad, (float)maskType);
            f4x4.c3 = c3;
            m_Param2.value.data = f4x4;
        }

        public void SetPosition(Vector3 position)
        {
            float4x4 f4x4 = m_Param1.value.data;
            float4 c3 = f4x4.c3;
            c3.z = position.x/100.0f;
            c3.w = position.y/100.0f;
            f4x4.c3 = c3;
            m_Param1.value.data = f4x4;
        }

        public void SetTextOrImage(bool text)
        {
            float4x4 f4x4 = m_Param2.value.data;
            float4 c3 = f4x4.c3;
            c3.y = HUDUtils.ToOneFloat(text ? 1 : 0, tagZ);// text ? 1 : 0;
            f4x4.c3 = c3;
            m_Param2.value.data = f4x4;
        }

        public void SetTagZ(float tagZ)
        {
            float4x4 f4x4 = m_Param2.value.data;
            float4 c3 = f4x4.c3;
            c3.y = HUDUtils.ToOneFloat(isText?1:0, tagZ);// text ? 1 : 0;
            f4x4.c3 = c3;
            m_Param2.value.data = f4x4;
        }

        public void ResetNineParam()
        {
            float4x4 f4x4 = m_Param2.value.data;
            ClearNineParam(ref f4x4);
            m_Param2.value.data = f4x4;
            f4x4 = m_Param1.value.data;
            ClearNineParam(ref f4x4);
            m_Param1.value.data = f4x4;
        }

        private void ClearNineParam(ref float4x4 parma)
        {
            parma.c0 = new float4();
            parma.c1 = new float4();
            float4 c2 = parma.c2;
            c2.x = 0;
            parma.c2 = c2;
        }

        private unsafe void SetValue(int paramIndex, float value)
        {
            int index = paramIndex % 16;
            if (paramIndex >= 16)
            {
                fixed (float4x4* array = &m_Param2.value.data)
                {
                    ((float*)array)[index] = value;
                }
            }
            else
            {
                fixed (float4x4* array = &m_Param1.value.data)
                {
                    ((float*)array)[index] = value;
                }
            }
        }

        private unsafe float GetValue(int paramIndex)
        {
            int index = paramIndex % 16;
            if (paramIndex >= 16)
            {
                fixed (float4x4* array = &m_Param2.value.data)
                {
                    return ((float*)array)[index];
                }
            }
            else
            {
                fixed (float4x4* array = &m_Param1.value.data)
                {
                    return ((float*)array)[index];
                }
            }
        }

        public void SetSpriteId(int index, int spriteId)
        {
            int valueindex = index / 2;
            int parity = index % 2;
            int floatIndex = 9 + valueindex;
            float fv = GetValue(floatIndex);
            float2 curfv = HUDUtils.ToTowFloat(fv);
            curfv[parity] = spriteId + 1;
            SetValue(floatIndex, HUDUtils.ToOneFloat(curfv.x, curfv.y));
        }

        public void SetSpritePositon(int index, float2 position)
        {
            float fv = HUDUtils.ToOneFloat(position.x, position.y);
            int paramIndex = index;
            SetValue(paramIndex, fv);
        }

        public float2 GetSpritePosition(int index)
        {
            float fv = GetValue(index);
            return HUDUtils.ToTowFloat(fv);
        }

        public void SetSpriteSize(int index, float2 size)
        {
            float fv = HUDUtils.ToOneFloat(size.x, size.y);
            int paramIndex = 16 + index;
            SetValue(paramIndex, fv);
        }

        public float2 GetSpriteScale(int index)
        {
            int paramIndex = 16 + index;
            float fv = GetValue(paramIndex);
            return HUDUtils.ToTowFloat(fv);
        }

        public void SetAlignment(float len)
        {
            float4x4 f4x4 = m_Param2.value.data;
            float4 c3 = f4x4.c3;
            c3.y = len;
            f4x4.c3 = c3;
            m_Param2.value.data = f4x4;
        }

        public void SetAmount(float amount, float origin, float method)
        {
            float4x4 f4x4 = m_Param2.value.data;
            float4 c2 = f4x4.c2;
            //   c2.w = amount;
            // c2.z = origin;
            //  c2.y = method;
            int tempVal = ((int)origin) << 8 | ((int)method);
            c2.y = HUDUtils.ToOneFloat(amount, tempVal);
            f4x4.c2 = c2;
            m_Param2.value.data = f4x4;
        }

        public void SetMaskRegion(Rect region)
        {
            float4x4 f4x4 = m_Param2.value.data;
            float4 c2 = f4x4.c2;
            if(m_pComponet.GetMaskType() == EMaskType.Circle)
            {
                c2.z = HUDUtils.ToOneFloat(region.position.x/100.0f, region.position.y / 100.0f);
                c2.w = HUDUtils.ToOneFloat(region.size.x / 100.0f, region.size.y / 100.0f);
            }
            else
            {
                c2.z = HUDUtils.ToOneFloat(region.xMin / 100.0f, region.yMin / 100.0f);
                c2.w = HUDUtils.ToOneFloat(region.xMax / 100.0f, region.yMax / 100.0f);
            }
            f4x4.c2 = c2;
            m_Param2.value.data = f4x4;
        }

        public void SetTmpParam(float padding, float scale)
        {
            float4x4 f4x4 = m_Param2.value.data;
            float4 c2 = f4x4.c2;
         //   c2.w = padding;
       //     c2.z = scale;
       //     c2.y = 1;
            c2.y = HUDUtils.ToOneFloat(padding, scale);
            f4x4.c2 = c2;
            m_Param2.value.data = f4x4;
        }

        public void OnReorder(HudRenderBatch rendererBatch)
        {
            int idx = -1;
            m_TransValue.idx = rendererBatch.AddTransformId(rootId, m_TransValue.value);
            if (idx == -1) idx = m_TransValue.idx;

            m_Param1.idx = rendererBatch.AddFloat4x4("_Param1", rootId, m_Param1.value);
            if (m_Param1.idx != idx)
            {
                Debug.LogError("data mismatch error!!!");
            }

            m_Param2.idx = rendererBatch.AddFloat4x4("_Param2", rootId, m_Param2.value);
            if (m_Param2.idx != idx)
            {
                Debug.LogError("data mismatch error!!!");
            }
        }
    }
}
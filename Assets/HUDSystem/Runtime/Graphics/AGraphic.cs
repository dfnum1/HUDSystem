/********************************************************************
生成日期:	11:11:2025
类    名: 	AGraphic
作    者:	HappLI
描    述:	HUD 图形基类
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
namespace Framework.HUD.Runtime
{
    public enum EGraphicType
    {
        Node,
        Text = 1,
        Sprite = 2,
        Count,
    }
    //--------------------------------------------------------
    public enum EDirtyFlag
    {
        ENone = 0,
        ETransform = 1 << 0,
        EQuad = 1 << 1,
    }
    //--------------------------------------------------------
    public enum EOperationType
    {
        None = 0,
        TransformChange = 1 << 0,
        VertexProperty = 1 << 1,
        Add = 1 << 2,
        Remove = 1 << 3,
        Active = 1 << 4,
        DeActive = 1 << 5
    }
    //--------------------------------------------------------
    //! AGraphic
    //--------------------------------------------------------
    [System.Serializable]
    public abstract class AGraphic
    {
        public string       name;
        public int          id;
        public float3       localPosition =float3.zero;
        public float3       localScale = HUDUtils.ONE3;
        public float        spacing = 0;
        public float        gscale = 0;

        public int          quadCount = 1;
        public float4[]     quadUV0;
        public float2[]     quadSizes;
        public float4[]     quadParams;

        public float4       extend;

        public Color32      color = Color.white;

        //--------------------------------------------------------
        [System.NonSerialized]
        private List<AGraphic> m_vChilds = null;
        public List<AGraphic> GetChilds()
        {
            return m_vChilds;
        }
        //--------------------------------------------------------
        public void Attack(AGraphic graphic)
        {
            if (graphic == null)
                return;
            if (m_vChilds == null)
                m_vChilds = new List<AGraphic>(2);
            if (m_vChilds.Contains(graphic))
                return;
            m_vChilds.Add(graphic);
        }
        //--------------------------------------------------------
        public void DeAttack(AGraphic graphic)
        {
            if (graphic == null || m_vChilds == null) return;
            m_vChilds.Remove(graphic);
        }
        //--------------------------------------------------------
        [System.NonSerialized]
        EOperationType m_OperationType = EOperationType.None;
        //--------------------------------------------------------
        [System.NonSerialized]
        private int m_nBuildIndex = -1;
        public int BuildIndex
        {
            get { return m_nBuildIndex; }
            set { m_nBuildIndex = value; }
        }
        //--------------------------------------------------------
        [System.NonSerialized]
        private GraphicBatch m_Batch = null;
        internal GraphicBatch Batch
        {
            get { return m_Batch; }
            set { m_Batch = value; }
        }
        //--------------------------------------------------------
        [System.NonSerialized]
        private HUDGroup m_Group = null;
        public HUDGroup Group
        {
            get { return m_Group; }
            set { m_Group = value; }
        }
        //--------------------------------------------------------
        public AGraphic()
        {
            quadCount = GetQuadCount();
            quadUV0 = new float4[quadCount];
            quadParams = new float4[quadCount];
            quadSizes = new float2[quadCount];
            for (int i = 0; i < quadCount; ++i)
            {
                quadSizes[i] = HUDUtils.ONE2;
            }
        }
        //--------------------------------------------------------
        public bool IsActive()
        {
            return m_nBuildIndex >= 0;
        }
        //--------------------------------------------------------
        public abstract int GetQuadCount();
        public abstract EGraphicType GetGraphicType();
    }
}

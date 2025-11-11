/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDGroup
作    者:	HappLI
描    述:	HUD 图组
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Framework.HUD.Runtime
{
    public class HUDGroup
    {
        int                     m_nID = -1;
        HUDSystem               m_pHUDSystem = null;
        private Mesh            m_pMesh = null;
        private MeshInfo        m_pRenderAble = null;
        private int             m_nMaxQuadCount = 0;
        private List<AGraphic>  m_vGraphics = null;
        //--------------------------------------------------------
        public HUDGroup(HUDSystem hudSystem)
        {
            m_pHUDSystem = hudSystem;
            m_nID = HUDUtils.GetUniqueID();
        }
        //--------------------------------------------------------
        public int GetID()
        {
            return m_nID;
        }
        //--------------------------------------------------------
        public List<AGraphic> GetGraphics()
        {
            return m_vGraphics;
        }
        //--------------------------------------------------------
        public Mesh GetMesh()
        {
            return m_pMesh;
        }
        //--------------------------------------------------------
        internal void SetMesh(Mesh mesh)
        {
            m_pMesh = mesh;
        }
        //--------------------------------------------------------
        public MeshInfo GetRenderAble()
        {
            if (m_pRenderAble == null)
            {
                m_pRenderAble = new MeshInfo(m_nMaxQuadCount);
            }
            else if (m_nMaxQuadCount > m_pRenderAble.quad_count)
            {
                m_pRenderAble.Resize(m_nMaxQuadCount);
            }
            return m_pRenderAble;
        }
        //--------------------------------------------------------
        public void ForceRebuild()
        {
            if (m_vGraphics == null)
                return;
            foreach(var db in m_vGraphics)
            {
                m_pHUDSystem.RebuildGraphic(db, HUDUtils.ALLDirty);
            }
        }
        //--------------------------------------------------------
        public void Add(LinkedList<AGraphic> graphics)
        {
            if (m_vGraphics == null)
                m_vGraphics = new List<AGraphic>(32);
            foreach (var g in graphics)
            {
                g.Group = this;
                m_vGraphics.Add(g);
                m_pHUDSystem.OnAddGraphic(g);
                m_nMaxQuadCount += (int)g.GetQuadCount();
            }
        }
        //--------------------------------------------------------
        public void Remove(LinkedList<AGraphic> graphics)
        {
            if (m_vGraphics == null)
                return;

            foreach (var g in graphics)
            {
                m_vGraphics.Remove(g);
                m_nMaxQuadCount -= (int)g.GetQuadCount();
                m_pHUDSystem.OnRemoveGraphic(g);
                g.Group = null;
            }
        }
    }
}

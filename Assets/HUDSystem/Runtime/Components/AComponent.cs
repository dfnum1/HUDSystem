/********************************************************************
生成日期:	11:11:2025
类    名: 	AComponent
作    者:	HappLI
描    述:	HUD 组件
*********************************************************************/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace Framework.HUD.Runtime
{
    public enum EHudType : byte
    {
        None =0,
        Canvas,
        Text,
        Image,
        Number
    }
    //--------------------------------------------------------
    public abstract class AComponent
    {
        protected EHudType m_eHudType = EHudType.None;

        protected int m_nId = 0;
        protected string m_strName = null;

        protected bool m_bVisible = true;

        protected List<AComponent> m_vChilds = null;
        protected HudSystem m_pHudSystem;

        protected HudController m_HudController;
        protected HudBaseData m_pHudData = null;

        AComponent m_pParent = null;
        protected List<HudDataSnippet> m_vDataSnippets;
        //--------------------------------------------------------
        public AComponent(HudSystem pSystem, HudBaseData hudData)
        {
            m_pHudSystem = pSystem;
            m_pHudData = hudData;
        }
        //--------------------------------------------------------
        internal void SetHudController(HudController hudObject)
        {
            m_HudController = hudObject;
        }
        //--------------------------------------------------------
        internal int GetRootId()
        {
            return 0;
        }
        //--------------------------------------------------------
        internal void SetId(int nId)
        {
            m_nId = nId;
        }
        //--------------------------------------------------------
        public int GetId()
        {
            return m_nId;
        }
        //--------------------------------------------------------
        internal void SetName(string name)
        {
            m_strName = name;
        }
        //--------------------------------------------------------
        public string GetName()
        {
            return m_strName;
        }
        //--------------------------------------------------------
        public EHudType GetHudType()
        {
            return m_eHudType;
        }
        //--------------------------------------------------------
        public int GetTransId()
        {
            if (m_HudController == null) return -1;
            return m_HudController.GetTransId();
        }
        //--------------------------------------------------------
        public HudAtlas GetHudAtlas()
        {
            if (m_HudController == null) return null;
            return m_HudController.GetAtlas();
        }
        //--------------------------------------------------------
        public HudController GetController()
        {
            return m_HudController;
        }
        //--------------------------------------------------------
        public HudRenderBatch GetRenderBatch()
        {
            if (m_HudController == null) return null;
            return m_HudController.renderBatch;
        }
        //--------------------------------------------------------
        public bool IsVisible()
        {
            if(m_pParent!=null)
                return m_bVisible && m_pParent.IsVisible();
            return m_bVisible;
        }
        //--------------------------------------------------------
        public void SetVisible(bool bVisible)
        {
            if (m_bVisible == bVisible) return;
            m_bVisible = bVisible;
            if (m_vDataSnippets != null)
            {
                for (int i = 0; i < m_vDataSnippets.Count; i++)
                {
                    m_vDataSnippets[i].SetShow(bVisible);
                }
            }
            if (m_vChilds != null)
            {
                for (int i = 0; i < m_vChilds.Count; i++)
                {
                    m_vChilds[i].SetVisible(bVisible);
                }
            }
        }
        //--------------------------------------------------------
        public List<AComponent> GetChilds()
        {
            return m_vChilds;
        }
        //--------------------------------------------------------
        public void Attach(AComponent pComp)
        {
            if (pComp == null) return;
            if (m_vChilds == null)
                m_vChilds = new List<AComponent>(2);
            if (m_vChilds.Contains(pComp))
                return;
            pComp.m_pParent = this;
            m_vChilds.Add(pComp);
        }
        //--------------------------------------------------------
        public void Detach(AComponent pComp)
        {
            if (pComp == null) return;
            if (m_vChilds == null)
                return;
            pComp.m_pParent = null;
            m_vChilds.Remove(pComp);
        }
        //--------------------------------------------------------
        public void DetachAll()
        {
            if (m_vChilds == null)
                return;
            foreach (var db in m_vChilds)
                db.m_pParent = null;
            m_vChilds.Clear();
        }
        //--------------------------------------------------------
        public void ResizeDataSnippet(int count)
        {
            if (m_vDataSnippets == null) { m_vDataSnippets = new List<HudDataSnippet>(); }
            if (m_vDataSnippets.Count == count) return;
            if (m_vDataSnippets.Count < count)
            {
                int deltaCount = count - m_vDataSnippets.Count;
                for (int i = 0; i < deltaCount; i++)
                {
                    HudDataSnippet snippet = new HudDataSnippet(this);
                    snippet.Init(IsVisible(), GetTransId());
                    snippet.SetColor(m_pHudData.color);
                    snippet.SetPosition(m_pHudData.position / 100f);
                    snippet.SetAngle(m_pHudData.angle);
                    snippet.WriteData();
                    m_vDataSnippets.Add(snippet);
                }
            }
            else
            {
                int deltaCount = m_vDataSnippets.Count - count;
                for (int i = 0; i < deltaCount; i++)
                {
                    m_vDataSnippets.RemoveAt(m_vDataSnippets.Count - 1);
                }
            }
        }
        //--------------------------------------------------------
        public HudDataSnippet GetDataSnippet(int index)
        {
            if (m_vDataSnippets == null) return null;
            if (index >= m_vDataSnippets.Count) return null;
            return m_vDataSnippets[index];
        }
        //--------------------------------------------------------
        internal void OnReorder()
        {
            if (m_HudController == null || m_vDataSnippets == null)
                return;

            if (!IsVisible())
                return;

            var renderBatch = m_HudController.renderBatch;
            if (renderBatch == null)
                return;

            for (int i = m_vDataSnippets.Count-1; i >=0; --i)
            {
                m_vDataSnippets[i].OnReorder(renderBatch);
            }

            if(m_vChilds!=null)
            {
                for(int i = m_vChilds.Count-1; i>=0; --i)
                {
                    m_vChilds[i].OnReorder();
                }
            }
        }
    }
}

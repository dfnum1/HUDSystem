/********************************************************************
生成日期:	11:11:2025
类    名: 	AComponent
作    者:	HappLI
描    述:	HUD 组件
*********************************************************************/
using System.Collections.Generic;
using UnityEngine;
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
    public abstract class AComponent
    {
        protected EHudType m_eHudType = EHudType.None;

        protected int m_nId = 0;
        protected string m_strName = null;

        protected List<AComponent> m_vChilds = null;
        protected HudSystem m_pHudSystem;
        //--------------------------------------------------------
        public AComponent(HudSystem pSystem)
        {
            m_pHudSystem = pSystem;
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
            m_vChilds.Add(pComp);
        }
        //--------------------------------------------------------
        public void Detach(AComponent pComp)
        {
            if (pComp == null) return;
            if (m_vChilds == null)
                return;
            m_vChilds.Remove(pComp);
        }
    }
}

/********************************************************************
生成日期:	11:11:2025
类    名: 	HudImage
作    者:	HappLI
描    述:	图片
*********************************************************************/
using Codice.CM.Common.Update.Partial;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    //--------------------------------------------------------
    [System.Serializable]
    public class HudParticleData : HudBaseData
    {
        public string strParticle;
        public Vector3 scale = Vector3.one;
        public int renderOrder = 0;
        public override AWidget CreateWidget(HudSystem pSystem)
        {
            return TypePool.MallocWidget<HudParticle>(pSystem, this);
        }
    }
    //--------------------------------------------------------
    //! HudParticle
    //--------------------------------------------------------
    [HudData(typeof(HudParticleData))]
    public class HudParticle : AWidget
    {
        enum EOverrideType : byte
        {
            Scale = EParamOverrideType.Count,
            SortingOrder,
        }
        Transform m_pParticleTransform = null;
        GameObject m_pParticle = null;
        string m_strCurrentParticle = null;
        string m_strParticle = null;
        List<ParticleSystemRenderer> m_vParticles = null;
        public HudParticle() : base()
        {
            m_eHudType = EHudType.Particle;
        }
        //--------------------------------------------------------
        protected override void OnInit()
        {
            OnDirty();
        }
        //--------------------------------------------------------
        public int GetSortingOrder()
        {
            if (GetOverrideParam((byte)EOverrideType.SortingOrder, out var temp))
                return temp.intVal0;
            HudParticleData data = m_pHudData as HudParticleData;
            return data.renderOrder;
        }
        //--------------------------------------------------------
        public void SetSortingOrder(int order)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.SortingOrder, order);
            if (bDirty)
            {
                OnDirty();
            }
        }
        //--------------------------------------------------------
        public void SetScale(Vector3 scale)
        {
            bool bDirty = SetOverrideParam((byte)EOverrideType.Scale, scale);
            if (bDirty)
            {
                OnDirty();
            }
        }
        //--------------------------------------------------------
        public Vector3 GetScale()
        {
            if (GetOverrideParam((byte)EOverrideType.Scale, out var temp))
                return new Vector3(temp.floatVal0, temp.floatVal1, temp.floatVal2);
            HudParticleData data = m_pHudData as HudParticleData;
            return data.scale;
        }
        //--------------------------------------------------------
        new public void SetVisibleSelf(bool bVisible)
        {
            if (m_bVisible == bVisible) return;
            m_bVisible = bVisible;
            OnDirty();
        }
        //--------------------------------------------------------
        protected override void OnDirty()
        {
            if (!string.IsNullOrEmpty(m_strParticle))
            {
                if(m_strParticle.CompareTo(m_strCurrentParticle) !=0)
                {
                    DestroyParticle();
                    m_strCurrentParticle = m_strParticle;
                    GetController().SpawnInstance(this, m_strCurrentParticle, (inst) =>
                    {
                        m_pParticle = inst;
                        if(m_pParticle!=null)
                        {
                            m_pParticleTransform = m_pParticle.transform;
                            OnTransformChanged();
                            if (m_vParticles == null) m_vParticles = new List<ParticleSystemRenderer>(2);
                            var temps = m_pParticle.GetComponents<ParticleSystemRenderer>();
                            if (temps != null) m_vParticles.AddRange(temps);
                            temps = m_pParticle.GetComponentsInChildren<ParticleSystemRenderer>();
                            if (temps != null) m_vParticles.AddRange(temps);

                            foreach(var db in m_vParticles)
                            {
                                db.sortingOrder = GetSortingOrder();
                            }
                        }
                    });
                }
            }
            OnTransformChanged();
            if (m_vParticles!=null)
            {
                foreach (var db in m_vParticles)
                {
                    db.sortingOrder = GetSortingOrder();
                }
            }
        }
        //--------------------------------------------------------
        protected override void OnTransformChanged()
        {
            if (m_pParticleTransform)
            {
                Transform pFollow = GetController().GetFollowTarget();
                if (pFollow)
                {
                    if(m_pParticleTransform.parent != pFollow)
                        m_pParticleTransform.SetParent(pFollow);
                    m_pParticleTransform.position = pFollow.position + GetPosition();
                    m_pParticleTransform.eulerAngles = new Vector3(0, 0, GetAngle());
                    m_pParticleTransform.localScale = IsVisible() ? GetScale() : Vector3.zero;
                }
                else
                {
                    m_pParticleTransform.SetParent(null);
                    m_pParticleTransform.position = GetController().GetWorldMatrix() * GetPosition();
                    m_pParticleTransform.eulerAngles = new Vector3(0, 0, GetAngle());
                    m_pParticleTransform.localScale = IsVisible() ? GetScale() : Vector3.zero;
                }
            }
        }
        //--------------------------------------------------------
        public new void SetPosition(Vector3 vPos)
        {
            if (m_pParticle)
                m_pParticle.transform.position = GetController().GetWorldMatrix() * GetPosition();
        }
        //--------------------------------------------------------
        protected override void OnSyncData()
        {
            HudParticleData data = m_pHudData as HudParticleData;
            m_strParticle = data.strParticle;
        }
        //--------------------------------------------------------
        void DestroyParticle()
        {
            if (m_pParticle != null)
            {
                GetController().DestroyInstance(this, m_pParticle);
                m_pParticle = null;
            }
            m_strCurrentParticle = null;
            if(m_vParticles!=null) m_vParticles.Clear();
        }
        //--------------------------------------------------------
        protected override void OnDestroy()
        {
            DestroyParticle();
            m_strParticle = null;
        }
    }
}

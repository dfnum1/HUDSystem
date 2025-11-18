/********************************************************************
生成日期:	11:11:2025
类    名: 	AComponent
作    者:	HappLI
描    述:	HUD 组件
*********************************************************************/
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
    enum EParamOverrideType : byte
    {
        Name = 0,
        Position,
        Size,
        Angle,
        Color,
        MaskType,
        MaskRegion,
        Sprite,
    }
    //------------------------------------------------------
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct Variable
    {
        [FieldOffset(0)]
        public int intVal0;
        [FieldOffset(0)]
        public float floatVal0;

        [FieldOffset(4)]
        public int intVal1;
        [FieldOffset(4)]
        public float floatVal1;

        [FieldOffset(8)]
        public int intVal2;
        [FieldOffset(8)]
        public float floatVal2;

        [FieldOffset(12)]
        public int intVal3;
        [FieldOffset(12)]
        public float floatVal3;

        [FieldOffset(0)]
        public long longValue0;

        [FieldOffset(8)]
        public long longValue1;

        public Vector3 ToVector3()
        {
            return new Vector3(floatVal0, floatVal1, floatVal2);
        }
        public Vector4 ToVector4()
        {
            return new Vector4(floatVal0, floatVal1, floatVal2, floatVal3);
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(floatVal0, floatVal1, floatVal2, floatVal3);
        }
        public Color ToColor()
        {
            return new Color(floatVal0, floatVal1, floatVal2, floatVal3);
        }

        public Vector2 ToVector2()
        {
            return new Vector2(floatVal0, floatVal1);
        }
        public void Destroy() { }
    }
    //--------------------------------------------------------
    struct ParamOverrideInfo
    {
        public Variable paramValue;
        public string strVal;
        public UnityEngine.Object unityVal;
    }
    //--------------------------------------------------------
    public abstract class AComponent
    {
        protected EHudType m_eHudType = EHudType.None;

        protected bool m_bVisible = true;

        protected List<AComponent> m_vChilds = null;
        protected HudSystem m_pHudSystem;

        protected HudController m_HudController;
        protected HudBaseData m_pHudData = null;

        AComponent m_pParent = null;
        protected List<HudDataSnippet> m_vDataSnippets;

        private Dictionary<EParamOverrideType, ParamOverrideInfo> m_vOverrideParams;
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
        internal virtual void Init()
        {
            OnInit();
            if(m_vChilds!=null)
            {
                for (int i = 0; i < m_vChilds.Count; ++i)
                {
                    m_vChilds[i].Init();
                }
            }
        }
        //--------------------------------------------------------
        protected abstract void OnInit();
        //--------------------------------------------------------
        internal int GetRootId()
        {
            return m_HudController.GetTransId();
        }
        //--------------------------------------------------------
        internal float GetTagZ()
        {
            return GetPosition().z;
        }
        //--------------------------------------------------------
        internal HudBaseData GetData()
        {
            return m_pHudData;
        }
        //--------------------------------------------------------
        internal void SetId(int nId)
        {
            m_pHudData.id = nId;
        }
        //--------------------------------------------------------
        public int GetId()
        {
            return m_pHudData.id;
        }
        //--------------------------------------------------------
        internal void SetName(string name)
        {
            m_pHudData.name = name;
        }
        //--------------------------------------------------------
        public string GetName()
        {
            return m_pHudData.name;
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
        public AComponent GetParent()
        {
            return m_pParent;
        }
        //--------------------------------------------------------
        public Vector3 GetPosition()
        {
            if (m_pParent != null) return m_pParent.GetPosition() + m_pHudData.position;
            return m_pHudData.position;
        }
        //--------------------------------------------------------
        public float GetAngle()
        {
            if (m_pParent != null) return m_pParent.GetAngle() + m_pHudData.angle;
            return m_pHudData.angle;
        }
        //--------------------------------------------------------
        public EMaskType GetMaskType()
        {
            if (m_pHudData.mask != EMaskType.None) return m_pHudData.mask;
            if (m_pParent != null) return m_pParent.GetMaskType();
            return EMaskType.None;
        }
        //--------------------------------------------------------
        public bool IsValidMask()
        {
            if (m_pHudData.mask == EMaskType.Rect && m_pHudData.maskRegion.width > 0 && m_pHudData.maskRegion.height > 0)
                return true;
            if (m_pHudData.mask == EMaskType.Circle && m_pHudData.maskRegion.width > 0)
                return true;
            return false;
        }
        //--------------------------------------------------------
        public Rect GetMaskRegion()
        {
            if (IsValidMask())
            {
                Rect region = m_pHudData.maskRegion;
                if(m_pHudData.mask == EMaskType.Circle)
                {
                    Vector3 worldPos =GetPosition() + new Vector3(region.position.x, region.position.y,0);
                    region.position = new Vector2(worldPos.x, worldPos.y);
                    return region;
                }
                else
                {
                    Vector3 worldPos = m_HudController.GetWorldMatrix()*GetPosition();
                    region.position += new Vector2(worldPos.x, worldPos.y) - region.size/2;
                    if (m_pParent != null)
                    {
                        Rect parentRegion = m_pParent.GetMaskRegion();
                        if (parentRegion.width > 0 && parentRegion.height > 0)
                        {
                            float xMin = Mathf.Max(region.xMin, parentRegion.xMin);
                            float yMin = Mathf.Max(region.yMin, parentRegion.yMin);
                            float xMax = Mathf.Min(region.xMax, parentRegion.xMax);
                            float yMax = Mathf.Min(region.yMax, parentRegion.yMax);
                            if (xMax > xMin && yMax > yMin)
                                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
                            else
                                return Rect.zero;
                        }
                        else
                            return region;
                    }
                }

                return region;
            }
            else if (m_pParent != null)
                return m_pParent.GetMaskRegion();

            return Rect.zero;
        }
        //--------------------------------------------------------
        public void SetDirty()
        {
            OnDirty();
            if (m_vDataSnippets != null)
            {
                for (int i = 0; i < m_vDataSnippets.Count; i++)
                {
                    m_vDataSnippets[i].SetColor(m_pHudData.color);
                    m_vDataSnippets[i].SetPosition(GetPosition());
                    m_vDataSnippets[i].SetTextOrImage(GetHudType() == EHudType.Text);
                    m_vDataSnippets[i].SetAngle(GetAngle());
                    m_vDataSnippets[i].SetMaskRegion(GetMaskRegion());
                    m_vDataSnippets[i].WriteData();
                }
            }
            if (m_vChilds!=null)
            {
                for (int i = m_vChilds.Count - 1; i >= 0; --i)
                {
                    m_vChilds[i].SetDirty();
                }
            }
        }
        //--------------------------------------------------------
        protected virtual void OnDirty()
        {
        }
        //--------------------------------------------------------
        public bool IsVisibleSelf()
        {
            return m_bVisible;
        }
        //--------------------------------------------------------
        public void SetVisibleSelf(bool bVisible)
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
            SetVisibleSelf(bVisible);
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
        public void Attach(AComponent pComp, int insertIndex =-1)
        {
            if (pComp == null) return;
            if (m_vChilds == null)
                m_vChilds = new List<AComponent>(2);

            if (m_vChilds.Contains(pComp))
            {
                int curIndex = m_vChilds.IndexOf(pComp);
                if (insertIndex>=0 && curIndex != insertIndex)
                {
                    if (insertIndex < curIndex) curIndex++;
                    m_vChilds.Insert(insertIndex, pComp);
                    m_vChilds.RemoveAt(curIndex);
                    m_HudController?.TriggerReorder();
                }
                return;
            }
            pComp.m_pParent = this;
            if(insertIndex>=0 && insertIndex<= m_vChilds.Count)
            {
                m_vChilds.Insert(insertIndex, pComp);
            }
            else
            {
                m_vChilds.Add(pComp);
            }
            m_HudController?.TriggerReorder();
        }
        //--------------------------------------------------------
        public void Detach(AComponent pComp)
        {
            if (pComp == null) return;
            if (m_vChilds == null)
                return;
            pComp.m_pParent = null;
            m_vChilds.Remove(pComp);
            m_HudController?.TriggerReorder();
        }
        //--------------------------------------------------------
        public void DetachAll()
        {
            if (m_vChilds == null)
                return;
            foreach (var db in m_vChilds)
                db.m_pParent = null;
            m_vChilds.Clear();
            m_HudController?.TriggerReorder();
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
                    snippet.SetPosition(GetPosition());
                    snippet.SetTextOrImage(GetHudType() == EHudType.Text);
                    snippet.SetAngle(GetAngle());
                    snippet.SetMaskRegion(GetMaskRegion());

                    snippet.WriteData();
                    m_vDataSnippets.Add(snippet);
                }
                m_HudController.TriggerReorder();
            }
            else
            {
                int deltaCount = m_vDataSnippets.Count - count;
                for (int i = 0; i < deltaCount; i++)
                {
                    m_vDataSnippets.RemoveAt(m_vDataSnippets.Count - 1);
                }
                m_HudController.TriggerReorder();
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
            if (m_HudController == null)
                return;

            if (!IsVisible())
                return;

            var renderBatch = m_HudController.renderBatch;
            if (renderBatch == null)
                return;

            if (m_vDataSnippets!=null)
            {
                for (int i = m_vDataSnippets.Count - 1; i >= 0; --i)
                {
                    m_vDataSnippets[i].OnReorder(renderBatch);
                }
            }


            if(m_vChilds!=null)
            {
                for(int i = m_vChilds.Count-1; i>=0; --i)
                {
                    m_vChilds[i].OnReorder();
                }
            }
        }
        //--------------------------------------------------------
        public void Destroy()
        {
            if (m_pParent != null) m_pParent.Detach(this);
        }
    }
}

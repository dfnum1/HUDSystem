/********************************************************************
生成日期:	11:11:2025
类    名: 	AWidget
作    者:	HappLI
描    述:	HUD 组件
*********************************************************************/
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Plastic.Newtonsoft.Json.Linq;
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
        Number,
        Particle,
    }
    //--------------------------------------------------------
    enum EParamOverrideType : byte
    {
        Position,
        Size,
        Angle,
        Color,
        RayTest,
        Count,
    }
    //--------------------------------------------------------
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct ParamOverrideInfo
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
        public static ParamOverrideInfo DEF = new ParamOverrideInfo()
        {
            longValue0 = 0,
            longValue1 = 0,
        };
    }
    //--------------------------------------------------------
    public abstract class AWidget : TypeObject
    {
        protected EHudType m_eHudType = EHudType.None;

        protected bool m_bVisible = true;
        protected string m_strName = null;

        protected List<AWidget> m_vChilds = null;
        protected HudSystem m_pHudSystem;

        protected HudController m_HudController;
        protected HudBaseData m_pHudData = null;

        AWidget m_pParent = null;
        protected List<HudDataSnippet> m_vDataSnippets;

        private Dictionary<byte, ParamOverrideInfo> m_vOverrideParams = null;
        //--------------------------------------------------------
        public AWidget()
        {
        }
        //--------------------------------------------------------
        internal void SetHudSystem(HudSystem hudSystem)
        {
            m_pHudSystem = hudSystem;
        }
        //--------------------------------------------------------
        internal void SetHudData(HudBaseData hudData)
        {
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
            SyncData();
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
        internal virtual void SyncData()
        {
            m_bVisible = m_pHudData.visible;
            m_strName = m_pHudData.name;
            if (m_vOverrideParams != null) m_vOverrideParams.Clear();
            OnSyncData();
            SetDirty();
        }
        //--------------------------------------------------------
        protected abstract void OnSyncData();
        //--------------------------------------------------------
        internal int GetRootId()
        {
            return m_HudController.GetTransId();
        }
        //--------------------------------------------------------
        protected bool SetOverrideParam(byte type, bool bValue)
        {
            CheckOverrideParam();
            bool bDirty = !m_vOverrideParams.TryGetValue(type, out var temp) || temp.intVal0 != (bValue ? 1 : 0);
            m_vOverrideParams[type] = new ParamOverrideInfo() { intVal0 = bValue?1:0 };
            return bDirty;
        }
        //--------------------------------------------------------
        protected bool SetOverrideParam(byte type, int nValue)
        {
            CheckOverrideParam();
            bool bDirty = !m_vOverrideParams.TryGetValue(type, out var temp) || temp.intVal0 != nValue;
            m_vOverrideParams[type] = new ParamOverrideInfo() { intVal0 = nValue };
            return bDirty;
        }
        //--------------------------------------------------------
        protected bool SetOverrideParam(byte type, float fValue)
        {
            CheckOverrideParam();
            bool bDirty = !m_vOverrideParams.TryGetValue(type, out var temp) || temp.floatVal0 != fValue;
            m_vOverrideParams[type] = new ParamOverrideInfo() { floatVal0 = fValue };
            return bDirty;
        }
        //--------------------------------------------------------
        protected bool SetOverrideParam(byte type, Vector2 vValue)
        {
            CheckOverrideParam();
            bool bDirty = !m_vOverrideParams.TryGetValue(type, out var temp) || temp.floatVal0 != vValue.x || temp.floatVal1 != vValue.y;
            m_vOverrideParams[type] = new ParamOverrideInfo() { floatVal0 = vValue.x, floatVal1 = vValue.y };
            return bDirty;
        }
        //--------------------------------------------------------
        protected bool SetOverrideParam(byte type, Vector3 vValue)
        {
            CheckOverrideParam();
            bool bDirty = !m_vOverrideParams.TryGetValue(type, out var temp) || temp.floatVal0 != vValue.x || temp.floatVal1 != vValue.y || temp.floatVal2 != vValue.z;
            m_vOverrideParams[type] = new ParamOverrideInfo() { floatVal0 = vValue.x, floatVal1 = vValue.y, floatVal2 = vValue.z };
            return bDirty;
        }
        //--------------------------------------------------------
        protected bool SetOverrideParam(byte type, Color color)
        {
            CheckOverrideParam();
            bool bDirty = !m_vOverrideParams.TryGetValue(type, out var temp) || temp.floatVal0 != color.r || temp.floatVal1 != color.g || temp.floatVal2 != color.b || temp.floatVal3 != color.a;
            m_vOverrideParams[type] = new ParamOverrideInfo() { floatVal0 = color.r, floatVal1 = color.g, floatVal2 = color.b, floatVal3 = color.a };
            return bDirty;
        }
        //--------------------------------------------------------
        protected void CheckOverrideParam()
        {
            if (m_vOverrideParams == null)
                m_vOverrideParams = new Dictionary<byte, ParamOverrideInfo>((int)EParamOverrideType.Count);
        }
        //--------------------------------------------------------
        protected bool GetOverrideParam(byte type, out ParamOverrideInfo info)
        {
            info = ParamOverrideInfo.DEF;
            if (m_vOverrideParams == null) return false;
            if (m_vOverrideParams.TryGetValue(type, out info))
                return true;
            return false;
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
            if (m_pHudData.id == nId)
                return;
            int oldId = m_pHudData.id;
            m_pHudData.id = nId;
            if (m_HudController != null)
                m_HudController.OnWidgetIDChange(this, nId, oldId);
        }
        //--------------------------------------------------------
        public int GetId()
        {
            return m_pHudData.id;
        }
        //--------------------------------------------------------
        protected bool IsEditor()
        {
            return m_HudController == null || m_HudController.IsEditorMode();
        }
        //--------------------------------------------------------
        internal void SetName(string name)
        { 
            m_strName = name;
            if (IsEditor())
            {
                m_pHudData.name = name;
            }
        }
        //--------------------------------------------------------
        public string GetName()
        {
            if (m_HudController == null || m_HudController.IsEditorMode())
                m_strName = m_pHudData.name;
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
        public AWidget GetParent()
        {
            return m_pParent;
        }
        //--------------------------------------------------------
        public bool CanRayTest()
        {
            if (GetOverrideParam((byte)EParamOverrideType.RayTest, out ParamOverrideInfo info))
                return info.intVal0 != 0;
            return m_pHudData.rayTest;
        }
        //--------------------------------------------------------
        internal void SetRayTest(bool rayTest)
        {
            SetOverrideParam((byte)EParamOverrideType.RayTest, rayTest);
        }
        //--------------------------------------------------------
        public Vector3 GetPosition()
        {
            Vector3 vPos = m_pHudData.position;
            if (GetOverrideParam((byte)EParamOverrideType.Position, out var info))
            {
                vPos.x = info.floatVal0;
                vPos.y = info.floatVal1;
                vPos.z = info.floatVal2;
            }
            if (m_pParent != null) return m_pParent.GetPosition() + vPos;
            return vPos;
        }
        //--------------------------------------------------------
        internal void SetPosition(Vector3 vPos)
        {
            bool bDirty = SetOverrideParam((byte)EParamOverrideType.Position, vPos);
            if(bDirty && m_vDataSnippets!=null)
            {
                for (int i = 0; i < m_vDataSnippets.Count; i++)
                {
                    m_vDataSnippets[i].SetPosition(GetPosition());
                    m_vDataSnippets[i].WriteParamData();
                }
            }
        }
        //--------------------------------------------------------
        public float GetAngle()
        {
            float fAngle = m_pHudData.angle;
            if (GetOverrideParam((byte)EParamOverrideType.Angle, out var info))
            {
                fAngle = info.floatVal0;
            }
            if (m_pParent != null) return m_pParent.GetAngle() + fAngle;
            return fAngle;
        }
        //--------------------------------------------------------
        internal void SetAngle(float fAngle)
        {
            bool bDirty = SetOverrideParam((byte)EParamOverrideType.Angle, fAngle); 
            if (bDirty && m_vDataSnippets != null)
            {
                for (int i = 0; i < m_vDataSnippets.Count; i++)
                {
                    m_vDataSnippets[i].SetAngle(GetAngle());
                    m_vDataSnippets[i].WriteParamData();
                }
            }
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
                    m_vDataSnippets[i].SetColor(GetColor());
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
        public Vector2 GetSize()
        {
            Vector2 size = m_pHudData.sizeDelta;
            if (GetOverrideParam((byte)EParamOverrideType.Size, out var info))
            {
                size.x = info.floatVal0;
                size.y = info.floatVal1;
            }
            return size;
        }
        //--------------------------------------------------------
        public void SetSize(Vector2 size)
        {
            bool bDirty = SetOverrideParam((byte)EParamOverrideType.Size, size);
            if (bDirty)
            {
                SetDirty();
            }
        }
        //--------------------------------------------------------
        public float GetAlpha()
        {
            if (m_pParent != null) return m_pParent.GetAlpha();
            Color color = m_pHudData.color;
            if (GetOverrideParam((byte)EParamOverrideType.Color, out var info))
            {
                return info.floatVal3;
            }
            return color.a;
        }
        //--------------------------------------------------------
        public Color GetColor()
        {
            Color color = m_pHudData.color;
            if (GetOverrideParam((byte)EParamOverrideType.Color, out var info))
            {
                color.r = info.floatVal0;
                color.g = info.floatVal1;
                color.b = info.floatVal2;
                color.a = info.floatVal3;
            }
            color.a *= GetAlpha();
            return color;
        }
        //--------------------------------------------------------
        public void SetColor(Color color)
        {
            bool bDirty = SetOverrideParam((byte)EParamOverrideType.Color, color);
            if(bDirty && m_vDataSnippets!=null)
            {
                for (int i = 0; i < m_vDataSnippets.Count; i++)
                {
                    m_vDataSnippets[i].SetColor(GetColor());
                    m_vDataSnippets[i].WriteParamData();
                }
            }
        }
        //--------------------------------------------------------
        protected void OnAlpha()
        {
            if (m_vDataSnippets != null)
            {
                for (int i = 0; i < m_vDataSnippets.Count; i++)
                {
                    m_vDataSnippets[i].SetColor(GetColor());
                }
            }
            if(m_vChilds!=null)
            {
                for (int i = 0; i < m_vChilds.Count; i++)
                {
                    m_vChilds[i].OnAlpha();
                }
            }
        }
        //--------------------------------------------------------
        public List<AWidget> GetChilds()
        {
            return m_vChilds;
        }
        //--------------------------------------------------------
        public void Attach(AWidget pComp, int insertIndex =-1)
        {
            if (pComp == null) return;
            if (m_vChilds == null)
                m_vChilds = new List<AWidget>(2);

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
        public void Detach(AWidget pComp)
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
                    snippet.SetColor(GetColor());
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
        internal void OnRebuild()
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
                    m_vChilds[i].OnRebuild();
                }
            }
        }
        //--------------------------------------------------------
        internal void DoTransformChanged()
        {
            if (m_HudController == null)
                return;
            if (!IsVisible())
                return;

            OnTransformChanged();
            if (m_vChilds != null)
            {
                for (int i = m_vChilds.Count - 1; i >= 0; --i)
                {
                    m_vChilds[i].DoTransformChanged();
                }
            }
        }
        //--------------------------------------------------------
        protected virtual void OnTransformChanged()
        {
        }
        //--------------------------------------------------------
        internal override void Destroy()
        {
            OnDestroy();
            if (m_pParent != null) m_pParent.Detach(this);
            if (m_HudController != null) m_HudController.OnWidgetDestroy(this);
            m_bVisible = true;
            m_strName = null;
            if (m_vChilds != null) m_vChilds.Clear();
            m_pHudSystem = null;
            m_HudController = null;
            m_pHudData = null;
            m_pParent = null;
            if (m_vDataSnippets != null) m_vDataSnippets.Clear();
            if (m_vOverrideParams != null) m_vOverrideParams.Clear();

            TypePool.Free(this);
        }
        //--------------------------------------------------------
        protected virtual void OnDestroy() { }
    }
}

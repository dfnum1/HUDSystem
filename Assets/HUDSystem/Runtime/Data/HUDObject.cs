/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDObject
作    者:	HappLI
描    述:	HUD 数据对象层
*********************************************************************/
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    [CreateAssetMenu]
    public class HudObject : ScriptableObject
    {
        public Vector2 center = Vector2.zero;
        public Vector2 size = new Vector2(100,100);

        public Mesh mesh;
        public Material material;
        public HudAtlas atlasAset;
        public TMP_FontAsset fontAsset;

        [SerializeField]
        internal List<HudCanvasData> vCanvas;

        [SerializeField]
        internal List<HudImageData> vImages;
        [SerializeField]
        internal List<HudTextData> vTexts;

        [System.Serializable]
        public struct Hierarchy
        {
            public int id;
            public int parentId;
            public List<Hierarchy> children;
        }
        public List<Hierarchy> vHierarchies;

        private bool m_bInited = false;
        Dictionary<int, HudBaseData> m_vDatas = null;
        //--------------------------------------------------------
        void Init()
        {
            if (m_bInited)
                return;
            m_bInited = true;
            int cnt = 0;
            if (vCanvas != null) cnt += vCanvas.Count;
            if (vImages != null) cnt += vImages.Count;
            if (vTexts != null) cnt += vTexts.Count;
            if (m_vDatas == null)
            {
                m_vDatas = new Dictionary<int, HudBaseData>(cnt);
            }
            m_vDatas.Clear();
            if (vCanvas != null)
            {
                foreach (var cav in vCanvas)
                {
                    m_vDatas[cav.id] = cav;
                }
            }
            if (vImages != null)
            {
                foreach (var img in vImages)
                {
                    m_vDatas[img.id] = img;
                }
            }
            if (vTexts != null)
            {
                foreach (var txt in vTexts)
                {
                    m_vDatas[txt.id] = txt;
                }
            }
        }
        //--------------------------------------------------------
        public HudBaseData GetData(int id)
        {
            Init();
            if (m_vDatas == null)
                return null;
            if (m_vDatas.TryGetValue(id, out var data))
            {
                return data;
            }
            return null;
        }
#if UNITY_EDITOR
        //--------------------------------------------------------
        internal void EditInit()
        {
            m_bInited = false;
            Init();
        }
#endif
    }
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(HudObject))]
    public class HudObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            HudObject hudObject = (HudObject)target;
            if (GUILayout.Button("打开编辑器"))
            {
                Editor.HUDEditor.EditorHud(hudObject);
            }
        }
    }
#endif
}

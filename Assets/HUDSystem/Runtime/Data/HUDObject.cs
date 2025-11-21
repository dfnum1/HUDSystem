/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDObject
作    者:	HappLI
描    述:	HUD 数据对象层
*********************************************************************/
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Framework.HUD.Runtime
{
    public class HudObject : ScriptableObject
    {
        public Vector2 center = Vector2.zero;
        public Vector2 size = new Vector2(100,100);
        public bool allowScale = true;
        public bool allowRotation = true;

        public Mesh mesh;
        public Material material;
        public HudAtlas atlasAset;
        public TMP_FontAsset fontAsset;

        [SerializeField,HideInInspector]
        internal List<HudCanvasData> vCanvas;

        [SerializeField, HideInInspector]
        internal List<HudImageData> vImages;
        [SerializeField, HideInInspector]
        internal List<HudTextData> vTexts;
        [SerializeField, HideInInspector]
        internal List<HudNumberData> vNumbers;
        [SerializeField, HideInInspector]
        internal List<HudParticleData> vParticles;
        [SerializeField, HideInInspector]
        internal List<HudRichData> vRichs;

        [System.Serializable]
        public class Hierarchy
        {
            public int id;
            public int parentId;
            public List<int> children;
        }
        [HideInInspector]
        public List<Hierarchy> vHierarchies;

        private bool m_bInited = false;
        Dictionary<int, HudBaseData> m_vDatas = null;
        //--------------------------------------------------------
        internal void Init()
        {
            if (m_vDatas == null)
                m_bInited = false;
            if (m_bInited)
                return;
            m_bInited = true;
            int cnt = 0;
            if (vCanvas != null) cnt += vCanvas.Count;
            if (vImages != null) cnt += vImages.Count;
            if (vTexts != null) cnt += vTexts.Count;
            if (vNumbers != null) cnt += vNumbers.Count;
            if (vParticles != null) cnt += vParticles.Count;
            if (vRichs != null) cnt += vRichs.Count;
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
            if (vNumbers != null)
            {
                foreach (var txt in vNumbers)
                {
                    m_vDatas[txt.id] = txt;
                }
            }
            if (vParticles != null)
            {
                foreach (var txt in vParticles)
                {
                    m_vDatas[txt.id] = txt;
                }
            }
            if (vRichs != null)
            {
                foreach (var txt in vRichs)
                {
                    m_vDatas[txt.id] = txt;
                }
            }
        }
        //--------------------------------------------------------
        public Dictionary<int, HudBaseData> GetDatas()
        {
            Init();
            return m_vDatas;
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
            HudObject hudObject = (HudObject)target;
            DrawDefaultInspector();
            if(hudObject.vCanvas!=null) EditorGUILayout.LabelField("Canvas个数:" + hudObject.vCanvas.Count);
            if (hudObject.vImages != null) EditorGUILayout.LabelField("Image个数:" + hudObject.vImages.Count);
            if (hudObject.vTexts != null) EditorGUILayout.LabelField("Text个数:" + hudObject.vTexts.Count);
            if (hudObject.vRichs != null) EditorGUILayout.LabelField("富文本个数:" + hudObject.vRichs.Count);
            if (hudObject.vNumbers != null) EditorGUILayout.LabelField("Number个数:" + hudObject.vNumbers.Count);
            if (hudObject.vParticles != null) EditorGUILayout.LabelField("Particle个数:" + hudObject.vParticles.Count);
            if (GUILayout.Button("打开编辑器"))
            {
                Editor.HUDEditor.EditorHud(hudObject);
            }
            if (hudObject.atlasAset && GUILayout.Button("生成图集映射"))
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(hudObject));
                EditorPrefs.SetString("HudAtlas_GenAtlasMapping_GUID_Select", guid);
                HudAtlasEditor.GenAtlasMappingInfo(hudObject.atlasAset,true);
            }
            if (hudObject.fontAsset && GUILayout.Button("生产字体映射"))
            {
                Editor.HUDEditorInit.GenFontAtlasMapping(hudObject.fontAsset);
            }
        }
        //--------------------------------------------------------
        [UnityEditor.Callbacks.OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj != null && obj is HudObject)
            {
                Editor.HUDEditor.EditorHud(obj as HudObject);
                return true;
            }
            return false;
        }
    }
#endif
}

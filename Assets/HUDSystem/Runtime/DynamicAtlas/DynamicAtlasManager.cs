using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DynamicAtlas
{
    //--------------------------------------------------------
    public interface IDynamicAtlasHandle
    {
        void OnDynamicAtlasDone(string name, Sprite sprite);
    }
    //--------------------------------------------------------
    internal class DynamicAtlasManager
    {
        public enum eLoadResult
        {
            Success,
            Failure,
        }

        public struct Setting
        {
            public int ATLAS_SIZE;
            public int SINGLE_TEXTURE_MAX_SIZE;
            public int PADDING;
            public TextureFormat AtlasFormat;
            public Func<string, Task<Sprite>> LoadSpriteFunc;
            public Action<string, eLoadResult> AtlasAppendDone;
        }

        public static DynamicAtlasManager Instance
        {
            get
            {
                if (ms_pInstance == null)
                {
                    ms_pInstance = new DynamicAtlasManager();
                }
                return ms_pInstance;
            }
        }
        private static DynamicAtlasManager ms_pInstance;
        public static int ATLAS_SIZE { get; private set; } = 2048;
        public static int SINGLE_TEXTURE_MAX_SIZE { get; private set; } = 512;
        public static int PADDING { get; private set; } = 2;
        public static TextureFormat AtlasFormat { get; private set; } = TextureFormat.RGBA32;
        public static Func<string, Task<Sprite>> LoadSpriteFunc { get; private set; }
        public static Action<string, eLoadResult> AppendAtlasDone { get; private set; }
        private List<DynamicAtlas> m_DynamicAtlases = null;
        //--------------------------------------------------------
        public static void Init(Setting setting)
        {
            ATLAS_SIZE = setting.ATLAS_SIZE;
            SINGLE_TEXTURE_MAX_SIZE = setting.SINGLE_TEXTURE_MAX_SIZE;
            PADDING = setting.PADDING;
            AtlasFormat = setting.AtlasFormat;
            LoadSpriteFunc = setting.LoadSpriteFunc;
            AppendAtlasDone = setting.AtlasAppendDone;
        }
        //--------------------------------------------------------
        internal static void Update()
        {
            if (ms_pInstance == null || ms_pInstance.m_DynamicAtlases == null)
                return;
            for (int i = 0; i < ms_pInstance.m_DynamicAtlases.Count; i++)
            {
                ms_pInstance.m_DynamicAtlases[i].LateUpdate();
            }
        }
        //--------------------------------------------------------
        public static void Shudown()
        {
            if (ms_pInstance != null)
            {
                if(ms_pInstance.m_DynamicAtlases != null)
                {
                    foreach (var db in ms_pInstance.m_DynamicAtlases)
                        db.Destroy();
                    ms_pInstance.m_DynamicAtlases.Clear();
                }
            }
        }
        //--------------------------------------------------------
        public DynamicAtlas GetDynamicAtlas()
        {
            if (m_DynamicAtlases == null) m_DynamicAtlases = new List<DynamicAtlas>(2);
            for (int i = 0; i < m_DynamicAtlases.Count; i++)
            {
                if (!m_DynamicAtlases[i].IsFull)
                {
                    return m_DynamicAtlases[i];
                }
            }
            var newAtlas = new DynamicAtlas();
            m_DynamicAtlases.Add(newAtlas);
            return newAtlas;
        }
    }
}
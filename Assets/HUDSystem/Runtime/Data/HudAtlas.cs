/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDObject
作    者:	HappLI
描    述:	HUD 数据对象层
            方案来源https://gitee.com/pies/hud
*********************************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using System.Reflection;
using System.ComponentModel;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D;
#endif

namespace Framework.HUD.Runtime
{
    [CreateAssetMenu]
    public class HudAtlas : ScriptableObject, IHudAtlas
    {
        [SerializeField, Header("图集资源")]
        private SpriteAtlas m_SpriteAtlas;

        [SerializeField, Header("精灵映射纹理")]
        private Texture2D m_AtlasMappingTex;
        public Texture2D GetAtlasMappingTex() { return m_AtlasMappingTex; }

        [SerializeField, Header("图集大小")]
        private int m_Width;
        public int GetAtlasWidth() { return m_Width; }


        [SerializeField]
        private int m_Height;
        public int GetAtlasHeight() { return m_Height; }

        [SerializeField, Header("映射纹理大小")]
        private int m_AtlasMappingWidth;
        public int GetAtlasMappingWidth() { return m_AtlasMappingWidth; }

        [SerializeField]
        private int m_AtlasMappingHeight;
        public int GetAtlasMappingHeight() { return m_AtlasMappingHeight; }

        private bool m_bInied = false;
        private bool m_isGenAtlasMapping = false;

        [SerializeField, HideInInspector]
        private List<SpriteInfo> m_vSprites;
        private Dictionary<string, SpriteInfo> m_vNameToSpriteInfo = new Dictionary<string, SpriteInfo>();

        Texture m_AtlasTex;
        public Texture GetAtlasTexture()
        {
            Init();
#if UNITY_EDITOR
            GetPakAtlasTexture();
#endif
            if (m_AtlasTex != null) return m_AtlasTex;
            foreach (var item in m_vNameToSpriteInfo)
            {
                Sprite sprite = m_SpriteAtlas.GetSprite(item.Key);
                if (sprite != null)
                {
                    m_AtlasTex = sprite.texture;
                    return m_AtlasTex;
                }
            }
            return null;
        }
        //--------------------------------------------------------
        public SpriteInfo GetSpriteInfo(string name)
        {
            Init();
            SpriteInfo spriteInfo;
            if(m_vNameToSpriteInfo.TryGetValue(name, out spriteInfo))
                return spriteInfo;
            return SpriteInfo.DEF;
        }
        //--------------------------------------------------------
        public void CheckPackSprite(Sprite sprite, bool bForceProcessPack = false)
        {
        }
        //--------------------------------------------------------
        public void Init()
        {
            if (m_vSprites == null)
                return;

            if (m_bInied)
            {
                if (m_vNameToSpriteInfo == null || m_vNameToSpriteInfo.Count != m_vSprites.Count)
                    m_bInied = false;
            }
            if (m_bInied)
                return;
            m_AtlasTex = null;
            m_bInied = true;

            if (m_vNameToSpriteInfo == null)
                m_vNameToSpriteInfo = new Dictionary<string, SpriteInfo>(m_vSprites.Count);
            m_vNameToSpriteInfo.Clear();
            foreach (var db in m_vSprites)
            {
                if (string.IsNullOrEmpty(db.name))
                    continue;
                m_vNameToSpriteInfo[db.name] = db;
            }
        }
        //--------------------------------------------------------
        public void GenAtlasMappingInfo(bool bForce = false)
        {
#if UNITY_EDITOR

            if (m_isGenAtlasMapping && !bForce) return;
            if(!Application.isPlaying)
            {
                Debug.LogWarning("Please enter Play Mode to generate Atlas Mapping Info.");
                return;
            }       
            m_isGenAtlasMapping = true;

            m_AtlasTex = null;
            Editor.HUDEditorInit.ClearAllSubAssets(this);
            m_AtlasMappingTex = null;
            CollectRefresh();
            GenMappingTexture();
            GenAtlasMappingTextrue();
#endif
        }
#if UNITY_EDITOR
        //--------------------------------------------------------
        public static void RemoveSubAsset(UnityEngine.Object parentAsset, UnityEngine.Object subAsset)
        {
            if (parentAsset == null || subAsset == null)
                return;

            // 1. 从主资源移除
            AssetDatabase.RemoveObjectFromAsset(subAsset);

            // 2. 销毁内存对象
            UnityEngine.Object.DestroyImmediate(subAsset, true);

            // 3. 标记主资源已修改并保存
            EditorUtility.SetDirty(parentAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        //--------------------------------------------------------
        Texture GetPakAtlasTexture()
        {
            if (!Application.isPlaying)
            {
                if(m_SpriteAtlas != null)
                {
                    var getPreviewTexturesMethod = typeof(UnityEditor.U2D.SpriteAtlasExtensions).GetMethod("GetPreviewTextures", BindingFlags.NonPublic | BindingFlags.Static);

                    if (getPreviewTexturesMethod != null)
                    {
                        Texture2D[] textures = getPreviewTexturesMethod.Invoke(null, new object[] { m_SpriteAtlas }) as Texture2D[];
                        if (textures != null && textures.Length > 0)
                        {
                            m_AtlasTex = textures[0];
                        }
                    }
                }

            }
            else
            {
                Sprite[] sprits = new Sprite[m_SpriteAtlas.spriteCount];
                m_SpriteAtlas.GetSprites(sprits);
                m_AtlasTex = sprits[0].texture;
            }
            return m_AtlasTex;
        }
        //--------------------------------------------------------
        private void GenMappingTexture()
        {
            int size = GetTexSize();
            m_AtlasMappingWidth = size;
            m_AtlasMappingHeight = size;
            if (m_AtlasMappingTex != null && m_AtlasMappingTex.width == size)
            {
                m_AtlasMappingTex.SetPixels(new Color[size * size]);
                m_AtlasMappingTex.Apply();
                return;
            }
            if (m_AtlasMappingTex != null)
            {
                m_AtlasMappingTex.Reinitialize(size, size);
                m_AtlasMappingTex.SetPixels(new Color[size * size]);
                m_AtlasMappingTex.Apply();
                return;
            }
            m_AtlasMappingTex = new Texture2D(size, size, TextureFormat.RGBA32, false, PlayerSettings.colorSpace == ColorSpace.Linear);
            m_AtlasMappingTex.wrapMode = TextureWrapMode.Clamp;
            m_AtlasMappingTex.filterMode = FilterMode.Point;
            m_AtlasMappingTex.SetPixels(new Color[size * size]);
            m_AtlasMappingTex.Apply();
            string path = AssetDatabase.GetAssetPath(this);
            AssetDatabase.AddObjectToAsset(m_AtlasMappingTex, path);
            EditorUtility.SetDirty(m_SpriteAtlas);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        //--------------------------------------------------------
        public void CollectRefresh()
        {
            if (m_vNameToSpriteInfo == null) m_vNameToSpriteInfo = new Dictionary<string, SpriteInfo>();
            if (m_vSprites == null) m_vSprites = new List<SpriteInfo>();
            m_vNameToSpriteInfo.Clear();
            m_vSprites.Clear();
            SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { m_SpriteAtlas }, EditorUserBuildSettings.activeBuildTarget);
            if (m_SpriteAtlas.spriteCount == 0) return;
            Sprite[] sprits = new Sprite[m_SpriteAtlas.spriteCount];
            m_SpriteAtlas.GetSprites(sprits);
            Array.Sort(sprits, (a, b) => { return a.name.CompareTo(b.name); });
            int spriteId = 0;
            for (int i = 0; i < sprits.Length; i++)
            {
                Sprite sprite = sprits[i];

                if (sprite.border.SqrMagnitude() > 0)
                {
                    SpriteInfo spriteInfo = new SpriteInfo();
                    spriteInfo.index = spriteId;
                    string name = sprits[i].name.Replace("(Clone)", "");
                    m_vNameToSpriteInfo[name] = spriteInfo;
                    spriteId++;
                    for (int s = 0; s < 9; s++)
                    {
                        SpriteInfo info = new SpriteInfo();
                        info.index = spriteId;
                        info.name = name + "_" + s;
                        m_vNameToSpriteInfo[name + "_" + s] = info;
                        spriteId++;
                    }
                }
                else
                {
                    SpriteInfo spriteInfo = new SpriteInfo();
                    spriteInfo.index = spriteId;
                    string name = sprits[i].name.Replace("(Clone)", "");
                    spriteInfo.name = name;
                    m_vNameToSpriteInfo[name] = spriteInfo;
                    spriteId++;
                }
            }
            foreach(var db in m_vNameToSpriteInfo)
            {
                m_vSprites.Add(db.Value);
            }
        }
        //--------------------------------------------------------
        Vector4 GetInnerUV(Sprite sprite)
        {
            if (Application.isPlaying)
                return UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            else
            {
                Rect rect = sprite.rect;
                Texture2D tex = GetPakAtlasTexture() as Texture2D;
                Vector4 padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);

                float atlasWidth = tex.width;
                float atlasHeight = tex.height;

                float xMin = (rect.x + padding.x) / atlasWidth;
                float yMin = (rect.y + padding.y) / atlasHeight;
                float xMax = (rect.x + rect.width - padding.z) / atlasWidth;
                float yMax = (rect.y + rect.height - padding.w) / atlasHeight;

                return new Vector4(xMin, yMin, xMax, yMax);
            }
        }
        //--------------------------------------------------------
        Vector4 GetOuterUV(Sprite sprite)
        {
            if (Application.isPlaying)
                return UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            else
            {
                Rect rect = sprite.rect;
                Texture2D tex = GetPakAtlasTexture() as Texture2D;

                float atlasWidth = tex.width;
                float atlasHeight = tex.height;

                float xMin = rect.x / atlasWidth;
                float yMin = rect.y / atlasHeight;
                float xMax = (rect.x + rect.width) / atlasWidth;
                float yMax = (rect.y + rect.height) / atlasHeight;

                return new Vector4(xMin, yMin, xMax, yMax);
            }
        }
        //--------------------------------------------------------
        private void GenAtlasMappingTextrue()
        {
            if (m_SpriteAtlas == null || m_SpriteAtlas.spriteCount == 0 || m_vNameToSpriteInfo == null) return;
            Sprite[] sprits = new Sprite[m_SpriteAtlas.spriteCount];
            m_SpriteAtlas.GetSprites(sprits);
            Texture atlasTex = GetPakAtlasTexture();
            int atlasWidth = atlasTex.width;
            int atlasHeight = atlasTex.height;
            int mappingWidth = m_AtlasMappingTex.width;
            m_Width = atlasWidth;
            m_Height = atlasHeight;
            for (int i = 0; i < sprits.Length; i++)
            {
                Sprite sprite = sprits[i];
                string name = sprite.name.Replace("(Clone)", "");
                SpriteInfo spriteInfo;
                if (m_vNameToSpriteInfo.TryGetValue(name, out spriteInfo))
                {
                    var uv = GetOuterUV(sprite);
                    SetSpriteUV(ref spriteInfo, new Vector2(uv.x, uv.y), new Vector2(uv.z, uv.w));
                    m_vNameToSpriteInfo[name] = spriteInfo;
                }
                if (sprite.border.SqrMagnitude() > 0)
                {
                    Vector4 outer, inner, padding, border;
                    outer = GetOuterUV(sprite);
                    inner = GetInnerUV(sprite);
                    padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
                    border = sprite.border;
                    Vector2[] s_UVScratch = new Vector2[4];
                    s_UVScratch[0] = new Vector2(outer.x, outer.y);
                    s_UVScratch[1] = new Vector2(inner.x, inner.y);
                    s_UVScratch[2] = new Vector2(inner.z, inner.w);
                    s_UVScratch[3] = new Vector2(outer.z, outer.w);
                    int spritesliceId = 0;
                    for (int x = 0; x < 3; ++x)
                    {
                        int x2 = x + 1;
                        for (int y = 0; y < 3; ++y)
                        {
                            int y2 = y + 1;
                            SpriteInfo info;
                            if (m_vNameToSpriteInfo.TryGetValue(name + "_" + spritesliceId, out info))
                            {
                                SetSpriteUV(ref info, new Vector2(s_UVScratch[x].x, s_UVScratch[y].y), new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
                                m_vNameToSpriteInfo[name + "_" + spritesliceId] = spriteInfo;
                                spritesliceId++;
                            }
                        }
                    }
                }
            }
            m_AtlasMappingTex.Apply();
        }
        //--------------------------------------------------------
        private void SetSpriteUV(ref SpriteInfo spriteInfo, Vector2 min, Vector2 max)
        {
            int atlasWidth = m_AtlasTex.width;
            int atlasHeight = m_AtlasTex.height;
            int mappingWidth = m_AtlasMappingTex.width;
            ushort spriteWidth = (ushort)((max.x - min.x) * atlasWidth);
            ushort spriteHeight = (ushort)((max.y - min.y) * atlasHeight);
            ushort spriteX = (ushort)(min.x * atlasWidth);
            ushort spriteY = (ushort)(min.y * atlasHeight);
            spriteInfo.size = new Vector2Int(spriteWidth, spriteHeight);
            byte posx0bytes = (byte)(spriteX % 256);
            byte posx1bytes = (byte)(spriteX / 256);
            byte posy0bytes = (byte)(spriteY % 256);
            byte posy1bytes = (byte)(spriteY / 256);

            byte spritew0bytes = (byte)(spriteWidth % 256);
            byte spritew1bytes = (byte)(spriteWidth / 256);
            byte spriteh0bytes = (byte)(spriteHeight % 256);
            byte spriteh1bytes = (byte)(spriteHeight / 256);

            int firstindex = spriteInfo.index * 2;
            int secondindex = firstindex + 1;

            int firstX = firstindex % mappingWidth;
            int firstY = firstindex / mappingWidth;

            int secondX = secondindex % mappingWidth;
            int secondY = secondindex / mappingWidth;

            m_AtlasMappingTex.SetPixel(firstX, firstY, new Color32(posx1bytes, posx0bytes, posy1bytes, posy0bytes));
            m_AtlasMappingTex.SetPixel(secondX, secondY, new Color32(spritew1bytes, spritew0bytes, spriteh1bytes, spriteh0bytes));
        }
        //--------------------------------------------------------
        private int GetTexSize()
        {
            int size = 1;
            if (m_vNameToSpriteInfo == null)
                return 1;
            int spriteCount = m_vNameToSpriteInfo.Count;
            for (int i = 0; i <= 10; i++)
            {
                size = (int)Mathf.Pow(2, i);
                if (size * size >= spriteCount * 2)
                {
                    return size;
                }
            }
            return size;
        }
#endif
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(HudAtlas))]
    internal class HudAtlasEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            HudAtlas hudAtlas = (HudAtlas)target;
            if (GUILayout.Button("生成图集映射"))
            {
                GenAtlasMappingInfo(hudAtlas);
            }
        }
        //--------------------------------------------------------
        public static void GenAtlasMappingInfo(HudAtlas hudAtlas, bool bDirtyRefresh = true)
        {
            EditorPrefs.DeleteKey("HudAtlas_GenAtlasMapping_GUID");
            if (!Application.isPlaying)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(hudAtlas));
                EditorPrefs.SetString("HudAtlas_GenAtlasMapping_GUID", guid);

                var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                UnityEditor.EditorApplication.isPlaying = true;
                return;
            }
            hudAtlas.GenAtlasMappingInfo(true);
            EditorUtility.SetDirty(hudAtlas);
            if(bDirtyRefresh)
            {
                AssetDatabase.SaveAssetIfDirty(hudAtlas);
                AssetDatabase.Refresh();
            }
        }
    }
#endif
}

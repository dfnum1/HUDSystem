/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDEditorInit
作    者:	HappLI
描    述:	HUD 编辑器初始化
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Framework.HUD.Editor
{
    [InitializeOnLoad]
    class HUDEditorInit
    {
        static Texture2D s_CustomIcon;
        static string m_strInstallPath = null;
        public static Texture2D LoadTexture(string path)
        {
            if (m_strInstallPath == null)
            {
                string[] scripts = AssetDatabase.FindAssets("t:Script HudEditor");
                if (scripts.Length > 0)
                {
                    string installPath = System.IO.Path.GetDirectoryName(UnityEditor.AssetDatabase.GUIDToAssetPath(scripts[0])).Replace("\\", "/");

                    installPath = Path.Combine(installPath, "EditorResources").Replace("\\", "/");
                    if (System.IO.Directory.Exists(installPath))
                    {
                        m_strInstallPath = installPath;
                    }
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(System.IO.Path.Combine(m_strInstallPath, path));
        }
        //-----------------------------------------------------
        static HUDEditorInit()
        {
            s_CustomIcon = LoadTexture("icon.png");
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        //-----------------------------------------------------
        static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            if (s_CustomIcon == null) return;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj is HudObject)
            {
                //      Rect iconRect = new Rect(selectionRect.x + 2, selectionRect.y + 2, 16, 16);
                //       GUI.DrawTexture(iconRect, s_CustomIcon, ScaleMode.ScaleToFit);
                if (EditorGUIUtility.GetIconForObject(obj) != s_CustomIcon)
                    EditorGUIUtility.SetIconForObject(obj, s_CustomIcon);
            }
        }
        //-----------------------------------------------------
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // 检查标记
                string guid = EditorPrefs.GetString("HudAtlas_GenAtlasMapping_GUID", "");
                if (!string.IsNullOrEmpty(guid))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var hudAtlas = AssetDatabase.LoadAssetAtPath<Framework.HUD.Runtime.HudAtlas>(path);
                    if (hudAtlas != null)
                    {
                        hudAtlas.GenAtlasMappingInfo(true);
                        EditorPrefs.DeleteKey("HudAtlas_GenAtlasMapping_GUID");
                        EditorApplication.isPlaying = false;
                        string selectGuid = EditorPrefs.GetString("HudAtlas_GenAtlasMapping_GUID_Select", "");
                        EditorPrefs.DeleteKey("HudAtlas_GenAtlasMapping_GUID_Select");
                        if(!string.IsNullOrEmpty(selectGuid))
                        {
                            path = AssetDatabase.GUIDToAssetPath(selectGuid);
                            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                        }
                        else
                            Selection.activeObject = hudAtlas;
                    }
                }
            }
        }
        //-----------------------------------------------------
        [MenuItem("Assets/Hud/HudAtlasMapping", false, 350)]
        static void AtlasMapping()
        {
            SpriteAtlas spriteAtlas = Selection.activeObject as SpriteAtlas;
            string path = AssetDatabase.GetAssetPath(spriteAtlas);
            string dirpath = Path.GetDirectoryName(path);
            string filename = Path.GetFileNameWithoutExtension(path);
            string mappath = Path.Combine(dirpath, filename + ".asset");
            HudAtlas atlasMapping = ScriptableObject.CreateInstance<HudAtlas>();
            SerializedObject so = new SerializedObject(atlasMapping);
            SerializedProperty sp = so.FindProperty("m_SpriteAtlas");
            sp.objectReferenceValue = spriteAtlas;
            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(atlasMapping, mappath);
            AssetDatabase.Refresh();
        }
        //-----------------------------------------------------
        [MenuItem("Assets/Hud/HudAtlasMapping", true, 350)]
        static bool ValidateAtlasMapping()
        {
            return Selection.activeObject is SpriteAtlas;
        }
        //-----------------------------------------------------
        static Texture2D GetFontMapping(TMP_FontAsset tmp_fontAtlas)
        {
            string path = AssetDatabase.GetAssetPath(tmp_fontAtlas);
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < objs.Length; i++)
            {
                Texture2D tex2d = objs[i] as Texture2D;
                if (tex2d != null && tex2d.name == "FontAtlasMapping") return tex2d;
            }
            return null;
        }
        //-----------------------------------------------------
        [MenuItem("Assets/Hud/FontAtlasMapping", false, 350)]
        static void FontAtlasMapping()
        {
            TMP_FontAsset tmp_fontAtlas = Selection.activeObject as TMP_FontAsset;
            GenFontAtlasMapping(tmp_fontAtlas, true);
        }
        //-----------------------------------------------------
        [MenuItem("Assets/Hud/FontAtlasMapping", true, 350)]
        static bool ValidateFontAtlasMapping()
        {
            return Selection.activeObject is TMP_FontAsset;
        }
        //-----------------------------------------------------
        internal static void GenFontAtlasMapping(TMP_FontAsset tmp_fontAtlas, bool bDirtyRefresh = true)
        {
            if (tmp_fontAtlas == null)
                return;
            Texture2D fontmapping = GetFontMapping(tmp_fontAtlas);
            if (fontmapping != null)
            {
                tmp_fontAtlas.SetFontAtlasMapping(fontmapping);
                return;
            }
            Texture2D m_FontAtlasMappingTex = new Texture2D(64, 64, TextureFormat.RGBA32, false, PlayerSettings.colorSpace == ColorSpace.Linear);
            m_FontAtlasMappingTex.wrapMode = TextureWrapMode.Clamp;
            m_FontAtlasMappingTex.filterMode = FilterMode.Point;
            m_FontAtlasMappingTex.name = "FontAtlasMapping";
            tmp_fontAtlas.SetFontAtlasMapping(m_FontAtlasMappingTex);
            string path = AssetDatabase.GetAssetPath(tmp_fontAtlas);
            AssetDatabase.AddObjectToAsset(m_FontAtlasMappingTex, path);
            EditorUtility.SetDirty(tmp_fontAtlas);
            if(bDirtyRefresh)
            {
                AssetDatabase.SaveAssetIfDirty(tmp_fontAtlas);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
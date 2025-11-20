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
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

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
                    }
                }
            }
        }
    }
}
#endif
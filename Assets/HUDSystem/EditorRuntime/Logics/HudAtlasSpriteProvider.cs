#if UNITY_EDITOR
using Framework.HUD.Runtime;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using static PlasticGui.WorkspaceWindow.Merge.MergeInProgress;

public class HudAtlasSpriteSearchProvider : ScriptableObject, ISearchWindowProvider
{
    public HudAtlas atlas;
    public System.Action<Sprite> onSelect;
    List<Sprite> m_vSprites;

    public HudAtlasSpriteSearchProvider()
    {
        m_vSprites = null;
    }

    void InitData()
    {
        m_vSprites = new List<Sprite>();
        m_vSprites.Clear();
        var filed = atlas.GetType().GetField("m_SpriteAtlas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (filed == null)
            return;


        var obj = filed.GetValue(atlas);
        if (obj == null)
            return;

        SpriteAtlas spriteAtlas = (SpriteAtlas)filed.GetValue(atlas);
        var packables = SpriteAtlasExtensions.GetPackables(spriteAtlas);
        foreach (var item in packables)
        {
            if (item is DefaultAsset) // 文件夹
            {
                string folderPath = AssetDatabase.GetAssetPath(item);
                string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null && !m_vSprites.Contains(sprite))
                        m_vSprites.Add(sprite);
                }
            }
            else if (item is Sprite)
            {
                Sprite sprite = item as Sprite;
                if (sprite != null && !m_vSprites.Contains(sprite))
                    m_vSprites.Add(sprite);
            }
            else if (item is Texture2D)
            {
                string texPath = AssetDatabase.GetAssetPath(item);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(texPath);
                foreach (var asset in sprites)
                {
                    if (asset is Sprite sprite && !m_vSprites.Contains(sprite))
                        m_vSprites.Add(sprite);
                }
            }
        }
    }
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        InitData();
        var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("图片资源"), 0)
            };
        {
            string display = $"None";
            var entry = new SearchTreeEntry(new GUIContent(display))
            {
                level = 1,
                userData = null
            };
            tree.Add(entry);
        }
        if(m_vSprites!=null)
        {
            foreach (var info in m_vSprites)
            {
                string display = $"{info.name}";
                string path = AssetDatabase.GetAssetPath(info);
                Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                if (texture == null) continue;
                var entry = new SearchTreeEntry(new GUIContent(display, texture))
                {
                    level = 1,
                    userData = info
                };
                tree.Add(entry);
            }
        }

        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
        if(entry.userData == null)
        {
            onSelect?.Invoke(null);
            return true;
        }
        else if (entry.userData is Sprite)
        {
            onSelect?.Invoke((Sprite)entry.userData);
            return true;
        }
        return false;
    }
}
#endif
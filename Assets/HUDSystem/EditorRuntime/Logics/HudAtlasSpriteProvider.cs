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
    Sprite[] m_vSprites;

    public HudAtlasSpriteSearchProvider()
    {
        m_vSprites = null;
    }

    void InitData()
    {
        m_vSprites = null;
        var filed = atlas.GetType().GetField("m_SpriteAtlas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (filed == null)
            return;


        var obj = filed.GetValue(atlas);
        if (obj == null)
            return;

        SpriteAtlas spriteAtlas = (SpriteAtlas)filed.GetValue(atlas);
        int count = spriteAtlas.spriteCount;
        var sprites = new Sprite[count];
        spriteAtlas.GetSprites(sprites);
        m_vSprites = sprites;
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
                string name = display.Replace("(Clone)", "");
                Sprite sprite = null;
                Texture2D icon = null;
                if (info.texture == null) continue;
                string assetPath = AssetDatabase.GetAssetPath(info.texture);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

                // 尝试获取Sprite的缩略图
                icon = AssetPreview.GetAssetPreview(info);
                var entry = new SearchTreeEntry(new GUIContent(name, icon))
                {
                    level = 1,
                    userData = sprite
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
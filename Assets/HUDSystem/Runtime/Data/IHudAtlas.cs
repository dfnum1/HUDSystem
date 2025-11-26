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
    [System.Serializable]
    public struct SpriteInfo
    {
        public string name;
        public int index;
        public Vector2Int size;

        public static SpriteInfo DEF = new SpriteInfo() { name = null, index = -1, size = Vector2Int.zero };
        public bool IsValid()
        {
            return index >= 0 && !string.IsNullOrEmpty(name) && size.sqrMagnitude>0;
        }
    }
    public interface IHudAtlas
    {
        void Init();
        int GetAtlasWidth();
        int GetAtlasHeight();

        Texture2D GetAtlasMappingTex();
        int GetAtlasMappingWidth();
        int GetAtlasMappingHeight();

        Texture GetAtlasTexture();
        SpriteInfo GetSpriteInfo(string name);
        void CheckPackSprite(Sprite sprite, bool bForceProcessPack = false);

        void GenAtlasMappingInfo(bool bForce = false);
    }
}

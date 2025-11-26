using DaVikingCode.RectanglePacking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DynamicAtlas
{
    internal class DynamicTextureData
    {
        IntegerRectangle m_Rect = new IntegerRectangle();
        public int Id { get { return Rect.id; } }
        public int ReferenceCount { get; private set; }
        public IntegerRectangle Rect { get { return m_Rect; } }
        public Sprite Sprite { get; private set; }

        public DynamicTextureData(int index, IntegerRectangle rect)
        {
            m_Rect = rect;
            m_Rect.id = index;
            ReferenceCount = 0;
        }

        public void SetSprite(Sprite sprite, string sprite_name, int x, int y, int width, int height)
        {
            sprite.name = sprite_name;
            Sprite = sprite;
            m_Rect.x = x;
            m_Rect.y = y;
            m_Rect.width = width;
            m_Rect.height = height;
        }

        public void AddReference()
        {
            ReferenceCount++;
        }

        public void RemoveReference()
        {
            ReferenceCount--;
        }
    }
}
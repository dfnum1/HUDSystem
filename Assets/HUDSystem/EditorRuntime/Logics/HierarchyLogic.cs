/********************************************************************
生成日期:	11:11:2025
类    名: 	HierarchyLogic
作    者:	HappLI
描    述:	HUD 图元节点
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Framework.HUD.Editor.TreeAssetView;

namespace Framework.HUD.Editor
{
    public class HierarchyLogic : AEditorLogic
    {
        class WidgetItem : TreeAssetView.ItemData
        {
            public AGraphic graphicItem;
            public override Color itemColor()
            {
                return Color.white;
            }
        }

        List<AGraphic> m_vTopGraphics = new List<AGraphic>();
        TreeAssetView m_pTree = null;
        bool m_bItemRightClick = false;
        public HierarchyLogic(HUDEditor editor, Rect viewRect) : base(editor,viewRect)
        {
        }
        //--------------------------------------------------------
        public override void OnEnable()
        {
            if(m_pTree == null)
            {
                m_pTree = new TreeAssetView(new string[] { "节点列表" });
                m_pTree.buildMutiColumnDepth = true;
                m_pTree.DepthIndentWidth = 20;
                m_pTree.OnCellDraw += OnItemDraw;
                m_pTree.OnViewRightClick += OnViewRightClick;
                m_pTree.OnItemRightClick += OnItemRightClick;
                m_pTree.OnDragItem += OnDragItem;
                m_pTree.Reload();
            }
            RefreshTree();
        }
        //--------------------------------------------------------
        void RefreshTree()
        {
            m_pTree.BeginTreeData();
            foreach (var db in m_vTopGraphics)
            {
                AddGraphicItem(db,0);
            }
            m_pTree.EndTreeData();
        }
        //--------------------------------------------------------
        void AddGraphicItem(AGraphic grapic, int depth)
        {
            var item = new WidgetItem();
            item.depth = depth;
            item.id = grapic.id;
            item.name = grapic.name;
            item.graphicItem = grapic;
            m_pTree.AddData(item);
            var childs = grapic.GetChilds();
            if (childs == null || childs.Count <= 0)
                return;
            for (int i =0; i < childs.Count; ++i) 
            {
                AddGraphicItem(childs[i], depth+1);
            }
        }
        //--------------------------------------------------------
        public override void OnGUI()
        {
            if(m_pTree!=null)
            {
                m_pTree.GetColumn(0).width = viewRect.width;
                m_pTree.OnGUI(viewRect);
            }
        }
        //--------------------------------------------------------
        bool OnDragItem(TreeAssetView.TreeItemData item)
        {
            return true;
        }
        //--------------------------------------------------------
        void OnViewRightClick()
        {
            if (m_bItemRightClick)
                return;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Widget/Node"), false, () =>
            {
                OnCreateItem(typeof(HUDNode), null);
            });
            menu.AddItem(new GUIContent("Widget/Text"), false, () =>
            {
                OnCreateItem(typeof(HUDText), null);
            });
            menu.AddItem(new GUIContent("Widget/Sprite"), false, () =>
            {
                OnCreateItem(typeof(HUDSprite), null);
            });
            menu.ShowAsContext();
            m_bItemRightClick = false;
        }
        //--------------------------------------------------------
        void OnItemRightClick(ItemData itemData)
        {
            m_bItemRightClick = true;
            WidgetItem widget = itemData as WidgetItem;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Widget/Node"), false, () =>
            {
                OnCreateItem(typeof(HUDNode), widget);
            });
            menu.AddItem(new GUIContent("Widget/Text"), false, () =>
            {
                OnCreateItem(typeof(HUDText), widget);
            });
            menu.AddItem(new GUIContent("Widget/Sprite"), false, () =>
            {
                OnCreateItem(typeof(HUDSprite), widget);
            });
            menu.ShowAsContext();
        }
        //--------------------------------------------------------
        AGraphic OnCreateItem(System.Type type, WidgetItem item)
        {
            var grapicItem = Activator.CreateInstance(type);
            if (grapicItem == null)
                return null;
            var grapic = grapicItem as AGraphic;
            if (grapic == null)
                return null;

            grapic.id = GeneratorID();
            grapic.name = type.Name;

            if (item!=null)
            {
                item.graphicItem.Attack(grapic);
            }
            else
            {
                m_vTopGraphics.Add(grapic);
            }

            RefreshTree();
            return grapic;
        }
        //--------------------------------------------------------
        bool OnItemDraw(RowArgvData argvData)
        {
            WidgetItem widget = argvData.itemData.data as WidgetItem;
            var rowRect = argvData.rowRect;
            if (widget.depth > 0)
                rowRect.x += widget.depth * m_pTree.DepthIndentWidth;
            EditorGUI.LabelField(rowRect, widget.name);
            return true;
        }
        //--------------------------------------------------------
        int GeneratorID()
        {
            int id = 0;
            HashSet<int> vIds = new HashSet<int>();
            foreach(var db in m_vTopGraphics)
            {
                CollectIds(db,vIds);
            }
            while(vIds.Contains(id))
            {
                id++;
            }
            return id;
        }
        //--------------------------------------------------------
        void CollectIds(AGraphic grapic, HashSet<int> vIds)
        {
            vIds.Add(grapic.id);
            if (grapic.GetChilds() == null)
                return;
            var childs = grapic.GetChilds();
            foreach(var db in childs)
            {
                CollectIds(db, vIds);
            }
        }
    }
}
#endif
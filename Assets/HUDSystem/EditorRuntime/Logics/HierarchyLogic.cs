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
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static Framework.HUD.Editor.TreeAssetView;

namespace Framework.HUD.Editor
{
    public class HierarchyLogic : AEditorLogic
    {
        class WidgetItem : TreeAssetView.ItemData
        {
            public AWidget graphicItem;
            public override Color itemColor()
            {
                return Color.white;
            }
        }

        List<AWidget> m_vTopGraphics = new List<AWidget>();
        TreeAssetView m_pTree = null;
        bool m_bItemRightClick = false;
        bool m_bTreeCallSelectChange = false;

        List<System.Type> m_vHudTypes = new List<Type>();
        public HierarchyLogic(HUDEditor editor, Rect viewRect) : base(editor,viewRect)
        {

        }
        //--------------------------------------------------------
        public override void OnEnable()
        {
            m_vHudTypes.Clear();
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = assembly.GetTypes();
                for (int t = 0; t < types.Length; ++t)
                {
                    System.Type enumType = types[t];
                    if (!enumType.IsDefined(typeof(HudDataAttribute)))
                        continue;
                    m_vHudTypes.Add(enumType);
                }
            }
            if (m_pTree == null)
            {
                m_pTree = new TreeAssetView(new string[] { "节点列表" });
                m_pTree.buildMutiColumnDepth = true;
                m_pTree.DepthIndentWidth = 20;
                m_pTree.ShowAlternatingRowBackgrounds(false);
                m_pTree.ShowBorder(true);
                m_pTree.OnCellDraw += OnItemDraw;
                m_pTree.OnViewRightClick += OnViewRightClick;
                m_pTree.OnItemRightClick += OnItemRightClick;
                m_pTree.OnDragItem += OnDragItem;
                m_pTree.OnDragDrop += OnDragDrop;
                m_pTree.OnSelectChange += OnItemSelected;
                m_pTree.Reload();
            }
            RefreshTree();
        }
        //--------------------------------------------------------
        public override void OnSetHudObject(HudObject hudObject)
        {
            GetHud().Destroy();
            GetHud().SetHudObject(hudObject);
            var canvas = GetHud().GetTopWidgets();
            if (canvas != null)
            {
                foreach (var db in canvas)
                {
                    m_vTopGraphics.Add(db);
                }
                RefreshTree();
            }
        }
        //--------------------------------------------------------
        public override void OnSave()
        {
            var hudObj = m_pEditor.GetHUDObject();
            if (hudObj == null)
                return;

            var typeLists = ConvertToDataList();
            var fields = hudObj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            for(int i =0;i < fields.Length; ++i)
            {
                if (!fields[i].FieldType.IsGenericType)
                    continue;
                var genericTypes = fields[i].FieldType.GenericTypeArguments;
                if (genericTypes == null || genericTypes.Length != 1)
                    continue;
                if(typeLists.TryGetValue(genericTypes[0],out var vTemps))
                {
                    var targetType = genericTypes[0];
                    var listType = typeof(List<>).MakeGenericType(targetType);
                    var listInstance = Activator.CreateInstance(listType) as System.Collections.IList;

                    foreach (var baseData in vTemps)
                    {
                        listInstance.Add(baseData);
                    }
                    fields[i].SetValue(hudObj, listInstance);
                }
            }

            List<Framework.HUD.Runtime.HudObject.Hierarchy> hierarchies = new List<Framework.HUD.Runtime.HudObject.Hierarchy>();
            foreach (var top in m_vTopGraphics)
            {
                BuildHierarchy(top, hierarchies);
            }
            hudObj.vHierarchies = hierarchies;
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
        void AddGraphicItem(AWidget grapic, int depth)
        {
            var item = new WidgetItem();
            item.depth = depth;
            item.id = grapic.GetId();
            item.name = grapic.GetName();
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
        protected override void OnGUI()
        {
            if(m_pTree!=null)
            {
                m_pTree.GetColumn(0).width = viewRect.width;
                m_pTree.OnGUI(new Rect(0,0,viewRect.width, viewRect.height));
            }
            if(Event.current.type == EventType.KeyUp)
            {
                if(Event.current.keyCode == KeyCode.Escape)
                {
                    m_pTree.SetSelection(new List<int>());
                    m_pEditor.OnSelectComponent(null);
                }
            }
        }
        //--------------------------------------------------------
        bool OnDragItem(TreeAssetView.TreeItemData item)
        {
            return m_pTree.IsSelected(item.id);
        }
        //--------------------------------------------------------
        protected void OnDragDrop(TreeAssetView.DragAndDropData drop)
        {
            var dragItem = drop.current?.data as WidgetItem;
            var targetParentItem = drop.parentItem?.data as WidgetItem;

            if (dragItem == null)
                return;

            var dragComp = dragItem.graphicItem;
            var oldParent = dragComp.GetParent();

            // 禁止拖拽到自身或子节点下
            if (targetParentItem != null)
            {
                var targetParentComp = targetParentItem.graphicItem;
                if (dragComp == targetParentComp || IsChildOf(dragComp, targetParentComp))
                {
                //    EditorUtility.DisplayDialog("错误", "不能拖拽到自身或子节点下！", "确定");
                    return;
                }
            }

            // 1. 从原父节点移除
            if (oldParent != null)
            {
                oldParent.Detach(dragComp);
            }
            else
            {
                m_vTopGraphics.Remove(dragComp);
            }

            // 2. 添加到新父节点
            if (targetParentItem != null)
            {
                var targetParentComp = targetParentItem.graphicItem;
                targetParentComp.Attach(dragComp, drop.insertAtIndex);
                GetHud().RemoveTopWidget(dragComp);
            }
            else
            {
                if (!m_vTopGraphics.Contains(dragComp))
                {
                    if (drop.insertAtIndex >= 0 && drop.insertAtIndex <= m_vTopGraphics.Count)
                        m_vTopGraphics.Insert(drop.insertAtIndex, dragComp);
                    else
                        m_vTopGraphics.Add(dragComp);
                    GetHud().AddTopWidget(dragComp);
                }
            }
            RefreshTree();
        }
        //--------------------------------------------------------
        bool IsChildOf(AWidget parent, AWidget child)
        {
            if (parent == null || child == null) return false;
            var childs = parent.GetChilds();
            if (childs == null) return false;
            foreach (var c in childs)
            {
                if (c == child || IsChildOf(c, child))
                    return true;
            }
            return false;
        }
        //--------------------------------------------------------
        void OnItemSelected(TreeAssetView.ItemData item)
        {
            WidgetItem widget = item as WidgetItem;
            m_bTreeCallSelectChange = true;
            m_pEditor.OnSelectComponent(widget.graphicItem);
            m_bTreeCallSelectChange = false;
        }
        //--------------------------------------------------------
        internal override void OnSelectComponent(AWidget component)
        {
            if (m_bTreeCallSelectChange) return;
            if (component == null)
                return;
            m_pTree.SetSelection(new List<int>() { component.GetId() });
            m_pTree.SetExpandedRecursive(component.GetId(), true);
        }
        //--------------------------------------------------------
        void OnViewRightClick()
        {
            if (m_bItemRightClick)
                return;
            GenericMenu menu = new GenericMenu();
            foreach (var tp in m_vHudTypes)
            {
                menu.AddItem(new GUIContent("Widget/" + tp.Name), false, () =>
                {
                    OnCreateItem(tp, null);
                });
            }
            menu.ShowAsContext();
            m_bItemRightClick = false;
        }
        //--------------------------------------------------------
        void OnItemRightClick(ItemData itemData)
        {
            m_bItemRightClick = true;
            WidgetItem widget = itemData as WidgetItem;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                widget.graphicItem.Destroy();
                GetHud().OnWidgetDestroy(widget.graphicItem);
                RefreshTree();
            });
            foreach(var tp in m_vHudTypes)
            {
                menu.AddItem(new GUIContent("Widget/" + tp.Name), false, () =>
                {
                    OnCreateItem(tp, widget);
                });
            }
            menu.ShowAsContext();
        }
        //--------------------------------------------------------
        AWidget OnCreateItem(System.Type type, WidgetItem item)
        {
            HudDataAttribute hudAttr = type.GetCustomAttribute<HudDataAttribute>();
            if (hudAttr == null)
                return null;
            var hudData = Activator.CreateInstance(hudAttr.dataType);
            var grapicItem = Activator.CreateInstance(type,m_pEditor.GetHudSystem(), hudData);
            if (grapicItem == null)
                return null;
            var grapic = grapicItem as AWidget;
            if (grapic == null)
                return null;

            grapic.SetId(GeneratorID());
            grapic.SetName(type.Name);
            grapic.SetHudController(GetHud());
            if (item!=null)
            {
                item.graphicItem.Attach(grapic);
            }
            else
            {
                m_vTopGraphics.Add(grapic);
            }
            grapic.Init();
            GetHud().AddTopWidget(grapic);

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
            EditorGUI.LabelField(rowRect, widget.graphicItem.GetName());
            return true;
        }
        //--------------------------------------------------------
        public bool IsExistID(int id)
        {
            HashSet<int> vIds = new HashSet<int>();
            foreach (var db in m_vTopGraphics)
            {
                CollectIds(db, vIds);
            }
            return vIds.Contains(id);
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
        void CollectIds(AWidget grapic, HashSet<int> vIds)
        {
            vIds.Add(grapic.GetId());
            if (grapic.GetChilds() == null)
                return;
            var childs = grapic.GetChilds();
            foreach(var db in childs)
            {
                CollectIds(db, vIds);
            }
        }
        //--------------------------------------------------------
        void BuildHierarchy(AWidget comp, List<Framework.HUD.Runtime.HudObject.Hierarchy> nodes)
        {
            var node = new Framework.HUD.Runtime.HudObject.Hierarchy();
            node.id = comp.GetId();
            node.parentId = comp.GetParent() != null ? comp.GetParent().GetId() : -1;
            node.children = new List<int>();
            var childs = comp.GetChilds();
            if (childs != null)
            {
                foreach (var child in childs)
                {
                    node.children.Add(child.GetId());
                    BuildHierarchy(child, nodes);
                }
            }
            nodes.Add(node);
        }
        //--------------------------------------------------------
        Dictionary<System.Type, List<HudBaseData>> ConvertToDataList()
        {
            Dictionary<System.Type, List<HudBaseData>> vList = new Dictionary<Type, List<HudBaseData>>();
            foreach (var db in m_vTopGraphics)
            {
                ConvertToDataList(db, vList);
            }
            return vList;
        }
        //--------------------------------------------------------
        void ConvertToDataList(AWidget grapic, Dictionary<System.Type, List<HudBaseData>> vList)
        {
            if(!vList.TryGetValue(grapic.GetData().GetType(), out var vTemps))
            {
                vTemps = new List<HudBaseData>();
                vList.Add(grapic.GetData().GetType(), vTemps);
            }
            vTemps.Add(grapic.GetData());
            if (grapic.GetChilds() == null)
                return;
            var childs = grapic.GetChilds();
            foreach (var db in childs)
            {
                ConvertToDataList(db, vList);
            }
        }
    }
}
#endif
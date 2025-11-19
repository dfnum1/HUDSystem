/********************************************************************
生成日期:	11:11:2025
类    名: 	InspectorLogic
作    者:	HappLI
描    述:	HUD图元数据编辑逻辑
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Framework.HUD.Editor
{
    public class UnDoLogic : AEditorLogic
    {
        private Stack<string> m_vUndoStack = new Stack<string>();
        private Stack<string> m_vRedoStack = new Stack<string>();
        public UnDoLogic(HUDEditor editor, Rect viewRect) : base(editor, viewRect)
        {
        }
        //--------------------------------------------------------
        public override void OnEvent(Event evt)
        {
            base.OnEvent(evt);
            if(evt.type == EventType.KeyUp)
            {
                if(evt.keyCode == KeyCode.Z && evt.control)
                {
                    Undo();
                    evt.Use();
                }
                if (evt.keyCode == KeyCode.X && evt.control)
                {
                    Redo();
                    evt.Use();
                }
            }
        }
        //--------------------------------------------------------
        public void Undo()
        {
        }
        //--------------------------------------------------------
        public void Redo()
        {
        }
        //--------------------------------------------------------
        void RecordUndo(HudBaseData data)
        {
        }
    }
}
#endif
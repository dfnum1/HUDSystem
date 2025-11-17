/********************************************************************
生成日期:	11:11:2025
类    名: 	InspectorLogic
作    者:	HappLI
描    述:	HUD图元数据编辑逻辑
*********************************************************************/
#if UNITY_EDITOR
using Framework.HUD.Runtime;
using UnityEditor;
using UnityEngine;

namespace Framework.HUD.Editor
{
    public class InspectorLogic : AEditorLogic
    {
        AComponent m_pSelectComponent = null;
        public InspectorLogic(HUDEditor editor, Rect viewRect) : base(editor, viewRect)
        {
        }
        //--------------------------------------------------------
        internal override void OnSelectComponent(AComponent component)
        {
            m_pSelectComponent = component;
        }
        //--------------------------------------------------------
        protected override void OnGUI()
        {
            if (m_pSelectComponent == null)
                return;
            var hudBase = m_pSelectComponent.GetData();
            if (hudBase == null)
                return;

            float lableWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80;
            DrawBase(m_pSelectComponent, hudBase);
            if (hudBase is HudImageData)
            {
                DrawImage(m_pSelectComponent as HudImage, hudBase as HudImageData);
            }
            else if(hudBase is HudTextData)
            {
                DrawText(m_pSelectComponent as HudText, hudBase as HudTextData);
            }
            EditorGUIUtility.labelWidth = lableWidth;
        }
        //--------------------------------------------------------
        void DrawBase(AComponent pComonent, HudBaseData data)
        {
            pComonent.SetVisibleSelf(EditorGUILayout.Toggle("Visible", pComonent.IsVisibleSelf()));
            int id = EditorGUILayout.DelayedIntField("ID", data.id);
            if(id != data.id)
            {
                if (m_pEditor.GetLogic<HierarchyLogic>().IsExistID(id))
                {
                    EditorUtility.DisplayDialog("提示", "Id 已被占用", "好的");
                }
                else
                    data.id = id;
            }
            data.name = EditorGUILayout.DelayedTextField("Name", data.name);

            EditorGUI.BeginChangeCheck();
            data.position = EditorGUILayout.Vector3Field("Position", data.position);
            data.sizeDelta = EditorGUILayout.Vector2Field("Size", data.sizeDelta);
            data.angle = EditorGUILayout.Slider("Angle", data.angle, 0, 360);
            data.color = EditorGUILayout.ColorField("Color", data.color);
            data.mask = EditorGUILayout.Toggle("mask", data.mask);
            if(data.mask)
            {
                EditorGUI.indentLevel++;
                data.maskRegion = EditorGUILayout.RectField("MaskRegion", data.maskRegion);
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
            {
                GetHud().TriggerReorder();
                pComonent.SetDirty();
            }
        }
        //--------------------------------------------------------
        void DrawImage(HudImage hudImage, HudImageData data)
        {
            EditorGUI.BeginChangeCheck();
            data.sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", data.sprite, typeof(Sprite), false);
            data.imageType = (HudImageData.ImageType)EditorGUILayout.EnumPopup("ImageType", data.imageType);
            if (data.imageType == HudImageData.ImageType.Filled)
            {
                EditorGUI.indentLevel++;
                data.fillMethod = (HudImageData.FillMethod)EditorGUILayout.EnumPopup("FillMethod", data.fillMethod);
                if (data.fillMethod == HudImageData.FillMethod.Horizontal)
                {
                    data.fillOrigin = (int)(HudImageData.OriginHorizontal)EditorGUILayout.EnumPopup("Origin", (HudImageData.OriginHorizontal)data.fillOrigin);
                }
                else
                {
                    data.fillOrigin = (int)(HudImageData.OriginVertical)EditorGUILayout.EnumPopup("Origin", (HudImageData.OriginVertical)data.fillOrigin);
                }
                data.fillAmount = EditorGUILayout.Slider("Amount", data.fillAmount, 0, 1);
                EditorGUI.indentLevel--;
            }
          //  else if (data.imageType == HudImageData.ImageType.Simple)
            {
                if(GUILayout.Button("SetNativeSize"))
                {
                    var size = data.sizeDelta;
                    if(data.sprite!=null && data.sprite.rect.size != size)
                    {
                        data.sizeDelta = data.sprite.rect.size;
                        hudImage.SetDirty();
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                hudImage.SetDirty();
            }
        }
        //--------------------------------------------------------
        void DrawText(HudText hudText, HudTextData data)
        {
            EditorGUI.BeginChangeCheck();
            data.text = EditorGUILayout.DelayedTextField("Text", data.text);
            data.fontSize = EditorGUILayout.IntField("FontSize", data.fontSize);
            data.lineSpacing = EditorGUILayout.FloatField("Spacing", data.lineSpacing);
            data.alignment = (HorizontalAlignment)EditorGUILayout.EnumPopup("Alignment", data.alignment);
            if (EditorGUI.EndChangeCheck())
            {
                hudText.SetDirty();
            }
        }
    }
}
#endif
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
            EditorGUI.BeginChangeCheck();
            data.name = EditorGUILayout.DelayedTextField("Name", data.name);
            if (EditorGUI.EndChangeCheck())
            {

            }
            EditorGUI.BeginChangeCheck();
            data.position = EditorGUILayout.Vector3Field("位置", data.position);
            if(!(pComonent is HudText))
            {
                data.sizeDelta = EditorGUILayout.Vector2Field("大小", data.sizeDelta);
            }
            data.angle = EditorGUILayout.Slider("旋转角度", data.angle, 0, 360);
            data.color = EditorGUILayout.ColorField("颜色", data.color);
            data.mask = (EMaskType)EditorGUILayout.EnumPopup("蒙版", data.mask);
            if(data.mask == EMaskType.Rect)
            {
                EditorGUI.indentLevel++;
                data.maskRegion = EditorGUILayout.RectField("蒙版区域", data.maskRegion);
                EditorGUI.indentLevel--;
            }
            else if (data.mask == EMaskType.Circle)
            {
                EditorGUI.indentLevel++;
                data.maskRegion.position = EditorGUILayout.Vector2Field("偏移", data.maskRegion.position);
                float radius = EditorGUILayout.FloatField("半径", data.maskRegion.size.x);
                float fade = EditorGUILayout.Slider("过渡", data.maskRegion.size.y,0.01f, data.maskRegion.size.x*0.9f);
                data.maskRegion.size = new Vector2(radius, fade);
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
            {
                pComonent.SetDirty();
                GetHud().TriggerReorder();
            }
        }
        //--------------------------------------------------------
        void DrawImage(HudImage hudImage, HudImageData data)
        {
            EditorGUI.BeginChangeCheck();
            data.sprite = (Sprite)EditorGUILayout.ObjectField("图片", data.sprite, typeof(Sprite), false);
            data.imageType = (HudImageData.ImageType)EditorGUILayout.EnumPopup("绘制类型", data.imageType);
            if (data.imageType == HudImageData.ImageType.Filled)
            {
                EditorGUI.indentLevel++;
                data.fillMethod = (HudImageData.FillMethod)EditorGUILayout.EnumPopup("填充方式", data.fillMethod);
                if (data.fillMethod == HudImageData.FillMethod.Horizontal)
                {
                    data.fillOrigin = (int)(HudImageData.OriginHorizontal)EditorGUILayout.EnumPopup("填充方向", (HudImageData.OriginHorizontal)data.fillOrigin);
                }
                else
                {
                    data.fillOrigin = (int)(HudImageData.OriginVertical)EditorGUILayout.EnumPopup("填充方向", (HudImageData.OriginVertical)data.fillOrigin);
                }
                data.fillAmount = EditorGUILayout.Slider("填充比率", data.fillAmount, 0, 1);
                EditorGUI.indentLevel--;
            }
          //  else if (data.imageType == HudImageData.ImageType.Simple)
            {
                if(data.sprite && GUILayout.Button("设置原始大小"))
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
                GetHud().TriggerReorder();
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
                GetHud().TriggerReorder();
            }
        }
    }
}
#endif
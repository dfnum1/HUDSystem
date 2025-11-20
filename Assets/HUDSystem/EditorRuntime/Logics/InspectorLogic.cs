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
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEditor.SearchService;
using UnityEngine;
using static Framework.HUD.Runtime.HudAtlas;

namespace Framework.HUD.Editor
{
    public class InspectorLogic : AEditorLogic
    {
        AWidget m_pSelectComponent = null;
        public InspectorLogic(HUDEditor editor, Rect viewRect) : base(editor, viewRect)
        {
        }
        //--------------------------------------------------------
        internal override void OnSelectComponent(AWidget component)
        {
            m_pSelectComponent = component;
        }
        //--------------------------------------------------------
        protected override void OnGUI()
        {
            if (m_pSelectComponent == null)
            {
                if(GetHudObject()!=null)
                {
                    var hudObj = GetHudObject();
                    hudObj.allowScale = EditorGUILayout.Toggle("允许跟随缩放", hudObj.allowScale);
                    hudObj.allowRotation = EditorGUILayout.Toggle("允许跟随旋转", hudObj.allowRotation);
                    hudObj.center = EditorGUILayout.Vector2Field("包围盒中心点", hudObj.center);
                    hudObj.size = EditorGUILayout.Vector2Field("Hud包围盒大小", hudObj.size);
                    if(GUILayout.Button("自动适配包围盒大小"))
                    {
                        AutoBoundSize();
                    }
                }
                return;
            }
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
            else if (hudBase is HudNumberData)
            {
                DrawNumber(m_pSelectComponent as HudNumber, hudBase as HudNumberData);
            }
            else if (hudBase is HudParticleData)
            {
                DrawParticle(m_pSelectComponent as HudParticle, hudBase as HudParticleData);
            }
            EditorGUIUtility.labelWidth = lableWidth;
        }
        //--------------------------------------------------------
        void AutoBoundSize()
        {
            // 初始化 min/max
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            var widgets = GetHud().GetWidgets();
            foreach (var data in widgets)
            {
                Vector3 pos = data.Value.GetPosition() / HUDUtils.PIXEL_SIZE; // 换算到世界单位
                Vector2 size = data.Value.GetSize() / HUDUtils.PIXEL_SIZE;    // 换算到世界单位

                // 左下角和右上角
                Vector3 pMin = new Vector3(pos.x - size.x * 0.5f, pos.y - size.y * 0.5f, pos.z);
                Vector3 pMax = new Vector3(pos.x + size.x * 0.5f, pos.y + size.y * 0.5f, pos.z);

                min = Vector3.Min(min, pMin);
                max = Vector3.Max(max, pMax);
            }

            // 计算中心和大小（世界单位）
            Vector2 newCenter = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            Vector2 newSize = new Vector2(max.x - min.x, max.y - min.y);

            // 存储时还原为“像素单位”
            GetHudObject().center = newCenter * HUDUtils.PIXEL_SIZE;
            GetHudObject().size = newSize * HUDUtils.PIXEL_SIZE;
        }
        //--------------------------------------------------------
        void DrawBase(AWidget pComonent, HudBaseData data)
        {
            pComonent.SetVisibleSelf(EditorGUILayout.Toggle("Visible", pComonent.IsVisibleSelf()));
            int id = EditorGUILayout.DelayedIntField("唯一Id", data.id);
            if(id != data.id)
            {
                if (m_pEditor.GetLogic<HierarchyLogic>().IsExistID(id))
                {
                    EditorUtility.DisplayDialog("提示", "Id 已被占用", "好的");
                }
                else
                {
                    pComonent.SetId(id);
                }
            }
            EditorGUI.BeginChangeCheck();
            data.name = EditorGUILayout.DelayedTextField("名称", data.name);
            data.rayTest = EditorGUILayout.Toggle("可点击", data.rayTest);
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
                pComonent.SyncData();
                GetHud().TriggerReorder();
            }
        }
        //--------------------------------------------------------
        void DrawImage(HudImage hudImage, HudImageData data)
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("图片资源");
                GUILayout.BeginHorizontal();

                // 精确绘制Sprite区域
                Rect previewRect = GUILayoutUtility.GetRect(96, 96, GUILayout.Width(96), GUILayout.Height(96));
                if (data.sprite != null)
                {
                    Texture2D tex = data.sprite.texture;
                    Rect texRect = data.sprite.textureRect;
                    // 归一化UV
                    Rect uv = new Rect(
                        texRect.x / tex.width,
                        texRect.y / tex.height,
                        texRect.width / tex.width,
                        texRect.height / tex.height
                    );
                    GUI.DrawTextureWithTexCoords(previewRect, tex, uv);

                    // 点击弹窗
                    if (Event.current.type == EventType.MouseDown && previewRect.Contains(Event.current.mousePosition))
                    {
                        var atlas = hudImage.GetHudAtlas();
                        var provider = ScriptableObject.CreateInstance<HudAtlasSpriteSearchProvider>();
                        provider.atlas = atlas;
                        provider.onSelect = (info) =>
                        {
                            if (data.sprite != info)
                            {
                                data.sprite = info;
                                if (data.sprite.border.SqrMagnitude() > 0)
                                {
                                    data.imageType = HudImageData.ImageType.Sliced;
                                }
                                hudImage.SyncData();
                                GetHud().TriggerReorder();
                            }
                        };
                        SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
                        Event.current.Use();
                    }
                }
                else
                {
                    if (GUI.Button(previewRect, "None"))
                    {
                        var atlas = hudImage.GetHudAtlas();
                        var provider = ScriptableObject.CreateInstance<HudAtlasSpriteSearchProvider>();
                        provider.atlas = atlas;
                        provider.onSelect = (info) =>
                        {
                            if (data.sprite != info)
                            {
                                data.sprite = info;
                                if (data.sprite.border.SqrMagnitude() > 0)
                                {
                                    data.imageType = HudImageData.ImageType.Sliced;
                                }
                                hudImage.SyncData();
                                GetHud().TriggerReorder();
                            }
                        };
                        SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
                    }
                }

                GUILayout.BeginVertical();
                GUILayout.Label(data.sprite ? data.sprite.name : "None", EditorStyles.miniLabel);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    var atlas = hudImage.GetHudAtlas();
                    var provider = ScriptableObject.CreateInstance<HudAtlasSpriteSearchProvider>();
                    provider.atlas = atlas;
                    provider.onSelect = (info) =>
                    {
                        if (data.sprite != info)
                        {
                            data.sprite = info;
                            if(data.sprite.border.SqrMagnitude()>0)
                            {
                                data.imageType = HudImageData.ImageType.Sliced;
                            }
                            hudImage.SyncData();
                            GetHud().TriggerReorder();
                        }
                    };
                    SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), provider);
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndHorizontal();
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
                        hudImage.SyncData();
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                hudImage.SyncData();
                GetHud().TriggerReorder();
            }
        }
        //--------------------------------------------------------
        void DrawText(HudText hudText, HudTextData data)
        {
            EditorGUI.BeginChangeCheck();
            data.text = EditorGUILayout.DelayedTextField("文本内容", data.text);
            data.fontSize = EditorGUILayout.FloatField("字体大小", data.fontSize);
            data.lineSpacing = EditorGUILayout.FloatField("间距", data.lineSpacing);
            data.alignment = (HorizontalAlignment)EditorGUILayout.EnumPopup("对齐方式", data.alignment);
            if (EditorGUI.EndChangeCheck())
            {
                hudText.SyncData();
                GetHud().TriggerReorder();
            }
        }
        //--------------------------------------------------------
        void DrawNumber(HudNumber hudText, HudNumberData data)
        {
            EditorGUI.BeginChangeCheck();
            data.strNumber = EditorGUILayout.DelayedTextField("数字内容", data.strNumber);
            data.fontSize = EditorGUILayout.FloatField("字体大小", data.fontSize);
            data.alignment = (HorizontalAlignment)EditorGUILayout.EnumPopup("对齐方式", data.alignment);
            if (EditorGUI.EndChangeCheck())
            {
                hudText.SyncData();
                GetHud().TriggerReorder();
            }
        }
        //--------------------------------------------------------
        void DrawParticle(HudParticle hudText, HudParticleData data)
        {
            EditorGUI.BeginChangeCheck();
            data.strParticle = EditorGUILayout.DelayedTextField("特效资源", data.strParticle);
            data.renderOrder = EditorGUILayout.IntField("渲染层级", data.renderOrder);
            data.scale = EditorGUILayout.Vector3Field("缩放", data.scale);
            if (EditorGUI.EndChangeCheck())
            {
                hudText.SyncData();
                GetHud().TriggerReorder();
            }
        }
    }
}
#endif
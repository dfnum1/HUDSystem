/********************************************************************
生成日期:	11:11:2025
类    名: 	HudCanvasRender
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GridBrushBase;

namespace Framework.HUD.Runtime
{
    public class HudCanvasRender
    {
        Mesh m_pMesh;
        Material m_pMaterial;
        HudAtlas m_pAtlasMapping;
        TMP_FontAsset m_pFontAsset;

        private HudRenderBatch m_pRenderBatch;
        public HudRenderBatch renderBatch
        {
            get { return m_pRenderBatch; }
        }
        //-----------------------------------------------------
    }
}

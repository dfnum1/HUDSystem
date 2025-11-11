/********************************************************************
生成日期:	11:11:2025
类    名: 	MeshInfo
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    public class MeshInfo
    {
        public int quad_count { get; private set; }

        public Vector3[] poices;
        public Vector2[] uv0;
        public Vector2[] uv1;
        public Color32[] colors;
        public int[] indices;

        public MeshInfo(int quad_count)
        {
            this.quad_count = quad_count;
            int size = quad_count * 4;
            poices = new Vector3[size];
            uv0 = new Vector2[size];
            uv1 = new Vector2[size];
            colors = new Color32[size];
            indices = new int[quad_count * 6];
        }

        public void Resize(int new_quad_count)
        {
            if (this.quad_count == new_quad_count)
                return;
            this.quad_count = new_quad_count;

            int new_size = quad_count * 4;
            System.Array.Resize(ref poices, new_size);
            System.Array.Resize(ref uv0, new_size);
            System.Array.Resize(ref uv1, new_size);
            System.Array.Resize(ref colors, new_size);
            System.Array.Resize(ref indices, quad_count * 6);
        }
    }
}

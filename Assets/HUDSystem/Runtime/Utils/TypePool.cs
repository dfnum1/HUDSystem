/********************************************************************
生成日期:	11:11:2025
类    名: 	HUDSystem
作    者:	HappLI
描    述:	HUD 系统
*********************************************************************/
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Framework.HUD.Runtime
{
    public abstract class TypeObject
    {
        internal abstract void Destroy();
    }
    //--------------------------------------------------------
    //! 类型对象池
    //--------------------------------------------------------
    internal static class TypePool
    {
        const int POOL_COUNT = 128;
        static Dictionary<System.IntPtr, Stack<TypeObject>> ms_vPools = new Dictionary<System.IntPtr, Stack<TypeObject>>(16);
        //--------------------------------------------------------
        internal static T Malloc<T>() where T : TypeObject, new()
        {
            System.IntPtr handle = typeof(T).TypeHandle.Value;
            if (ms_vPools.TryGetValue(handle, out var pool) && pool.Count > 0)
            {
                var user = pool.Pop();
                return user as T;
            }
            T newT = new T();
            return newT;
        }
        //--------------------------------------------------------
        internal static T MallocWidget<T>(HudSystem pSystem, HudBaseData pData) where T : AWidget, new()
        {
            System.IntPtr handle = typeof(T).TypeHandle.Value;
            if (ms_vPools.TryGetValue(handle, out var pool) && pool.Count > 0)
            {
                var user = pool.Pop();
                T pWidget = user as T;
                pWidget.SetHudSystem(pSystem);
                pWidget.SetHudData(pData);
                return pWidget;
            }
            T newT = new T();
            newT.SetHudSystem(pSystem);
            newT.SetHudData(pData);
            return newT;
        }
        //--------------------------------------------------------
        public static void Free(this TypeObject pObj)
        {
            System.IntPtr handle = pObj.GetType().TypeHandle.Value;
            if (!ms_vPools.TryGetValue(handle, out var pool))
            {
                pool = new Stack<TypeObject>(POOL_COUNT);
                ms_vPools[handle] = pool;
            }
            if (pool.Count < POOL_COUNT) pool.Push(pObj);
        }
    }
}

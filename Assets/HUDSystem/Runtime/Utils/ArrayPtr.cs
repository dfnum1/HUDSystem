using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
namespace Framework.HUD.Runtime
{
    public unsafe class ArrayPtr<T, U> : IDisposable
        where T : unmanaged
        where U : unmanaged
    {
        public List<U[]> arrays = new List<U[]>();
        public List<ulong> gchandles = new List<ulong>();
        public NativeList<IntPtr> arrayPtrs = new NativeList<IntPtr>(4, Allocator.Persistent);
        //-----------------------------------------------------
        public int capacity
        {
            get
            {
                int curCapacity = 0;
                for (int i = 0; i < arrays.Count; i++)
                {
                    curCapacity += arrays[i].Length;
                }
                return curCapacity;
            }
        }
        //-----------------------------------------------------
        public ArrayPtr(int _capacity)
        {
            Resize(_capacity);
        }
        //-----------------------------------------------------
        public void Resize(int _capacity)
        {
            if (capacity >= _capacity) return;
            int remainder = _capacity % HUDUtils.batchMaxCount;
            int partCount = _capacity / HUDUtils.batchMaxCount;
            partCount = remainder == 0 ? partCount : (partCount + 1);
            int addCount = partCount - arrays.Count;
            for (int i = 0; i < addCount; i++)
            {
                U[] array = new U[HUDUtils.batchMaxCount];
                arrays.Insert(0, array);
            }
            gchandles.Clear();
            arrayPtrs.Clear();
            for (int i = 0; i < arrays.Count; i++)
            {
                ulong gchandle = 0;
                void* ptr = UnsafeUtility.PinGCArrayAndGetDataAddress(arrays[i], out gchandle);
                gchandles.Add(gchandle);
                arrayPtrs.Add(new IntPtr(ptr));
            }
        }
        //-----------------------------------------------------
        public void Dispose()
        {
            arrayPtrs.Clear();
            arrayPtrs.Dispose();
            for (int i = 0; i < gchandles.Count; i++)
            {
                UnsafeUtility.ReleaseGCObject(gchandles[i]);
            }
            gchandles.Clear();
        }
    }
}
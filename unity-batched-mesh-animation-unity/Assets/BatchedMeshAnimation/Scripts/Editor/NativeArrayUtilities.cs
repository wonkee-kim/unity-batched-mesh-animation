using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Mathematics;

public static class NativeArrayUtilities
{
    // https://gist.github.com/LotteMakesStuff/c2f9b764b15f74d14c00ceb4214356b4
    public static unsafe NativeArray<float3> GetNativeArrays(Vector3[] array, Allocator allocator, NativeArrayOptions nativeArrayOptions)
    {
        NativeArray<float3> nativeArray = new NativeArray<float3>(array.Length, allocator, nativeArrayOptions);

        fixed (void* arrayBufferPointer = array)
        {
            UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(nativeArray),
                arrayBufferPointer, array.Length * (long)UnsafeUtility.SizeOf<float3>());
        }

        return nativeArray;
    }

    public static unsafe NativeArray<float2> GetNativeArrays(Vector2[] array, Allocator allocator, NativeArrayOptions nativeArrayOptions)
    {
        NativeArray<float2> nativeArray = new NativeArray<float2>(array.Length, allocator, nativeArrayOptions);

        fixed (void* arrayBufferPointer = array)
        {
            UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(nativeArray),
                arrayBufferPointer, array.Length * (long)UnsafeUtility.SizeOf<float2>());
        }

        return nativeArray;
    }

    public static unsafe NativeArray<int> GetNativeArrays(int[] array, Allocator allocator, NativeArrayOptions nativeArrayOptions)
    {
        NativeArray<int> nativeArray = new NativeArray<int>(array.Length, allocator, nativeArrayOptions);

        fixed (void* arrayBufferPointer = array)
        {
            UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(nativeArray),
                arrayBufferPointer, array.Length * (long)UnsafeUtility.SizeOf<int>());
        }

        return nativeArray;
    }

    public static unsafe NativeArray<BoneWeight> GetNativeArrays(BoneWeight[] array, Allocator allocator, NativeArrayOptions nativeArrayOptions)
    {
        NativeArray<BoneWeight> nativeArray = new NativeArray<BoneWeight>(array.Length, allocator, nativeArrayOptions);

        fixed (void* arrayBufferPointer = array)
        {
            UnsafeUtility.MemCpy(NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(nativeArray),
                arrayBufferPointer, array.Length * (long)UnsafeUtility.SizeOf<BoneWeight>());
        }

        return nativeArray;
    }

    public static unsafe void GetArrayFromNativeArray(Vector3[] vertexArray, NativeArray<float3> vertexBuffer)
    {
        fixed (void* vertexArrayPointer = vertexArray)
        {
            UnsafeUtility.MemCpy(vertexArrayPointer, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(vertexBuffer), vertexArray.Length * (long)UnsafeUtility.SizeOf<float3>());
        }
    }

    public static unsafe void GetArrayFromNativeArray(Vector2[] array, NativeArray<float2> arrayBuffer)
    {
        fixed (void* vertexArrayPointer = array)
        {
            UnsafeUtility.MemCpy(vertexArrayPointer, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(arrayBuffer), array.Length * (long)UnsafeUtility.SizeOf<float2>());
        }
    }

    public static unsafe void GetArrayFromNativeArray(int[] array, NativeArray<int> arrayBuffer)
    {
        fixed (void* vertexArrayPointer = array)
        {
            UnsafeUtility.MemCpy(vertexArrayPointer, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(arrayBuffer), array.Length * (long)UnsafeUtility.SizeOf<int>());
        }
    }

    public static unsafe void GetArrayFromNativeArray(Color32[] array, NativeArray<Color32> arrayBuffer)
    {
        fixed (void* vertexArrayPointer = array)
        {
            UnsafeUtility.MemCpy(vertexArrayPointer, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(arrayBuffer), array.Length * (long)UnsafeUtility.SizeOf<Color32>());
        }
    }

}

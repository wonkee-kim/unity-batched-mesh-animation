using System.IO;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Utilities
{
    // https://docs.unity3d.com/Packages/com.unity.shadergraph@12.1/manual/Colorspace-Conversion-Node.html
    public static float3 Unity_ColorspaceConversion_RGB_HSV(float3 c)
    {
        float4 K = float4(0.0f, -1.0f / 3.0f, 2.0f / 3.0f, -1.0f);
        float4 P = lerp(float4(c.zy, K.wz), float4(c.yz, K.xy), step(c.z, c.y));
        float4 Q = lerp(float4(P.xyw, c.x), float4(c.x, P.yzx), step(P.x, c.x));
        float D = Q.x - min(Q.w, Q.y);
        float E = 1e-10f;
        return float3(abs(Q.z + (Q.w - Q.y) / (6.0f * D + E)), D / (Q.x + E), Q.x);
    }

    public static float3 Unity_ColorspaceConversion_HSV_RGB(float3 c)
    {
        c.yz = saturate(c.yz);
        float4 K = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
        float3 P = abs(frac(c.xxx + K.xyz) * 6.0f - K.www);
        return c.z * lerp(K.xxx, saturate(P - K.xxx), c.y);
    }

    public static float3 Unity_ColorspaceConversion_HSV_Linear(float3 c)
    {
        float3 RGB = Unity_ColorspaceConversion_HSV_RGB(c);
        float3 linearRGBLo = RGB / 12.92f;
        float3 linearRGBHi = pow(max(abs((RGB + 0.055f) / 1.055f), 1.192092896e-07f), float3(2.4f, 2.4f, 2.4f));
        bool lessThan = (RGB.x + RGB.y + RGB.z) * 0.333f <= 0.04045f;
        return (lessThan) ? linearRGBLo : linearRGBHi;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Store mesh data to asset database.
    /// </summary>
    public static void StoreMesh(Mesh mesh, string filePath, string filename = null)
    {
        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);

        if (filename == null)
        {
            if (!string.IsNullOrEmpty(mesh.name))
            {
                filename = mesh.name;
            }
            else
            {
                filename = "untitled";
            }
        }
        AssetDatabase.CreateAsset(mesh, filePath + filename + ".mesh");
    }
#endif
}

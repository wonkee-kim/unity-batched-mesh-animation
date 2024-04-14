# Batched Mesh Animation
An optimal way of placing a large amount of animated objects in the background.<br>
Video shows 10,000 vehicles running in 60 fps on WebGL.

https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/980443d7-71a3-4c91-b780-9b4725b04c61

### Demo (Available on Web, Mobile and MetaQuest - Powered by [Spatial Creator Toolkit](https://www.spatial.io/toolkit))
https://www.spatial.io/s/Batched-Mesh-Animation-661c58eb49e39492db5a86f2


## Breakdown
One batched mesh contains 100 vehicles, and each vehicle has 50 vertices, totaling 5,000 vertices in each batched mesh.
To draw 10,000 vehicles, there are 100 draw calls and 500,000 vertices in total.

### Vehicle 3D model
Mesh has been simplified for efficiency, given its small size in the background.<br>
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/2c1ee5b4-d6fa-4c6f-953a-3d87e53c6d08" height="160">
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/ba70fbd2-33b6-406e-9334-7355984b816e" height="160">
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/265832c0-060b-4bcd-9875-68925c85bb46" height="160">

### Vertex attributes in the vehicle model
Each part is identified using UV coordinates.
- Headlight (uv.x < 0.5 && uv.y > 0.5)<br>
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/2e067f34-9dfa-40c2-9db8-456fa9028f67" width="480">
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/b3e1ea8e-25e3-4ed5-ac39-90381d07585f" width="316">

- Backlight (uv.x > 0.5 && uv.y > 0.5)<br>
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/bb58705f-d8df-4477-a098-8e15ca122215" width="480">
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/5d80196e-d6bb-4080-9743-f726ab3014fc" width="298">

- Glass (uv.x < 0.5 && uv.y < 0.5)<br>
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/365b19bd-a772-49dd-b35f-45684226c059" width="480">

- Body (uv.x > 0.5 && uv.y < 0.5)<br>
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/7f493182-8dc8-4e1e-8fbe-52f517540ad1" width="480">


### Create batched mesh using script
[BatchedMeshGenerator.cs](https://github.com/wonkee-kim/unity-batched-mesh-animation/blob/main/unity-batched-mesh-animation-unity/Assets/BatchedMeshAnimation/Scripts/Editor/BatchedMeshGenerator.cs)

1. Copy mesh data into NativeArrays<br>
[BatchedMeshGenerator.cs#L52-L56](https://github.com/wonkee-kim/unity-batched-mesh-animation/blob/main/unity-batched-mesh-animation-unity/Assets/BatchedMeshAnimation/Scripts/Editor/BatchedMeshGenerator.cs#L52-L56)

```C#
// Copy vehicle mesh to NativeArray
NativeArray<float3> vehicleVertices = NativeArrayUtilities.GetNativeArrays(_vehicleMesh.vertices, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
NativeArray<float3> vehicleNormals = NativeArrayUtilities.GetNativeArrays(_vehicleMesh.normals, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
NativeArray<float2> vehicleUVs = NativeArrayUtilities.GetNativeArrays(_vehicleMesh.uv, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
NativeArray<int> vehicleIndices = NativeArrayUtilities.GetNativeArrays(_vehicleMesh.GetIndices(0), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
```

2. Create another NativeArrays that will contain 100 vehicle mesh<br>
[BatchedMeshGenerator.cs#L58-L62](https://github.com/wonkee-kim/unity-batched-mesh-animation/blob/main/unity-batched-mesh-animation-unity/Assets/BatchedMeshAnimation/Scripts/Editor/BatchedMeshGenerator.cs#L58-L62)

```C#
// Mesh data
int vertexCount = _vehicleCount * vehicleVertices.Length;
int indexCount = _vehicleCount * vehicleIndices.Length;
var vertexBuffer = new NativeArray<Vertex>(vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
var indexBuffer = new NativeArray<int>(indexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
```

3. Run jobs that copy vehicle mesh data and add more vertex attributes<br>
[BatchedMeshGenerator.cs#L178-L204](https://github.com/wonkee-kim/unity-batched-mesh-animation/blob/main/unity-batched-mesh-animation-unity/Assets/BatchedMeshAnimation/Scripts/Editor/BatchedMeshGenerator.cs#L178-L204)

It generates a random color for each instance and puts it in the vertex color.
It also creates three random values that will be used for random positioning and puts them into uv2.xy and uv3.x.
Lastly, it adds the instance ID into uv3.y.
```C#
public void Execute(int particleIndex)
{
    Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)particleIndex * 3498 + 1);

    float3 hsv = new float3(
        _colorReference.x + random.NextFloat(-_colorParameters.x, _colorParameters.x) * 360f,
        _colorReference.y + random.NextFloat(-_colorParameters.y, _colorParameters.y),
        _colorReference.z + random.NextFloat(-_colorParameters.z, _colorParameters.z));
    float3 color = Utilities.Unity_ColorspaceConversion_HSV_Linear(hsv);
    float2 uv2 = new float2(random.NextFloat(), random.NextFloat()); // random(x,y)
    float2 uv3 = new float2(random.NextFloat(), particleIndex); // random(z), id

    int currentVertexIndex = _vertexCountPerParticle * particleIndex;
    for (int i = 0; i < _vertexCountPerParticle; i++)
    {
        Vertex vertexBuffer = new Vertex();
        vertexBuffer.position = _sourceVertices[i];
        vertexBuffer.normal = _sourceNormals[i];
        vertexBuffer.color = color;
        vertexBuffer.texCoord0 = _sourceUVs[i];
        vertexBuffer.texCoord1 = uv2;
        vertexBuffer.texCoord2 = uv3;

        int index = currentVertexIndex + i;
        _vertexBuffer[index] = vertexBuffer;
    }
}
```

4. Generated batched mesh<br>
Generated mesh has 5,000 vertices and each vertex contains position, normal, color, uv0, uv1 and uv2. (293 KB)
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/7f51df11-1ae8-4257-84ea-2f6bd4f6d49c" width="480">



### Shader

#### Animation
[BGVehiclesMeshAnimator.hlsl](https://github.com/wonkee-kim/unity-batched-mesh-animation/blob/main/unity-batched-mesh-animation-unity/Assets/BatchedMeshAnimation/Vehicles/BGVehiclesMeshAnimator.hlsl)

Utilize object position to ensure unique random values for each object.<br>
uv1.x, uv1.y and uv2.x has different random values range between 0 and 1 and apply position random by applying random range.
```hlsl
float3 random = float3(uv1.x, uv1.y, uv2.x) + dot(objectPosition, float3(4567.89, 1234.34, 6832.72));
random = frac(random);
output = positionOS + random * randomRange;
```

Animate the z position using `_Time` and `frac` to loop between `-halfRange` and `halfRange`.
```hlsl
float vehicleInterval = 1.0 / vehicleCount;
float z = uv2.y * vehicleInterval; // 0~1
// range animSpeed.x ~ animSpeed.y
float randomSpeed = frac(dot(random, float3(123.45, 234.56, 345.67)));
float vehicleSpeed = randomSpeed * (animSpeed.y - animSpeed.x) + animSpeed.x;;
vehicleSpeed = vehicleSpeed / 3.6; // km/h to m/s
z = frac(z + _Time.y / animateRange * vehicleSpeed); // animated, 0~1
z = -animateRange * 0.5 + z * animateRange; // -animateRange/2 ~ animateRange/2
output.z += z;
```

#### Surface Data
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/2e067f34-9dfa-40c2-9db8-456fa9028f67" width="480"><br>
The UV coordinates uv.x and uv.y are used to mask each part of mesh, allowing for the application of different surface data such as color, brightness, metallic and smoothness.

[BGVehiclesShader.shader#L129-L141](https://github.com/wonkee-kim/unity-batched-mesh-animation/blob/main/unity-batched-mesh-animation-unity/Assets/BatchedMeshAnimation/Vehicles/BGVehiclesShader.shader#L129-L141)
****
```hlsl
 half isBodyOrBackLight = step(0.5, IN.uv0.x);
 half isLight = step(0.5, IN.uv0.y);

 half3 diffuse = dot(normalWS, half3(0, 1, 0)) * 0.5 + 0.5; // half lambert from top
 half3 ambient = SampleSH(OUT.normalWS) * _AmbientIntensity;
 half3 emission = lerp(_ColorFrontLight, _ColorBackLight, isBodyOrBackLight) * isLight * _Brightness;
 half3 color = lerp(_ColorGlass.rgb, IN.color * _BaseColor.rgb, isBodyOrBackLight);
 color *= diffuse;
 color += ambient + emission;
 OUT.surfaceColor = half4(color, 1);

 half metallic = lerp(_MetallicGlass, _Metallic, saturate(isBodyOrBackLight + isLight));
 half smoothness = lerp(_SmoothnessGlass, _Smoothness, isBodyOrBackLight);
```

## Result
Drawing 10,000 animated vehicles costs the same as drawing 100 objects that have 5,000 vertices each.

This method is applied to a cross-platform game [Neon Ghost](https://www.spatial.io/s/Neon-Ghost-65e2209a07789d42d8a8c56c) on [Spatial](https://www.spatial.io/) and it runs smooth with the number of vehicles in the background.
The game is available on web, mobile and Quest platforms.
<img src="https://github.com/wonkee-kim/unity-batched-mesh-animation/assets/830808/c4a85172-1148-45aa-a701-48b6f8728ad6"><br>

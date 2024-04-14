#ifndef BG_VEHICLES_ANIMATOR_INCLUDED
#define BG_VEHICLES_ANIMATOR_INCLUDED

#define ANIMATE_VEHICLE_VERTEX_OUTPUT(positionOS, uv1, uv2, randomRange, animateRange, animSpeed, vehicleCount, objectPosition, output) AnimateVehicleVertex(positionOS, uv1, uv2, randomRange, animateRange, animSpeed, vehicleCount, objectPosition, output);

// uv1(x,y) and uv2(x): random value (0~1)
void AnimateVehicleVertex(float3 positionOS, float2 uv1, float2 uv2, float3 randomRange, float animateRange, float2 animSpeed, float vehicleCount, float3 objectPosition, out float3 output)
{
    #if SHADERGRAPH_PREVIEW
        output = float3(0.5, 0.5, 0.5);
    #else
        // Use object position to make the random value unique per object
        float3 random = float3(uv1.x, uv1.y, uv2.x) + dot(objectPosition, float3(4567.89, 1234.34, 6832.72));
        random = frac(random);
        output = positionOS + random * randomRange;
        float vehicleInterval = 1.0 / vehicleCount;
        float z = uv2.y * vehicleInterval; // 0~1
        // range animSpeed.x ~ animSpeed.y
        float randomSpeed = frac(dot(random, float3(123.45, 234.56, 345.67)));
        float vehicleSpeed = randomSpeed * (animSpeed.y - animSpeed.x) + animSpeed.x;;
        vehicleSpeed = vehicleSpeed / 3.6; // km/h to m/s
        z = frac(z + _Time.y / animateRange * vehicleSpeed); // animated, 0~1
        z = -animateRange * 0.5 + z * animateRange; // -animateRange/2 ~ animateRange/2
        output.z += z;
    #endif
}

// ShaderGraph
void AnimateVehicleVertex_float(float3 positionOS, float2 uv1, float2 uv2, float3 randomRange, float animateRange, float2 animSpeed, float vehicleCount, float3 objectPosition, out float3 output)
{
    AnimateVehicleVertex(positionOS, uv1, uv2, randomRange, animateRange, animSpeed, vehicleCount, objectPosition, output);
}
void AnimateVehicleVertex_half(float3 positionOS, half2 uv1, half2 uv2, float3 randomRange, float animateRange, half2 animSpeed, float vehicleCount, float3 objectPosition, out float3 output)
{
    AnimateVehicleVertex(positionOS, uv1, uv2, randomRange, animateRange, animSpeed, vehicleCount, objectPosition, output);
}
#endif // BG_VEHICLES_ANIMATOR_INCLUDED
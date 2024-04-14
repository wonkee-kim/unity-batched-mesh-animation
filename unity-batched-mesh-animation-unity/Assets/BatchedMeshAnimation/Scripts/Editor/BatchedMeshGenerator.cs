using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BatchedMeshAnimation
{
    public class BatchedMeshGenerator : MonoBehaviour
    {
        public struct Vertex
        {
            public float3 position;
            public float3 normal;
            public float3 color;
            public float2 texCoord0;
            public float2 texCoord1;
            public float2 texCoord2;
        }

        [Header("Vehicle")]
        [SerializeField] private Mesh _vehicleMesh;
        [SerializeField, Range(1, 1000)] private int _vehicleCount = 100;
        [SerializeField, Min(1)] private uint _randomSeed = 1;

        [Header("Color")]
        [SerializeField] private Color _color = new Color(80, 85, 99, 255) / 255f;
        [SerializeField, Range(0f, 1f)] private float _colorHueRange = 1.0f;
        [SerializeField, Range(0f, 1f)] private float _colorSaturationRange = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _colorBrightnessRange = 0.5f;

        [Header("Build")]
        [SerializeField] private Transform _rootTransform;
        [SerializeField] private GameObject _vehicleMeshObject;
        [SerializeField] private Mesh _meshGenerated;
        [SerializeField] private Shader _shader;
        [SerializeField] private Material _material;
        [SerializeField] private string _filePath = "Assets/Cyberpunk/Scripts/BGVehicles/";

        public void GenerateVehicleMesh()
        {
            // Copy vehicle mesh to NativeArray
            NativeArray<float3> vehicleVertices = NativeArrayUtilities.GetNativeArrays(_vehicleMesh.vertices, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<float3> vehicleNormals = NativeArrayUtilities.GetNativeArrays(_vehicleMesh.normals, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<float2> vehicleUVs = NativeArrayUtilities.GetNativeArrays(_vehicleMesh.uv, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> vehicleIndices = NativeArrayUtilities.GetNativeArrays(_vehicleMesh.GetIndices(0), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            // Mesh data
            int vertexCount = _vehicleCount * vehicleVertices.Length;
            int indexCount = _vehicleCount * vehicleIndices.Length;
            var vertexBuffer = new NativeArray<Vertex>(vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var indexBuffer = new NativeArray<int>(indexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);


            // CreateParticleVerticesJob
            JobHandle verticesJobHandle =
                new CreateParticleVerticesJob(
                    sourceVertices: vehicleVertices,
                    sourceNormals: vehicleNormals,
                    sourceUVs: vehicleUVs,
                    vertexBuffer: vertexBuffer,
                    vertexCountPerParticle: vehicleVertices.Length,
                    colorReference: Utilities.Unity_ColorspaceConversion_RGB_HSV(new float3(_color.r, _color.g, _color.b)),
                    colorParameters: new float3(_colorHueRange, _colorSaturationRange, _colorBrightnessRange)
                    ).ScheduleParallel(_vehicleCount, innerloopBatchCount: 16, new JobHandle());
            verticesJobHandle.Complete();

            // CreateParticleIndicesJob
            JobHandle indicesJobHandle =
                new CreateParticleIndicesJob(
                    sourceIndices: vehicleIndices,
                    indexBuffer: indexBuffer,
                    indexCountPerParticleMesh: vehicleIndices.Length,
                    vertexCountPerParticleMesh: vehicleVertices.Length
                    ).ScheduleParallel(_vehicleCount, innerloopBatchCount: 16, new JobHandle());
            indicesJobHandle.Complete();


            // Build mesh
            // Bounds should match to shader property (z range)
            Mesh mesh = new Mesh { name = "BGVehicles_mesh", bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f, 1000f)) };

            // Set VertexBuffer
            mesh.SetVertexBufferParams(
                vertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 3),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, dimension: 3),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, dimension: 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, dimension: 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, dimension: 2),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, dimension: 2)
            );
            mesh.SetVertexBufferData(vertexBuffer, 0, 0, vertexCount, 0, MeshUpdateFlags.DontRecalculateBounds);

            // Set IndexBuffer
            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
            mesh.SetIndexBufferData(indexBuffer, 0, 0, indexCount);

            // Set SubMesh
            mesh.SetSubMesh(index: 0, new SubMeshDescriptor(indexStart: 0, indexCount, MeshTopology.Triangles));

            // Store mesh data to AssetDatabase
            _meshGenerated = mesh;
#if UNITY_EDITOR
            Utilities.StoreMesh(_meshGenerated, _filePath);
#endif

            // Set GameObject
            if (_vehicleMeshObject == null)
            {
                _vehicleMeshObject = new GameObject() { name = "BGVehicles_mesh (0)" };
                _vehicleMeshObject.transform.parent = _rootTransform;
                _vehicleMeshObject.layer = this.gameObject.layer;
                if (_material == null)
                {
                    _material = new Material(_shader);
                }
                _vehicleMeshObject.AddComponent<MeshFilter>().sharedMesh = _meshGenerated;
                _vehicleMeshObject.AddComponent<MeshRenderer>().material = _material;
            }
            else
            {
                _vehicleMeshObject.GetComponent<MeshFilter>().sharedMesh = _meshGenerated;
            }

            // Dispose
            vertexBuffer.Dispose();
            indexBuffer.Dispose();

            vehicleVertices.Dispose();
            vehicleNormals.Dispose();
            vehicleUVs.Dispose();
            vehicleIndices.Dispose();
        }

        [BurstCompile]
        public struct CreateParticleVerticesJob : IJobFor
        {
            [ReadOnly] NativeArray<float3> _sourceVertices;
            [ReadOnly] NativeArray<float3> _sourceNormals;
            [ReadOnly] NativeArray<float2> _sourceUVs;

            [NativeDisableParallelForRestriction]
            [WriteOnly] NativeArray<Vertex> _vertexBuffer;

            int _vertexCountPerParticle;
            [ReadOnly] float3 _colorReference; // HSV
            [ReadOnly] float3 _colorParameters;

            public CreateParticleVerticesJob(
                NativeArray<float3> sourceVertices,
                NativeArray<float3> sourceNormals,
                NativeArray<float2> sourceUVs,
                NativeArray<Vertex> vertexBuffer,
                int vertexCountPerParticle,
                float3 colorReference,
                float3 colorParameters)
            {
                _sourceVertices = sourceVertices;
                _sourceNormals = sourceNormals;
                _sourceUVs = sourceUVs;
                _vertexBuffer = vertexBuffer;
                _vertexCountPerParticle = vertexCountPerParticle;
                _colorReference = colorReference;
                _colorParameters = colorParameters;
            }

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
        } // CreateParticleVerticesJob

        [BurstCompile]
        public struct CreateParticleIndicesJob : IJobFor
        {
            [ReadOnly] NativeArray<int> _sourceIndices;
            [NativeDisableParallelForRestriction]
            [WriteOnly] NativeArray<int> _indexBuffer;
            int _indexCountPerParticleMesh;
            int _vertexCountPerParticleMesh;

            public CreateParticleIndicesJob(
                NativeArray<int> sourceIndices,
                NativeArray<int> indexBuffer,
                int indexCountPerParticleMesh,
                int vertexCountPerParticleMesh)
            {
                _sourceIndices = sourceIndices;
                _indexBuffer = indexBuffer;
                _indexCountPerParticleMesh = indexCountPerParticleMesh;
                _vertexCountPerParticleMesh = vertexCountPerParticleMesh;
            }

            public void Execute(int particleIndex)
            {
                int currentIndicesIndex = _indexCountPerParticleMesh * particleIndex;
                int currentIndicesStartIndex = _vertexCountPerParticleMesh * particleIndex;
                for (int i = 0; i < _indexCountPerParticleMesh; i++)
                {
                    _indexBuffer[currentIndicesIndex + i] = _sourceIndices[i] + currentIndicesStartIndex;
                }
            }
        } // CreateParticleIndicesJob
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(BatchedMeshGenerator))]
    public class BGVehicleMeshGeneratorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();
            EditorGUILayout.Space(10f);

            BatchedMeshGenerator generator = (BatchedMeshGenerator)target;

            if (GUILayout.Button(nameof(generator.GenerateVehicleMesh)))
            {
                generator.GenerateVehicleMesh();
            }
        }
    }
#endif
}

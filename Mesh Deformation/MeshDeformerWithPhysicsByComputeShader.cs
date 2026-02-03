using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace VertexDisplacement
{
    public class MeshDeformerWithPhysicsByComputeShader : MeshDeformer, IDisposable
    {
        /// <summary>
        /// Векторы ускорения вершин
        /// </summary>
        private Vector3[] vertexVelocities;

        private VertexData[] vertexData;
        private GraphicsBuffer vertexBuffer;
        private GraphicsBuffer deformBuffer;

        private ComputeBuffer deformSettingsComputeBuffer;

        private readonly ComputeShader compute;
        private readonly int threadGroups;
        private int kernel;

        public MeshDeformerWithPhysicsByComputeShader(Deformer deformer) : base(deformer)
        {
            meshDeformTarget = deformer.deformTarget;
            deformSettings = meshDeformTarget.deformSettings;
            var mesh = meshDeformTarget.MeshFilter.mesh;

            vertexCount = mesh.vertexCount;

            originalVertices = new Vector3[vertexCount];
            mesh.vertices.CopyTo(originalVertices, 0);

            displacedVertices = new Vector3[vertexCount];
            originalVertices.CopyTo(displacedVertices, 0);

            normals = new Vector3[mesh.normals.Length];
            mesh.normals.CopyTo(normals, 0);

            vertexVelocities = new Vector3[vertexCount];

            vertexData = new VertexData[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                vertexData[i].original = originalVertices[i];
                vertexData[i].displaced = displacedVertices[i];
                vertexData[i].velocity = vertexVelocities[i];
                vertexData[i].normal = normals[i];
            }

            vertexBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                vertexCount,
                UnsafeUtility.SizeOf<VertexData>()
            );
            vertexBuffer.SetData(vertexData);

            compute = deformer.compute;

            kernel = compute.FindKernel("ComputeVertexDisplacement");
            compute.SetBuffer(kernel, "vertices", vertexBuffer);

            threadGroups = Mathf.CeilToInt(vertexCount / 4f);


            deformSettingsComputeBuffer = new ComputeBuffer(
                1,
                UnsafeUtility.SizeOf<DeformSettingsCompute>(),
                ComputeBufferType.Constant
            );
            var _deformSettingsCompute = new DeformSettingsCompute[1] { new DeformSettingsCompute() };
            _deformSettingsCompute[0].CopyFrom(deformSettings);

            deformSettingsComputeBuffer.SetData(_deformSettingsCompute);
            compute.SetConstantBuffer(
                nameof(DeformSettingsCompute),
                deformSettingsComputeBuffer,
                0,
                UnsafeUtility.SizeOf<DeformSettingsCompute>()
            );
        }

        public override void Update()
        {
            if (deformingForcePoints.Count == 0)
            {
                if (deformBuffer != null)
                    deformBuffer.Release();
            }

            compute.SetInt("deformCount", deformingForcePoints.Count);
            deformBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                deformingForcePoints.Count == 0 ? 6 : deformingForcePoints.Count,
                UnsafeUtility.SizeOf<Vector3>()
            );
            deformBuffer.SetData(deformingForcePoints);

            compute.SetBuffer(kernel, nameof(deformingForcePoints), deformBuffer);

            compute.Dispatch(kernel, threadGroups, 1, 1);

            // take datas back
            vertexBuffer.GetData(vertexData);
            for (int i = 0; i < vertexCount; i++)
                displacedVertices[i] = vertexData[i].displaced;

            meshDeformTarget.MeshFilter.mesh.vertices = displacedVertices;
            meshDeformTarget.MeshFilter.mesh.RecalculateNormals();
            deformingForcePoints.Clear();
        }

        public void Dispose()
        {
            if (vertexBuffer != null)
                vertexBuffer.Release();
            if (deformBuffer != null)
                deformBuffer.Release();
        }
    }
}

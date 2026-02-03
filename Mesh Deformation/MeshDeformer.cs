using System;
using System.Collections.Generic;
using UnityEngine;

namespace VertexDisplacement
{
    public abstract class MeshDeformer : IMeshDeformer
    {
        public MeshDeformTarget meshDeformTarget;

        protected DeformSettings deformSettings;
        public DeformSettings DeformSettings => deformSettings;

        protected int vertexCount;
        /// <summary>
        /// Оригинальные вершины
        /// </summary>
        protected Vector3[] originalVertices;
        /// <summary>
        /// Смещённые веришны
        /// </summary>
        protected Vector3[] displacedVertices;
        /// <summary>
        /// Векторы нормалей вершин
        /// </summary>
        protected Vector3[] normals;

        protected List<Vector3> deformingForcePoints = new();

        public MeshDeformer(Deformer deformer)
        {
            meshDeformTarget = deformer.deformTarget;
            deformSettings = meshDeformTarget.deformSettings;
        }

        public virtual void Update()
        {
        }

        public virtual void AddDeformingForce(Vector3 point)
        {
            deformingForcePoints.Add(meshDeformTarget.transform.InverseTransformPoint(point));
        }

        public virtual void OnDrawGizmos()
        {
        }
    }

}

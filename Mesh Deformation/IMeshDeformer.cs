using UnityEngine;

namespace VertexDisplacement
{
    public interface IMeshDeformer
    {
        DeformSettings DeformSettings { get; }

        void Update();
        void AddDeformingForce(Vector3 point);

        void OnDrawGizmos();
    }
}
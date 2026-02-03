using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VertexDisplacement
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DeformSettingsCompute
    {
        public float maxDistance;

        public float deltaTime;

        public float springForce;
        public float edgeSpringForce;
        public float damping;

        public float dotValExcludeForce;
        public float dotValMultipliedForce;
        public float dotValEdgeForce;

        public float contactRadius;
        public float force;

        public float closestToContactRadius;
        public float edgeForce;

        public float outForce;

        public float scale;

        public void CopyFrom(DeformSettings settings)
        {
            maxDistance = settings.maxDistance;

            deltaTime = Time.fixedDeltaTime;

            springForce = settings.springForce;
            edgeSpringForce = settings.edgeSpringForce;
            damping = settings.damping;

            dotValExcludeForce = settings.dotValExcludeForce;
            dotValMultipliedForce = settings.dotValMultipliedForce;
            dotValEdgeForce = settings.dotValEdgeForce;

            contactRadius = settings.contactRadius;
            force = settings.force;

            closestToContactRadius = settings.closestToContactRadius;
            edgeForce = settings.edgeForce;

            outForce = settings.outForce;

            scale = settings.Scale;
        }
    }
}
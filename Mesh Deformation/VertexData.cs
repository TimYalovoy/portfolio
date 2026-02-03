using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VertexDisplacement
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexData
    {
        public Vector3 original;
        public Vector3 displaced;
        public Vector3 velocity;
        public Vector3 normal;
    }
}
using Eidos.Platon;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VertexDisplacement
{
    [Serializable]
    public class DeformSettings
    {
        [Header("Spring")]
        /// <summary>
        /// Основная сила "пружины" для возврата вершин в исходное положение
        /// </summary>
        [Tooltip("Основная сила \"пружины\" для возврата вершин в исходное положение")]
        public float springForce = 20f;
        [Tooltip("Дополнительная сила \"пружины\" для возврата вершин в исходное положение")]
        public float edgeSpringForce = 15f;
        /// <summary>
        /// Сила гашения силы пружины
        /// </summary>
        [Tooltip("Сила гашения силы пружины")]
        public float damping = 5f;

        [Header("Filtering")]
        [Range(-1f, 0.5f), Tooltip("Значение скалярного произведения нормализованных векторов направления от точки контакта к вершине и нормали этой веришны, при котором вершина исключается для воздействия силы")]
        public float dotValExcludeForce = -.08f;
        [Range(-1f, 0.5f), Tooltip("Значение скалярного произведения нормализованных векторов направления от точки контакта к вершине и нормали этой веришны, при котором достигается увеличенная сила")]
        public float dotValMultipliedForce = -.85f;
        [Range(-1f, 0.5f), Tooltip("Значение скалярного произведения нормализованных векторов направления от точки контакта к вершине и нормали этой веришны, при котором применяется рассеивающая сила")]
        public float dotValEdgeForce = -.08f;

        [Header("Main Force")]
        /// <summary>
        /// Основая сила направленная на вершины в радиусе [<see cref="contactRadius"/>]
        /// </summary>
        [Tooltip("Основая сила направленная на вершины в радиусе [Contact Radius]")]
        public float force = 10f;
        /// <summary>
        /// Основной радиус воздествия на веришны
        /// </summary>
        [Tooltip("Основной радиус воздествия на веришны")]
        public float contactRadius = 0.012f;
        /// <summary>
        /// Сила применяемая на веришны, которые не попали в радиус
        /// </summary>
        [Tooltip("Сила применяемая на веришны, которые не попали в радиус")]
        public float outForce = 1f;

        [Header("Additional Force")]
        /// <summary>
        /// Рассеивающая сила направленная на вершины в радиусе [<see cref="closestToContactRadius"/>]
        /// </summary>
        [Tooltip("Рассеивающая сила направленная на вершины в радиусе [Closest To Contact Radius]")]
        public float edgeForce = 13.5f;
        [Tooltip("Ослабленная рассеивающая сила направленная на вершины в радиусе [Closest To Contact Radius]")]
        public float weakenedEdgeForce = 1.15f;
        [Tooltip("Дополнительный радус воздействия")]
        public float closestToContactRadius = 0.02f;

        [Tooltip("Максимальное расстояние смещения вершины")]
        public float maxDistance = 0.9f;
[Header("Scale")]
        /// <summary>
        /// Использовать скейл вне зависимости от размера объекта?
        /// </summary>
        [Tooltip("Использовать скейл вне зависимости от размера объекта?")]
        public bool useScaleFactor = false;
        /// <summary>
        /// Независимый скейл
        /// </summary>
        public float uniformScale = 2f;
        /// <summary>
        /// Текущий скейл
        /// </summary>
        [SerializeField] private float scale = 2f;
        public float Scale
        {
            get => scale;
            set
            {
                if (useScaleFactor)
                {
                    scale = uniformScale;
                }
                else
                {
                    scale = value;
                }
            }
        }

        public void OnValidate()
        {
            if (springForce < 0f)
            {
                springForce = Mathf.Abs(springForce);
            }
            if (damping < 0f)
            {
                damping = Mathf.Abs(damping);
            }

            if (force < 0f)
            {
                force = Mathf.Abs(force);
            }
            if (outForce < 0f)
            {
                outForce = Mathf.Abs(outForce);
            }
            if (contactRadius < 0.001f)
            {
                contactRadius = 0.001f;
            }
            if (closestToContactRadius < 0.001f)
            {
                closestToContactRadius = 0.001f;
            }

            if (outForce > force)
            {
                outForce = force / 10f;
            }
        }

        public void PassParametersToComputeShader(ComputeShader compute)
        {
            compute.SetFloat(nameof(maxDistance), maxDistance);

            compute.SetFloat(nameof(springForce), springForce);
            compute.SetFloat(nameof(edgeSpringForce), edgeSpringForce);
            compute.SetFloat(nameof(damping), damping);

            compute.SetFloat(nameof(dotValExcludeForce), dotValExcludeForce);
            compute.SetFloat(nameof(dotValMultipliedForce), dotValMultipliedForce);
            compute.SetFloat(nameof(dotValEdgeForce), dotValEdgeForce);

            compute.SetFloat(nameof(contactRadius), contactRadius);
            compute.SetFloat(nameof(force), force);

            compute.SetFloat(nameof(closestToContactRadius), closestToContactRadius);
            compute.SetFloat(nameof(edgeForce), edgeForce);

            compute.SetFloat(nameof(outForce), outForce);

            compute.SetFloat("scale", Scale);
        }

        public float Displace(float distance2Vertex)
        {
            return contactRadius * curve.Evaluate(distance2Vertex / contactRadius);
        }
    }

    [RequireComponent(typeof(MeshFilter))]
    public class MeshDeformTarget : MonoBehaviour, IInitializable
    {
        // ...
    }
}

using Obi; // physics engine for unity link: https://obi.virtualmethodstudio.com
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RopeTraining // 
{
    #region === KNOT TYPE ===
    public enum KnotType
    {
        Trefoil = 0,
        FigureEight = 1,
        Square = 2,
        Granny = 3,
        Frictional = 4
    }
    #endregion

    #region === INTERSECTION ===
    [System.Serializable]
    public struct Intersection
    {
        public int firstElementIndex;
        public int secondElementIndex;
        public float dot;
        public float distance;
        public int sign;

        public Intersection(bool init)
        {
            firstElementIndex = -1;
            secondElementIndex = -1;
            dot = float.PositiveInfinity;
            distance = float.PositiveInfinity;
            sign = 0;
        }

        public Intersection(int firstIndex, int secondIndex, float dot, float distance, int sign)
        {
            firstElementIndex = firstIndex;
            secondElementIndex = secondIndex;
            this.dot = dot;
            this.distance = distance;
            this.sign = sign;
        }

        public bool IsNotInited()
            => (firstElementIndex == -1 || secondElementIndex == -1);

        public static bool operator ==(Intersection lhs, Intersection rhs)
            => (lhs.firstElementIndex == rhs.firstElementIndex && lhs.secondElementIndex == rhs.secondElementIndex)
                || (lhs.firstElementIndex == rhs.secondElementIndex && lhs.secondElementIndex == rhs.firstElementIndex);

        public static bool operator !=(Intersection lhs, Intersection rhs)
            => !(lhs == rhs);

        public readonly override bool Equals(object obj)
        {
            if (obj is Intersection comparable)
            {
                return comparable == this;
            }
            else return false;
        }
    }
    #endregion
    
    //
    // link en: https://en.wikipedia.org/wiki/Skein_relation
    // link ru: https://ru.wikipedia.org/wiki/Скейн-соотношение
    //
    // base for Knot Theory:
    // link en: 
    // https://en.wikipedia.org/wiki/Knot_theory
    // link ru: https://ru.wikipedia.org/wiki/Теория_узлов
    //
    /// <summary>
    /// Is detector Skein Relation (rope's self intersections) and send event if intersections is forming knot
    /// </summary>
    public class RopeKnotDetector : MonoBehaviour
    {
        private const float maxDegres = 90f;

        [SerializeField] private KnotType knotToDetection;
        [SerializeField] private float distanceThreshold; // 0.0381
        [Min(0)][SerializeField] private float degresOffset;

        private ObiRope _rope;
        private ObiSolver _solver;
        private List<Intersection> intersections = new List<Intersection>();
        
        private int leftIndex;
        private int rightIndex;

        private List<Intersection> intersectionsInKnot = new List<Intersection>();
        private List<ObiStructuralElement> particlesInKnot = new List<ObiStructuralElement>();
        private Dictionary<int, (List<ObiStructuralElement> elements, List<Intersection> intersections)> knots = new Dictionary<int, (List<ObiStructuralElement> elements, List<Intersection> intersections)>();
        private int knotCounts = 0;

        private List<ObiStructuralElement> unknottedElements = new List<ObiStructuralElement>();
        
        private float degOffset2Radians;
        private float radiansThreshold;

        #region === RopeKnotDetectorEditor ===
        // ...
        #endregion

        private Action<List<Intersection>> knotFindAlgorithm;
        public event Action<int, int> OnKnotDetected = (begin, end) => { };


        #region === DEBUGGING ===
        private System.Text.StringBuilder sb = new System.Text.StringBuilder();
        private Matrix4x4 debugMatrix;

        // ...
        #endregion

        private void Awake()
        {
            _rope = GetComponent<ObiRope>();
            _solver = transform.parent.GetComponent<ObiSolver>();

            debugMatrix = _solver.transform.localToWorldMatrix;
            RadiansThresholdCalculate();

            var radius = _rope.blueprint.GetParticleMaxRadius(0);
            var diametr = 2 * radius;
            distanceThreshold = diametr + (radius / 2f);

            unknottedElements = _rope.elements;

            ChangeKnotTypeToDetection(knotToDetection);

        }

        private void OnValidate()
        {
            if (degresOffset >= maxDegres)
                degresOffset = maxDegres;

            RadiansThresholdCalculate();

            ChangeKnotTypeToDetection(knotToDetection);
        }

        private void RadiansThresholdCalculate()
        {
            degOffset2Radians = Mathf.Deg2Rad * (maxDegres - degresOffset);
            radiansThreshold = Mathf.Cos(degOffset2Radians);
        }

        public void ChangeKnotTypeToDetection(KnotType knotType = KnotType.Trefoil)
        {
            knotToDetection = knotType;
            switch (knotToDetection)
            {
                case KnotType.Trefoil:
                    knotFindAlgorithm = CheckForTrefoilKnot;
                    break;
                // etc.
            }
        }

        bool isHaveAnyIntersection = false;
        void Update()
        {
            if (!_solver) return;

            isHaveAnyIntersection = false;
            intersections.Clear();
            for (int i = 0; i < unknottedElements.Count - 3; i++)
            {
                var element = unknottedElements[i];
                leftIndex = i - 2;
                if (leftIndex >= 0)
                {
                    for (int j = leftIndex; j >= 0; j--)
                    {
                        var secondElement = unknottedElements[j];
                        SkeinRelationCheck(element, secondElement,
                        (dot, distance, sign) =>
                        {
                            OnSkeinRelationPass(i, j, dot, distance, sign);
                        });
                    }
                }

                rightIndex = i + 2;
                if (rightIndex < unknottedElements.Count)
                {
                    for (int k = rightIndex; k < unknottedElements.Count; k++)
                    {
                        var secondElement = unknottedElements[k];
                        SkeinRelationCheck(element, secondElement,
                        (dot, distance, sign) =>
                        {
                            OnSkeinRelationPass(i, k, dot, distance, sign);
                        });
                    }
                }
            }

            if (isHaveAnyIntersection)
            {
                knotFindAlgorithm(intersections);
                isHaveAnyIntersection = false;
            }
        }


        string signDefinedMsg = "Sign defined by ";
        string axis = "";
        private void SkeinRelationCheck(ObiStructuralElement fEl, ObiStructuralElement sEl, Action<float, float, int> OnSuccess, Action OnFailed = default)
        {
            GetFullDataOfElement(fEl,
                    out var fElCenter, out var fElDirection, out var fElNormal);
            GetFullDataOfElement(sEl,
                out var sElCenter, out var sElDirection, out var sElNormal);

            var distance = Vector3.Distance(fElCenter, sElCenter);
            if (distance < distanceThreshold)
            {
                var dot = Vector3.Dot(fElDirection, sElDirection);
                var absDot = Mathf.Abs(dot);
                if (!(Mathf.Approximately(absDot, 1f) || absDot >= radiansThreshold))
                {
                    var yDiff = fElCenter.y - sElCenter.y;
                    var zDiff = fElCenter.z - sElCenter.z;
                    var xDiff = fElCenter.x - sElCenter.x;

                    if (Mathf.Approximately(yDiff, 0f))
                    {
                        if (Mathf.Approximately(zDiff, 0f))
                        {
                            OnSuccess(dot, distance, Mathf.Approximately(xDiff, 0f) ? 0 : (int)Mathf.Sign(xDiff));
                            axis += $"{fEl.particle1}: " + signDefinedMsg + "<b><color=red>X</color>-axis.</b>\n";
                        }
                        else
                        {
                            OnSuccess(dot, distance, (int)Mathf.Sign(zDiff));
                            axis += $"{fEl.particle1}: " + signDefinedMsg + $"<b><color=blue>Z</color>-axis.</b>\n";
                        }
                    }
                    else
                    {
                        OnSuccess(dot, distance, (int)Mathf.Sign(yDiff));
                        axis += $"{fEl.particle1}: " + signDefinedMsg + $"<b><color=green>Y</color>-axis.</b>\n";
                    }
                }
                else
                {
                    if (OnFailed != null)
                    {
                        OnFailed();
                    }
                }
            } else if (OnFailed != null)
            {
                OnFailed();
            }
        }


        private void OnSkeinRelationPass(int fInx, int sInx, float dot, float distance, int sign)
        {
            isHaveAnyIntersection = true;
            var Intersection = new Intersection(fInx, sInx, dot, distance, sign);
            if (intersections.Contains(Intersection))
            {
                var sameJunc = intersections.Find(junc => junc == Intersection);
                var index = intersections.IndexOf(sameJunc);
                intersections[index] = Intersection;
            }
            else
            {
                intersections.Add(Intersection);
            }
        }


        // 
        // Trefoil knot
        // pairs (crosses): (p0, p1), (p2, p3), (p4, p5)
        // sequence of indexes: p0 < p2 < p4 < p1 < p3 < p5
        //                      p0           <           p5
        //                           p2      <      p3
        //                                p4 < p1
        //
        //
        // Figure-eight knot
        // pairs (crosses): (p0, p1), (p2, p3), (p4, p5), (p6, p7)
        // sequence of indexes: p0 < p2 < p4 < p6 < p1 < p3 < p5 < p7
        //                      p0                <                p7
        //                           p2           <           p5
        //                                p4      <      p3
        //                                     p6 < p1
        //
        // SURGICAL TIE (or square knot) - is 2 trefoils
        // 
        //
        // Surgeon’s or Friction Knot (8_2 square?)
        // pairs (crosses): (p0, p1), (p2, p3), (p4, p5), (p6, p7), (p8, p9), (p10, p11), (p12, p13), (p14, p15)
        // sequence of indexes: p0 < p2 < p4 < p6 < p8 < p10 < p12 < p1 < p14 < p5 < p7 < p9 < p11 < p13 < p3 < p15
        // 
        // Surgeon’s or Friction Knot
        // pairs (crosses): (p0, p1), (p2, p3), (p4, p5), (p6, p7), (p8, p9), (p10, p11), (p12, p13), (p14, p15)
        // sequence of indexes:
        //                      p0 < p2 < p4 < p6 < p8 < (p10 < p12 < p14 < p11 < p13 < p15) < p1 < p3 < p5 < p7 < p9
        //                                                  
        //                      p0 < p2 < p4 < p6 < p8 < (   T___R___E___F___O___I___L   ) < p1 < p3 < p5 < p7 < p9
        //                                               p10 < p12 < p14 < p11 < p13 < p15
        //
        // p9 is max
        // p0 is min
        //

        List<int> sequence = new List<int>();
        int min = 0;
        int minCounts = 0;
        int max = 0;
        int maxCounts = 0;
        int centralIntersectionIndex = -1;
        private void CheckForTrefoilKnot(List<Intersection> intersections)
        {
            if (intersections == null) return;
            if (intersections.Count < 3) return;

            sb.Append($"Intersections sequence:\n");
            sequence.Clear();
            foreach (var intersection in intersections)
            {
                sequence.Add(intersection.firstElementIndex);
                sequence.Add(intersection.secondElementIndex);
                sb.Append($"({intersection.firstElementIndex}; {intersection.secondElementIndex}) ");
            }
            sb.Append($"\nOnBeginCheck: intersections.Count:{intersections.Count}\n");

            min = sequence.Min();
            minCounts = sequence.Count(el => el == min);
            max = sequence.Max();
            maxCounts = sequence.Count(el => el == max);
            centralIntersectionIndex = Mathf.FloorToInt(intersections.Count / 2);
            sb.Append($"min: {min}; max: {max}; centralIntersection index: {centralIntersectionIndex}\n" +
                $"minCounts: {minCounts} ={((minCounts == maxCounts) ? "=" : "/")}= maxCounts:{maxCounts}\n");
            sb.Append($"sign sequence: {intersections[0].sign} {intersections[centralIntersectionIndex].sign} {intersections[^1].sign}\n");

            if ((max - min) > 8) return;

            if (intersections[centralIntersectionIndex].secondElementIndex != min)
            {
                sb.Append($"cond failed: element2 of central intersection != min\n");
                WriteLogs();
                return;
            }

            if (intersections[centralIntersectionIndex].firstElementIndex != max)
            {
                sb.Append($"cond failed: element1 of central intersection != max\n");
                WriteLogs();
                return;
            }

            if (intersections[0].secondElementIndex != min)
            {
                sb.Append($"cond failed: element2 of first intersection != min\n");
                WriteLogs();
                return;
            }


            if (intersections[^1].firstElementIndex != max)
            {
                sb.Append($"cond failed: element1 of last intersection != max\n");
                WriteLogs();
                return;
            }

            var left = max - 1;
            var right = min + 1;

            bool isOneIntersection = left == intersections[0].firstElementIndex && right == intersections[^1].secondElementIndex;
            if (isOneIntersection)
            {
                sb.Append($"It is <b><color=cyan>not</color> trefoil knot</b>\n");
                WriteLogs();
                return;
            }

            if (minCounts != maxCounts)
            {
                sb.Append($"It is <color=red>not tighted</color> <b>trefoil knot</b>\n");
                WriteLogs();
                return;
            }

            if (!(intersections[0].sign == intersections[^1].sign && intersections[0].sign != intersections[centralIntersectionIndex].sign))
            {
                sb.Append($"It is <b><color=magenta>NOT</color> knot</b>\n");
                WriteLogs();
                return;
            }

            sb.Append($"It is <color=green>tighted</color> <b>trefoil knot</b>.\n\n" +
                $"min - 1: {min - 1}\tp1Index: {_rope.elements[min - 1].particle1}\n" +
                $"max + 1: {max + 1}\tp2Index:{_rope.elements[max + 1].particle2}\n");
            WriteLogs();
            OnKnotDetected(_rope.elements[min - 1].particle1, _rope.elements[max + 1].particle2);
            
            var elementsInKnot = _rope.elements.SkipWhile(el => el.particle1 != _rope.elements[min - 1].particle1)
                                    .Take((_rope.elements[max + 1].particle2 - _rope.elements[min - 1].particle1));

            particlesInKnot = elementsInKnot.ToList();
            unknottedElements = _rope.elements.Except(elementsInKnot).ToList();

            intersectionsInKnot = intersections;

            if (knots.TryAdd(knotCounts, (particlesInKnot, intersectionsInKnot)))
            {
                knotCounts++;
            }
        }

        private void CheckForFigureEightKnot(List<Intersection> intersections)
        {
            if (intersections == null) return;
            if (intersections.Count < 4) return;

            sb.Append($"Intersections sequence:\n");
            sequence.Clear();
            foreach (var intersection in intersections)
            {
                sequence.Add(intersection.firstElementIndex);
                sequence.Add(intersection.secondElementIndex);
                sb.Append($"({intersection.firstElementIndex}; {intersection.secondElementIndex}) ");
            }
            sb.Append($"\nOnBeginCheck: intersections.Count:{intersections.Count}\n");

            /*
            (min + max) / 2 == multipleIntersectionElement >= 2
            
            Junctions sequence:
            (29; 20) (28; 21) (29; 21) (30; 21) (31; 21) (27; 22) (33; 23) (33; 24) (33; 25) (34; 25) (33; 26) (32; 27) (33; 27) 
            OnBeginCheck: intersections.Count:13
            min: 20; max: 34
            centralJunction index: 6
            minCounts: 1 === maxCounts:1

            multipleIntersectionElement == 27
            multiJuncElIntersectionCount == 3
             */

            min = sequence.Min();
            max = sequence.Max();

            var multipleIntersectionElement = (min + max) / 2;
            var multiInterscsElIntersectionCount = sequence.Count(el =>  el == multipleIntersectionElement);
            sb.AppendLine($"multipleIntersectionElement: {multipleIntersectionElement}; intersectionsCount: {multiInterscsElIntersectionCount}");
            if (multiInterscsElIntersectionCount == intersections.Count)
            {
                sb.AppendLine($"<b>Break.</b> multiInterscsElIntersectionCount == intersections.Count");
                WriteLogs();
                return;
            }


            if (multiInterscsElIntersectionCount < 2)
            {
                sb.AppendLine($"<b>Break.</b> multiInterscsElIntersectionCount < 2.");
                WriteLogs();
                return;
            }

            sb.AppendLine($"It is <color=green>tighted?</color> <b>Figure-Eight Knot</b>");
            WriteLogs();
            Debug.Break();
        }

        private void CheckForSquareKnot(List<Intersection> intersections)
        {
            // TODO: Implement
        }

        private void CheckForGrannyKnot(List<Intersection> intersections)
        {
            // TODO: Implement
        }

        private void CheckForFrictionalKnot(List<Intersection> intersections)
        {
            // TODO: Implement
        }

        private void GetElementCenter(ObiStructuralElement element, out Vector3 elementCenter)
        {
            elementCenter = Vector3.Lerp(_solver.positions[element.particle1], _solver.positions[element.particle2], 0.5f);
        }

        public Vector3 editor_GetElementCenter(ObiStructuralElement element)
        { /* ... */ }

        private void GetElementDirection(ObiStructuralElement element, out Vector3 elementDirection)
        {
            elementDirection = (_solver.positions[element.particle2] - _solver.positions[element.particle1]).normalized;
        }

        public Vector3 editor_GetELementDirection(ObiStructuralElement element)
        { /* ... */ }

        private void GetElementNormal(ObiStructuralElement element, out Vector3 elementNormal)
        {
            elementNormal = Vector3.Cross(_solver.positions[element.particle1], _solver.positions[element.particle2]);
        }

        public Vector3 editor_GetElementNormal(ObiStructuralElement element)
        { /* ... */ }

        private void GetFullDataOfElement(ObiStructuralElement element, out Vector3 elementCenter, out Vector3 elementDirection, out Vector3 elementNormal)
        {
            GetElementCenter(element, out elementCenter);
            GetElementDirection(element, out elementDirection);
            GetElementNormal(element, out elementNormal);
        }
    }
}

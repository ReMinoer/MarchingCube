using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(BoxCollider))]
public class MarchingCubeRenderer : MonoBehaviour
{
    public GameObject BlobsRootNode;
    public Material Material;
    public float GridStep = 0.1f;
    public float WeightThreshold = 10f;
    private BoxCollider _gridBound;
    private Mesh _mesh;

    static private readonly IFilterBuilder[] FilterBuilders =
    {
        new SingleFilter(),
        new DuoFilter(),
        new TrioFilter(), 
        new SquareFilter(), 
        new TetraFilter(), 
        new SnakeFilter()
    };

    void Awake()
    {
        _gridBound = GetComponent<BoxCollider>();
        _gridBound.isTrigger = true;
        _mesh = gameObject.AddComponent<MeshFilter>().mesh;

        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = Material;
    }

    void Update()
    {
        // NOTE : Ne pas updater la liste de blob à chaque update
        Blob[] blobs = (BlobsRootNode ?? gameObject).GetComponentsInChildren<Blob>();
        bool[][][] grid = ComputeGrid(blobs);

        GenerateMesh(grid);
    }

    public bool[][][] ComputeGrid(Blob[] blobs)
    {
        int sizeX = (int)(_gridBound.bounds.size.x / GridStep) + 1;
        int sizeY = (int)(_gridBound.bounds.size.y / GridStep) + 1;
        int sizeZ = (int)(_gridBound.bounds.size.z / GridStep) + 1;

        var grid = new bool[sizeX][][];
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = new bool[sizeY][];
            for (int j = 0; j < grid[i].Length; j++)
                grid[i][j] = new bool[sizeZ];
        }

        for (int i = 0; i < grid.Length; i++)
            for (int j = 0; j < grid[i].Length; j++)
                for (int k = 0; k < grid[i][j].Length; k++)
                {
                    float x = _gridBound.bounds.min.x + i * GridStep;
                    float y = _gridBound.bounds.min.y + j * GridStep;
                    float z = _gridBound.bounds.min.z + k * GridStep;

                    var pos = new Vector3(x, y, z);

                    float totalWeigth = 0;
                    foreach (Blob blob in blobs)
                    {
                        float distance = Vector3.Distance(pos, blob.transform.position);

                        float weight = blob.WeightFunction(distance);
                        if (weight < 0)
                            weight = 0;

                        totalWeigth += weight;
                    }

                    grid[i][j][k] = totalWeigth > WeightThreshold;
                }

        return grid;
    }

    private void GenerateMesh(bool[][][] grid)
    {
        _mesh.Clear();
        var meshData = new MeshData();

        for (int i = 0; i < grid.Length - 1; i++)
            for (int j = 0; j < grid[i].Length - 1; j++)
                for (int k = 0; k < grid[i][j].Length - 1; k++)
                {
                    foreach (IFilterBuilder filter in FilterBuilders)
                    {
                        IEnumerable<int> result;
                        bool state;
                        var localCube = new LocalCube(grid, i, j, k);

                        if (localCube.ApplyFilter(filter, out result, out state))
                        {
                            Vector3 cubeOrigin = _gridBound.bounds.min + new Vector3(i, j, k) * GridStep;
                            filter.Draw(meshData, cubeOrigin, result.ToArray(), !state);
                        }
                    }
                }

        _mesh.vertices = meshData.Vertices.ToArray();
        _mesh.triangles = meshData.Triangles.ToArray();
    }

    public class LocalCube : IEnumerable<PointState>
    {
        private readonly bool[][][] _grid;
        private readonly int _i;
        private readonly int _j;
        private readonly int _k;

        private readonly List<PointState> _pointStates;

        public bool this[int pointIndex]
        {
            get { return _grid[_i + pointIndex / 4][_j + (pointIndex / 2) % 2][_k + pointIndex % 2]; }
        }

        public LocalCube(bool[][][] grid, int i, int j, int k)
        {
            _grid = grid;
            _i = i;
            _j = j;
            _k = k;
            _pointStates = new List<PointState>();

            Add(0, grid[i][j][k]);
            Add(1, grid[i][j][k + 1]);
            Add(2, grid[i][j + 1][k]);
            Add(3, grid[i][j + 1][k + 1]);
            Add(4, grid[i + 1][j][k]);
            Add(5, grid[i + 1][j][k + 1]);
            Add(6, grid[i + 1][j + 1][k]);
            Add(7, grid[i + 1][j + 1][k + 1]);
        }

        private void Add(int pointIndex, bool value)
        {
            StopIgnoring(new PointState
            {
                PointIndex = pointIndex,
                Value = value
            });
        }

        public bool ApplyFilter(IFilterBuilder filter, out IEnumerable<int> result, out bool state)
        {
            Stack<NeighborhoodRule> rules = filter.GenerateRulesStack();

            var resultQueue = new Queue<int>();
            foreach (PointState pointState in this.Select(x => x).ToArray())
            {
                resultQueue.Enqueue(pointState.PointIndex);

                if (ApplyFilter(rules, resultQueue))
                {
                    result = resultQueue;
                    state = pointState.Value;
                    return true;
                }

                resultQueue.Dequeue();
            }

            result = null;
            state = false;
            return false;
        }

        public bool ApplyFilter(Stack<NeighborhoodRule> rules, Queue<int> result)
        {
            if (!rules.Any())
                return true;

            NeighborhoodRule rule = rules.Pop();

            int resultPoint = result.ElementAt(rule.PointId);
            int[] neighbors = LookAtTable.Neighborhood[resultPoint];
            IEnumerable<PointState> localNeighbors = this.Where(x => neighbors.Contains(x.PointIndex)).ToArray();

            foreach (PointState point in localNeighbors)
            {
                if ((rule.MustBe == MustBe.Equal && point.Value == this[rule.PointId])
                    || (rule.MustBe == MustBe.NotEqual && point.Value != this[rule.PointId]))
                {
                    result.Enqueue(point.PointIndex);
                    Ignore(point);

                    if (ApplyFilter(rules, result))
                        return true;

                    StopIgnoring(point);
                    result.Dequeue();
                }
            }

            rules.Push(rule);
            return false;
        }

        public void StopIgnoring(PointState pointState)
        {
            _pointStates.Add(pointState);
            _pointStates.Sort((x,y) => y.PointIndex.CompareTo(x.PointIndex));
        }

        public void Ignore(PointState pointState)
        {
            _pointStates.Remove(pointState);
        }

        public bool Contains(int pointIndex)
        {
            return _pointStates.Any(x => x.PointIndex == pointIndex);
        }

        public IEnumerator<PointState> GetEnumerator()
        {
            return _pointStates.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct PointState
    {
        public int PointIndex { get; set; }
        public bool Value { get; set; }
    }

    #region Filters

    public class SingleFilter : FilterBuilder
    {
        /*  Cube :
         * 
         *     .----.
         *   ./|   /|
         *   2----. |
         *   | 1--|-.
         *   |/   |/
         *   0----3
         */

        public SingleFilter()
        {
            int p1 = NeighborOf(0, MustBe.NotEqual);
            int p2 = NeighborOf(0, MustBe.NotEqual);
            int p3 = NeighborOf(0, MustBe.NotEqual);

            AddTriangle(0, p1, 0, p2, 0, p3);
        }
    }

    public class DuoFilter : FilterBuilder
    {
        /*  Cube :
         * 
         *     .----.
         *   ./|   /|
         *   3----5 |
         *   | 2--|-4
         *   |/   |/
         *   0----1
         */

        public DuoFilter()
        {
            int p1 = NeighborOf(0, MustBe.Equal);

            int p2 = NeighborOf(0, MustBe.NotEqual);
            int p3 = NeighborOf(0, MustBe.NotEqual);
            int p4 = NeighborOf(p1, MustBe.NotEqual);
            int p5 = NeighborOf(p1, MustBe.NotEqual);

            AddQuad(0, p2, 0, p3, p1, p5, p1, p4);
        }
    }

    public class TrioFilter : FilterBuilder
    {
        /*  Cube :
         * 
         *     2----6
         *   ./|   /|
         *   3----. |
         *   | 1--|-5
         *   |/   |/
         *   0----4
         */

        public TrioFilter()
        {
            int p1 = NeighborOf(0, MustBe.Equal);
            int p2 = NeighborOf(p1, MustBe.Equal);

            int p3 = NeighborOf(0, MustBe.NotEqual);
            int p4 = NeighborOf(0, MustBe.NotEqual);
            int p5 = NeighborOf(p1, MustBe.NotEqual);
            int p6 = NeighborOf(p2, MustBe.NotEqual);

            AddQuad(0, p4, 0, p3, p2, p3, p2, p6);
            AddTriangle(0, p4, 0, p6, p1, p5);
        }
    }

    public class SquareFilter : FilterBuilder
    {
        /*  Cube :
         * 
         *     2----6
         *   ./|   /|
         *   3----7 |
         *   | 1--|-5
         *   |/   |/
         *   0----4
         */

        public SquareFilter()
        {
            int p1 = NeighborOf(0, MustBe.Equal);
            int p2 = NeighborOf(p1, MustBe.Equal);
            int p3 = NeighborOf(p2, MustBe.Equal);

            int p4 = NeighborOf(0, MustBe.NotEqual);
            int p5 = NeighborOf(p1, MustBe.NotEqual);
            int p6 = NeighborOf(p2, MustBe.NotEqual);
            int p7 = NeighborOf(p3, MustBe.NotEqual);

            AddQuad(0, p4, p1, p5, p2, p6, p3, p7);
        }
    }

    public class TetraFilter : FilterBuilder
    {
        /*  Cube :
         * 
         *     2----6
         *   ./|   /|
         *   4----. |
         *   | 1--|-3
         *   |/   |/
         *   0----5
         */

        public TetraFilter()
        {
            int p1 = NeighborOf(0, MustBe.Equal);
            int p2 = NeighborOf(p1, MustBe.Equal);
            int p3 = NeighborOf(p1, MustBe.Equal);

            int p4 = NeighborOf(0, MustBe.NotEqual);
            int p5 = NeighborOf(0, MustBe.NotEqual);
            int p6 = NeighborOf(p2, MustBe.NotEqual);

            AddQuad(0, p4, 0, p5, p5, p3, p2, p4);
            AddQuad(p2, p4, p3, p5, p3, p6, p2, p6);
        }
    }

    public class SnakeFilter : FilterBuilder
    {
        /*  Cube :
         * 
         *     4----6
         *   ./|   /|
         *   1----7 |
         *   | 3--|-5
         *   |/   |/
         *   0----2
         */

        public SnakeFilter()
        {
            int p1 = NeighborOf(0, MustBe.NotEqual);
            int p2 = NeighborOf(0, MustBe.NotEqual);
            int p3 = NeighborOf(0, MustBe.Equal);

            int p4 = NeighborOf(p3, MustBe.NotEqual);
            int p5 = NeighborOf(p3, MustBe.Equal);

            int p6 = NeighborOf(p5, MustBe.Equal);
            int p7 = NeighborOf(p5, MustBe.NotEqual);

            AddTriangle(0, p1, p3, p4, 0, p2);
            AddTriangle(p3, p4, p6, p7, p4, p6);
            AddTriangle(p6, p7, 0, p2, p2, p5);
            AddTriangle(p3, p4, 0, p2, p6, p7);
        }
    }

    #endregion // Filters

    #region Base

    public interface IFilterBuilder
    {
        Stack<NeighborhoodRule> GenerateRulesStack();
        void Draw(MeshData meshData, Vector3 cubeOrigin, int[] solution, bool reverseNormal);
    }

    public struct NeighborhoodRule
    {
        public int PointId { get; set; }
        public MustBe MustBe { get; set; }
    }

    public struct TriangleDescriptor
    {
        public int P0A { get; set; }
        public int P0B { get; set; }
        public int P1A { get; set; }
        public int P1B { get; set; }
        public int P2A { get; set; }
        public int P2B { get; set; }
    }

    public enum MustBe
    {
        Equal,
        NotEqual
    }

    public class MeshData
    {
        public List<Vector3> Vertices { get; private set; }
        public List<int> Triangles { get; private set; }

        public MeshData()
        {
            Vertices = new List<Vector3>();
            Triangles = new List<int>();
        }

        public void AddTriangle(int a, int b, int c)
        {
            Triangles.Add(Vertices.Count + a);
            Triangles.Add(Vertices.Count + b);
            Triangles.Add(Vertices.Count + c);

            Triangles.Add(Vertices.Count + a);
            Triangles.Add(Vertices.Count + c);
            Triangles.Add(Vertices.Count + b);
        }
    }

    public abstract class FilterBuilder : IFilterBuilder
    {
        private readonly List<NeighborhoodRule> _rules;
        private readonly List<TriangleDescriptor> _trianglesDescriptors;

        protected FilterBuilder()
        {
            _rules = new List<NeighborhoodRule>();
            _trianglesDescriptors = new List<TriangleDescriptor>();
        }

        protected int NeighborOf(int pointId, MustBe mustBe)
        {
            var rule = new NeighborhoodRule {
                PointId = pointId,
                MustBe = mustBe
            };
            _rules.Add(rule);

            return _rules.Count;
        }

        protected void AddTriangle(int p0A, int p0B, int p1A, int p1B, int p2A, int p2B)
        {
            var triangle = new TriangleDescriptor {
                P0A = p0A,
                P0B = p0B,
                P1A = p1A,
                P1B = p1B,
                P2A = p2A,
                P2B = p2B
            };

            _trianglesDescriptors.Add(triangle);
        }

        protected void AddQuad(int a0, int a1, int b0, int b1, int c0, int c1, int d0, int d1)
        {
            AddTriangle(a0, a1, b0, b1, c0, c1);
            AddTriangle(a0, a1, c0, c1, d0, d1);
        }

        public Stack<NeighborhoodRule> GenerateRulesStack()
        {
            _rules.Reverse();
            var stack = new Stack<NeighborhoodRule>(_rules);
            _rules.Reverse();

            return stack;
        }

        public void Draw(MeshData meshData, Vector3 cubeOrigin, int[] solution, bool reverseNormal)
        {
            var edges = new List<int>();
            foreach (TriangleDescriptor tri in _trianglesDescriptors)
            {
                int e0 = LookAtTable.Edges[solution[tri.P0A], solution[tri.P0B]];
                int e1 = LookAtTable.Edges[solution[tri.P1A], solution[tri.P1B]];
                int e2 = LookAtTable.Edges[solution[tri.P2A], solution[tri.P2B]];

                if (!edges.Contains(e0))
                    edges.Add(e0);
                if (!edges.Contains(e1))
                    edges.Add(e1);
                if (!edges.Contains(e2))
                    edges.Add(e2);

                if (reverseNormal)
                    meshData.AddTriangle(edges.IndexOf(e0), edges.IndexOf(e2), edges.IndexOf(e1));
                else
                    meshData.AddTriangle(edges.IndexOf(e0), edges.IndexOf(e1), edges.IndexOf(e2));
            }

            foreach (var edge in edges)
                meshData.Vertices.Add(cubeOrigin + LookAtTable.EdgeCenters[edge]);
        }
    }

    static public class LookAtTable
    {
        /*  Cube :
         * 
         *     3----7
         *   ./|   /|
         *   2----6 |
         *   | 1--|-5
         *   |/   |/
         *   0----4
         */

        static public readonly int[][] Neighborhood;
        static public readonly int[,] Edges;
        static public readonly Vector3[] EdgeCenters;

        static LookAtTable()
        {
            Neighborhood = new int[8][];

            Neighborhood[0] = new[] { 1, 2, 4 };
            Neighborhood[1] = new[] { 0, 3, 5 };
            Neighborhood[2] = new[] { 3, 0, 6 };
            Neighborhood[3] = new[] { 2, 1, 7 };
            Neighborhood[4] = new[] { 5, 6, 0 };
            Neighborhood[5] = new[] { 4, 7, 1 };
            Neighborhood[6] = new[] { 7, 4, 2 };
            Neighborhood[7] = new[] { 6, 5, 3 };

            Edges = new int[8, 8];
            for (int i = 0; i < Edges.Length; i++)
                Edges[i / 8, i % 8] = -1;

            Edges[0, 1] = 0;
            Edges[0, 2] = 1;
            Edges[3, 1] = 2;
            Edges[3, 2] = 3;
            Edges[4, 5] = 4;
            Edges[4, 6] = 5;
            Edges[7, 5] = 6;
            Edges[7, 6] = 7;
            Edges[0, 4] = 8;
            Edges[1, 5] = 9;
            Edges[2, 6] = 10;
            Edges[3, 7] = 11;

            Edges[1, 0] = 0;
            Edges[2, 0] = 1;
            Edges[1, 3] = 2;
            Edges[2, 3] = 3;
            Edges[5, 4] = 4;
            Edges[6, 4] = 5;
            Edges[5, 7] = 6;
            Edges[6, 7] = 7;
            Edges[4, 0] = 8;
            Edges[5, 1] = 9;
            Edges[6, 2] = 10;
            Edges[7, 3] = 11;

            EdgeCenters = new Vector3[12];

            EdgeCenters[0] = new Vector3(0.0f, 0.0f, 0.5f);
            EdgeCenters[1] = new Vector3(0.0f, 0.5f, 0.0f);
            EdgeCenters[2] = new Vector3(0.0f, 0.5f, 1.0f);
            EdgeCenters[3] = new Vector3(0.0f, 1.0f, 0.5f);
            EdgeCenters[4] = new Vector3(1.0f, 0.0f, 0.5f);
            EdgeCenters[5] = new Vector3(1.0f, 0.5f, 0.0f);
            EdgeCenters[6] = new Vector3(1.0f, 0.5f, 1.0f);
            EdgeCenters[7] = new Vector3(1.0f, 1.0f, 0.5f);
            EdgeCenters[8] = new Vector3(0.5f, 0.0f, 0.0f);
            EdgeCenters[9] = new Vector3(0.5f, 0.0f, 1.0f);
            EdgeCenters[10] = new Vector3(0.5f, 1.0f, 0.0f);
            EdgeCenters[11] = new Vector3(0.5f, 1.0f, 1.0f);
        }
    }

    #endregion // Base
}

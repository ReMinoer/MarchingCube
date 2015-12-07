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
    private bool[][][] _grid;

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
        Blob[] blobs = (BlobsRootNode ?? gameObject).GetComponentsInChildren<Blob>();

        if (blobs.All(x => !x.Dirty))
            return;

        bool[][][] grid = ComputeGrid(blobs);
        _grid = grid;

        GenerateMesh(grid);
    }

    void OnDrawGizmos()
    {
        if (_grid == null)
            return;

        for (int i = 0; i < _grid.Length; i++)
            for (int j = 0; j < _grid[i].Length; j++)
                for (int k = 0; k < _grid[i][j].Length; k++)
                {
                    if (!_grid[i][j][k])
                        continue;

                    float x = _gridBound.bounds.min.x + i * GridStep;
                    float y = _gridBound.bounds.min.y + j * GridStep;
                    float z = _gridBound.bounds.min.z + k * GridStep;

                    var pos = new Vector3(x, y, z);
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(pos, GridStep / 2f);
                }
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

                        if (localCube.All(x => x.Value == localCube.First().Value))
                            continue;

                        if (localCube.ApplyFilter(filter, out result, out state))
                        {
                            Vector3 cubeOrigin = _gridBound.bounds.min + new Vector3(i, j, k) * GridStep;
                            filter.Draw(meshData, cubeOrigin, result.Reverse().ToArray(), !state);
                        }
                    }
                }

        _mesh.vertices = meshData.Vertices.ToArray();
        _mesh.triangles = meshData.Triangles.ToArray();
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
            AddTriangle(0, p4, p2, p6, p1, p5);
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
}

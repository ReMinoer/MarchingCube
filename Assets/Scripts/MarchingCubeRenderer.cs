using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MarchingCubeRenderer : MonoBehaviour
{
    private const int VerticesPerMesh = 60000;
    static private readonly int[] VerticesOrder = { 2, 1, 0 };
    public GameObject BlobsRootNode;
    public Material Material;
    public float GridStep = 0.1f;
    public float WeightThreshold = 10f;
    private BoxCollider _gridBound;
    private Mesh _currentMesh;
    private bool[][][] _grid;
    private readonly List<GameObject> _additionalMeshes = new List<GameObject>();
    
    private void Awake()
    {
        _gridBound = GetComponent<BoxCollider>();
        _gridBound.isTrigger = true;

        _currentMesh = gameObject.AddComponent<MeshFilter>().mesh;

        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = Material;
    }

    private void Update()
    {
        Blob[] blobs = (BlobsRootNode ?? gameObject).GetComponentsInChildren<Blob>();

        if (blobs.All(x => !x.Dirty))
            return;

        bool[][][] grid = ComputeGrid(blobs);
        _grid = grid;

        CreateMesh(grid);
    }

    private bool[][][] ComputeGrid(Blob[] blobs)
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

    /*  Cube :
         * 
         *     4----5
         *    /|   /|
         *   0----1 |
         *   | 7--|-6
         *   |/   |/
         *   3----2
         */

    private void CreateMesh(bool[][][] grid)
    {
        _currentMesh.Clear();

        foreach (GameObject additionalMesh in _additionalMeshes)
            Destroy(additionalMesh);

        var meshData = new MeshData();

        for (int x = 0; x < grid.Length - 1; x++)
            for (int y = 0; y < grid[x].Length - 1; y++)
                for (int z = 0; z < grid[x][y].Length - 1; z++)
                {
                    Vector3 gridPosition = new Vector3(x, y, z) * GridStep;

                    // En partant des sommets in / out, on récupère les arrètes intersectées.
                    int index = GetIntersectedEdgesIndex(x, y, z, grid);
                    int intersectedEdges = LookAtTable.IntersectedIndex[index];

                    var intersectedEdgesCenters = new Vector3[12];
                    for (int i = 0; i < 12; i++)
                        if ((intersectedEdges & (1 << i)) != 0) // On regarde le i-ème bit de intersectedEdges, si il est à 1 on récupère le centre de l'arrète intersectée
                            intersectedEdgesCenters[i] = gridPosition + LookAtTable.EdgeCenterRelativePosition[i] - _gridBound.bounds.extents;

                    // A partir des arrètes intersectées, on va chercher quels triangles il faut créer.
                    for (int i = 0; i < 5; i++)
                    {
                        if (LookAtTable.IntersectedEdgesToTriangles[index, 3 * i] < 0)
                            break;

                        int currentIndex = meshData.Vertices.Count;
                        for (int j = 0; j < 3; j++)
                        {
                            int vertex = LookAtTable.IntersectedEdgesToTriangles[index, 3 * i + j];
                            meshData.Triangles.Add(currentIndex + VerticesOrder[j]);
                            meshData.Vertices.Add(intersectedEdgesCenters[vertex]);
                        }

                        if (meshData.Vertices.Count > VerticesPerMesh)
                        {
                            _currentMesh.vertices = meshData.Vertices.ToArray();
                            _currentMesh.triangles = meshData.Triangles.ToArray();

                            var additionalMesh = new GameObject("AdditionalMesh");
                            additionalMesh.transform.parent = transform;
                            _additionalMeshes.Add(additionalMesh);

                            _currentMesh = additionalMesh.AddComponent<MeshFilter>().mesh;

                            var meshRenderer = additionalMesh.AddComponent<MeshRenderer>();
                            meshRenderer.material = Material;

                            meshData = new MeshData();
                        }
                    }
                }

        _currentMesh.vertices = meshData.Vertices.ToArray();
        _currentMesh.triangles = meshData.Triangles.ToArray();
    }

    private int GetIntersectedEdgesIndex(int x, int y, int z, bool[][][] grid)
    {
        int index = 0;

        for (int i = 0; i < 8; i++)
        {
            int[] relative = LookAtTable.VertexRelativePosition[i];

            if (grid[x + relative[0]][y + relative[1]][z + relative[2]])
                index |= 1 << i;
        }

        return index;
    }
    
    /*
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
    */
}
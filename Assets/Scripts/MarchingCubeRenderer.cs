using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class MarchingCubeRenderer : MonoBehaviour
{
    public GameObject BlobsRootNode;
    public Material Material;
    public float GridStep = 0.1f;
    public float WeightThreshold = 10f;
    private BoxCollider _gridBound;
    private Mesh _mesh;

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

        var grid = new bool[sizeZ][][];
        for (int i = 0; i < grid.Length; i++)
        {
            grid[i] = new bool[sizeY][];
            for (int j = 0; i < grid[j].Length; i++)
                grid[i][j] = new bool[sizeZ];
        }

        for (int i = 0; i < grid.Length; i++)
            for (int j = 0; j < grid[i].Length; j++)
                for (int k = 0; k < grid[i][j].Length; k++)
                {
                    float z = _gridBound.bounds.min.z + i * GridStep;
                    float y = _gridBound.bounds.min.y + j * GridStep;
                    float x = _gridBound.bounds.min.x + k * GridStep;

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
        float half = GridStep / 2;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        int verticesCount = 0;
        for (int i = 0; i < grid.Length - 1; i++)
            for (int j = 0; j < grid[i].Length - 1; j++)
                for (int k = 0; k < grid[i][j].Length - 1; k++)
                {

                    var filters = new List<IFilter>();

                    foreach (IFilter filter in filters)
                    {
                        /*
                        float z = _gridBound.bounds.min.z + ii * GridStep;
                        float y = _gridBound.bounds.min.y + jj * GridStep;
                        float x = _gridBound.bounds.min.x + kk * GridStep;
                        */
                    }
                }

        _mesh.vertices = vertices.ToArray();
        _mesh.normals = normals.ToArray();
        _mesh.triangles = triangles.ToArray();
    }

    public interface IFilter
    {
        void Process(bool[][][] grid, int i, int j, int k);
    }

    public abstract class FilterBase : IFilter
    {
        public void Process(bool[][][] grid, int i, int j, int k)
        {
            for (int ii = 0; ii <= 1; ii++)
                for (int jj = 0; jj <= 1; jj++)
                    for (int kk = 0; kk <= 1; kk++)
                    {
                        ApplyFilter(grid, i, j, k, ii, jj, kk);
                    }
        }

        protected abstract void ApplyFilter(bool[][][] grid, int i, int j, int k, int ii, int jj, int kk);
    }

    public class CornerFilter : FilterBase
    {
        protected override void ApplyFilter(bool[][][] grid, int i, int j, int k, int ii, int jj, int kk)
        {
            bool originValue = grid[i + ii][j + jj][k + kk];

            bool filtered = grid[i + (ii + 1) % 2][j + jj][k + kk] != originValue
                && grid[i + ii][j + (jj + 1) % 2][k + kk] != originValue
                && grid[i + ii][j + jj][k + (kk + 1) % 2] != originValue;
        }
    }

    void OnDrawGizmos()
    {
        var bounds = GetComponent<BoxCollider>().bounds;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}

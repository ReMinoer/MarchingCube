using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(RectTransform))]
public class MarchingSquareRenderer : MonoBehaviour
{
    public GameObject BlobsRootNode;
    public Material Material;
    public float GridStep = 0.1f;
    public float WeightThreshold = 10f;
    private RectTransform _gridBound;
    private Mesh _mesh;

    void Awake()
    {
        _gridBound = GetComponent<RectTransform>();
        _mesh = gameObject.AddComponent<MeshFilter>().mesh;
        
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material = Material;
    }

    void Update()
    {
        // NOTE : Ne pas updater la liste de blob à chaque update
        Blob[] blobs = (BlobsRootNode ?? gameObject).GetComponentsInChildren<Blob>();
        bool[][] vertices = ComputeGrid(blobs);

        int[][] patterns = ComputeGridPatterns(vertices);
        GenerateMesh(patterns);
    }

    public bool[][] ComputeGrid(Blob[] blobs)
    {
        int sizeX = (int)(_gridBound.rect.width / GridStep) + 1;
        int sizeY = (int)(_gridBound.rect.height / GridStep) + 1;

        var vertices = new bool[sizeY][];
        for (int i = 0; i < sizeY; i++)
            vertices[i] = new bool[sizeX];

        for (int i = 0; i < sizeY; i++)
            for (int j = 0; j < sizeX; j++)
            {
                float y = _gridBound.offsetMin.y + i * GridStep;
                float x = _gridBound.offsetMin.x + j * GridStep;
                var pos = new Vector2(x, y);

                float totalWeigth = 0;
                foreach (Blob blob in blobs)
                {
                    float distance = Vector2.Distance(pos, blob.transform.position);

                    float weight = blob.WeightFunction(distance);
                    if (weight < 0)
                        weight = 0;

                    totalWeigth += weight;
                }

                vertices[i][j] = totalWeigth > WeightThreshold;
            }

        return vertices;
    }

    private int[][] ComputeGridPatterns(bool[][] vertices)
    {
        var cases = new int[vertices.Length - 1][];
        for (int i = 0; i < cases.Length; i++)
            cases[i] = new int[vertices[i].Length - 1];

        for (int i = 0; i < cases.Length; i++)
            for (int j = 0; j < cases[i].Length; j++)
            {
                if (vertices[i][j]) cases[i][j] += 1;
                if (vertices[i][j + 1]) cases[i][j] += 2;
                if (vertices[i + 1][j + 1]) cases[i][j] += 4;
                if (vertices[i + 1][j]) cases[i][j] += 8;
            }

        return cases;
    }

    private void GenerateMesh(int[][] patterns)
    {
        _mesh.Clear();
        float half = GridStep / 2;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();

        int verticesCount = 0;
        for (int i = 0; i < patterns.Length; i++)
        {
            for (int j = 0; j < patterns[i].Length; j++)
            {
                float y = _gridBound.offsetMin.y + i * GridStep;
                float x = _gridBound.offsetMin.x + j * GridStep;

                switch (patterns[i][j])
                {
                    case 0:
                        break;
                    case 1:
                        vertices.Add(new Vector3(x, y + half));
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x, y));
                        normals.AddNormals(3);
                        triangles.AddTriangle(verticesCount, 0, 1, 2);
                        verticesCount += 3;
                        break;
                    case 2:
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x + half, y));
                        normals.AddNormals(3);
                        triangles.AddTriangle(verticesCount, 0, 1, 2);
                        verticesCount += 3;
                        break;
                    case 3:
                        vertices.Add(new Vector3(x, y + half));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x, y));
                        normals.AddNormals(4);
                        triangles.AddRectangle(verticesCount, 0, 1, 2, 3);
                        verticesCount += 4;
                        break;
                    case 4:
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        normals.AddNormals(3);
                        triangles.AddTriangle(verticesCount, 0, 1, 2);
                        verticesCount += 3;
                        break;
                    case 5:
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x, y));
                        vertices.Add(new Vector3(x, y + half));
                        normals.AddNormals(6);
                        triangles.AddTriangle(verticesCount, 0, 1, 2);
                        triangles.AddRectangle(verticesCount, 0, 2, 3, 5);
                        triangles.AddTriangle(verticesCount, 5, 3, 4);
                        verticesCount += 6;
                        break;
                    case 6:
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x + half, y));
                        normals.AddNormals(4);
                        triangles.AddRectangle(verticesCount, 0, 1, 2, 3);
                        verticesCount += 4;
                        break;
                    case 7:
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x, y));
                        vertices.Add(new Vector3(x, y + half));
                        normals.AddNormals(5);
                        triangles.AddTriangle(verticesCount, 0, 1, 2);
                        triangles.AddTriangle(verticesCount, 0, 2, 4);
                        triangles.AddTriangle(verticesCount, 4, 2, 3);
                        verticesCount += 5;
                        break;
                    case 8:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x, y + half));
                        normals.AddNormals(3);
                        triangles.AddTriangle(verticesCount, 0, 1, 2);
                        verticesCount += 3;
                        break;
                    case 9:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x, y));
                        normals.AddNormals(4);
                        triangles.AddRectangle(verticesCount, 0, 1, 2, 3);
                        verticesCount += 4;
                        break;
                    case 10:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x, y + half));
                        normals.AddNormals(6);
                        triangles.AddTriangle(verticesCount, 0, 1, 5);
                        triangles.AddRectangle(verticesCount, 1, 2, 4, 5);
                        triangles.AddTriangle(verticesCount, 4, 2, 3);
                        verticesCount += 6;
                        break;
                    case 11:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x, y));
                        normals.AddNormals(5);
                        triangles.AddTriangle(verticesCount, 0, 1, 4);
                        triangles.AddTriangle(verticesCount, 1, 2, 4);
                        triangles.AddTriangle(verticesCount, 2, 3, 4);
                        verticesCount += 5;
                        break;
                    case 12:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x, y + half));
                        normals.AddNormals(4);
                        triangles.AddRectangle(verticesCount, 0, 1, 2, 3);
                        verticesCount += 4;
                        break;
                    case 13:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x, y));
                        normals.AddNormals(5);
                        triangles.AddTriangle(verticesCount, 0, 1, 2);
                        triangles.AddTriangle(verticesCount, 0, 2, 3);
                        triangles.AddTriangle(verticesCount, 0, 3, 4);
                        verticesCount += 5;
                        break;
                    case 14:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x, y + half));
                        normals.AddNormals(5);
                        triangles.AddTriangle(verticesCount, 0, 1, 4);
                        triangles.AddTriangle(verticesCount, 1, 3, 4);
                        triangles.AddTriangle(verticesCount, 1, 2, 3);
                        verticesCount += 5;
                        break;
                    case 15:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x, y));
                        normals.AddNormals(4);
                        triangles.AddRectangle(verticesCount, 0, 1, 2, 3);
                        verticesCount += 4;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        _mesh.vertices = vertices.ToArray();
        _mesh.normals = normals.ToArray();
        _mesh.triangles = triangles.ToArray();
    }

    void OnDrawGizmos()
    {
        var bounds = GetComponent<RectTransform>();

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(bounds.position, new Vector3(bounds.rect.width, bounds.rect.height));
    }
}

static class MeshFactoryTools
{
    static public void AddTriangle(this List<int> list, int currentIndex, int a, int b, int c)
    {
        list.Add(currentIndex + a);
        list.Add(currentIndex + b);
        list.Add(currentIndex + c);
    }

    static public void AddRectangle(this List<int> list, int currentIndex, int a, int b, int c, int d)
    {
        list.Add(currentIndex + a);
        list.Add(currentIndex + b);
        list.Add(currentIndex + c);
        list.Add(currentIndex + a);
        list.Add(currentIndex + c);
        list.Add(currentIndex + d);
    }

    static public void AddNormals(this List<Vector3> list, int count)
    {
        for (int i = 0; i < count; i++)
            list.Add(Vector3.back);
    }
}

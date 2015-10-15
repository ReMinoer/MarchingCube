using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(RectTransform))]
public class MarchingSquareRenderer : MonoBehaviour
{
    public struct WeightGrid
    {
        public bool[][] Vertices { get; set; }
    }

    public GameObject BlobsRootNode;
    public float GridStep = 0.1f;
    public float WeightThreshold = 10f;
    private RectTransform _gridBound;
    private Mesh _mesh;

    void Awake()
    {
        _gridBound = GetComponent<RectTransform>();
        _mesh = gameObject.AddComponent<MeshFilter>().mesh;
        gameObject.AddComponent<MeshRenderer>();
    }

    void Update()
    {
        // NOTE : Ne pas updater la liste de blob à chaque update
        IEnumerable<Blob> blobs = (BlobsRootNode ?? gameObject).GetComponentsInChildren<Blob>();
        bool[][] vertices = ComputeGrid(blobs);

        int[][] patterns = ComputeGridPatterns(vertices);
        GenerateMesh(patterns);
    }

    public bool[][] ComputeGrid(IEnumerable<Blob> blobs)
    {
        int sizeX = (int)(_gridBound.rect.width / GridStep);
        int sizeY = (int)(_gridBound.rect.height / GridStep);

        bool[][] vertices = new bool[sizeY][];
        for (int i = 0; i < sizeY; i++)
            vertices[i] = new bool[sizeX];

        for (int i = 0; i < sizeY; i++)
            for (int j = 0; j < sizeX; j++)
            {
                float y = _gridBound.anchorMin.y + i * GridStep;
                float x = _gridBound.anchorMin.x + j * GridStep;
                Vector2 pos = new Vector2(x, y);

                float weigth = 0;
                foreach (Blob blob in blobs)
                {
                    float distance = Vector2.Distance(pos, blob.transform.position);
                    weigth += blob.WeightFunction(distance);
                }

                vertices[i][j] = weigth > WeightThreshold;
            }

        return vertices;
    }

    private int[][] ComputeGridPatterns(bool[][] vertices)
    {
        int[][] cases = new int[vertices.Length - 1][];
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

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int triangleCount = 0;
        for (int i = 0; i < patterns.Length; i++)
        {
            for (int j = 0; j < patterns[i].Length; j++)
            {
                float x = j * GridStep;
                float y = i * GridStep;

                switch (patterns[i][j])
                {
                    case 0:
                        break;
                    case 1:
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x, y + half));
                        vertices.Add(new Vector3(x, y));
                        triangles.AddTriangle(triangleCount, 0, 1, 2);
                        triangleCount += 1;
                        break;
                    case 2:
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + GridStep, y));
                        triangles.AddTriangle(triangleCount, 0, 1, 2);
                        triangleCount += 1;
                        break;
                    case 3:
                        vertices.Add(new Vector3(x, y));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x, y + half));
                        triangles.AddRectangle(triangleCount, 0, 1, 2, 3);
                        triangleCount += 2;
                        break;
                    case 4:
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        triangles.AddTriangle(triangleCount, 0, 1, 2);
                        triangleCount += 1;
                        break;
                    case 5:
                        vertices.Add(new Vector3(x, y));
                        vertices.Add(new Vector3(x, y + half));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + half, y));
                        triangles.AddTriangle(triangleCount, 0, 1, 5);
                        triangles.AddRectangle(triangleCount, 1, 2, 4, 5);
                        triangles.AddTriangle(triangleCount, 2, 3, 4);
                        triangleCount += 4;
                        break;
                    case 6:
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y));
                        triangles.AddRectangle(triangleCount, 0, 1, 2, 3);
                        triangleCount += 2;
                        break;
                    case 7:
                        vertices.Add(new Vector3(x, y));
                        vertices.Add(new Vector3(x, y + half));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y));
                        triangles.AddTriangle(triangleCount, 0, 1, 4);
                        triangles.AddTriangle(triangleCount, 1, 2, 4);
                        triangles.AddTriangle(triangleCount, 2, 3, 4);
                        triangleCount += 3;
                        break;
                    case 8:
                        vertices.Add(new Vector3(x, y + half));
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        triangles.AddTriangle(triangleCount, 0, 1, 2);
                        triangleCount += 1;
                        break;
                    case 9:
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x, y));
                        triangles.AddRectangle(triangleCount, 0, 1, 2, 3);
                        triangleCount += 2;
                        break;
                    case 10:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x, y + half));
                        triangles.AddTriangle(triangleCount, 0, 1, 5);
                        triangles.AddRectangle(triangleCount, 1, 2, 4, 5);
                        triangles.AddTriangle(triangleCount, 2, 3, 4);
                        triangleCount += 4;
                        break;
                    case 11:
                        vertices.Add(new Vector3(x, y));
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + GridStep, y));
                        triangles.AddTriangle(triangleCount, 0, 1, 2);
                        triangles.AddTriangle(triangleCount, 0, 2, 3);
                        triangles.AddTriangle(triangleCount, 0, 3, 4);
                        triangleCount += 3;
                        break;
                    case 12:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x, y + half));
                        triangles.AddRectangle(triangleCount, 0, 1, 2, 3);
                        triangleCount += 2;
                        break;
                    case 13:
                        vertices.Add(new Vector3(x, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y + half));
                        vertices.Add(new Vector3(x + half, y));
                        vertices.Add(new Vector3(x, y));
                        triangles.AddTriangle(triangleCount, 0, 1, 2);
                        triangles.AddTriangle(triangleCount, 0, 2, 3);
                        triangles.AddTriangle(triangleCount, 0, 3, 4);
                        triangleCount += 3;
                        break;
                    case 14:
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x, y));
                        vertices.Add(new Vector3(x, y + half));
                        vertices.Add(new Vector3(x + half, y + GridStep));
                        triangles.AddTriangle(triangleCount, 0, 1, 2);
                        triangles.AddTriangle(triangleCount, 0, 2, 3);
                        triangles.AddTriangle(triangleCount, 0, 3, 4);
                        triangleCount += 3;
                        break;
                    case 15:
                        vertices.Add(new Vector3(x, y));
                        vertices.Add(new Vector3(x + GridStep, y));
                        vertices.Add(new Vector3(x + GridStep, y + GridStep));
                        vertices.Add(new Vector3(x, y + GridStep));
                        triangles.AddRectangle(triangleCount, 0, 1, 2, 3);
                        triangleCount += 2;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
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
}
